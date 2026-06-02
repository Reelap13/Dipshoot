using System.Collections.Generic;
using UnityEngine;

namespace Game.ProcGen.Warehouse
{
    internal static class WarehousePlacementRules
    {
        private const float DefaultHighContainerChance = 0.15f;

        public static int CleanupLayout(WarehouseLayoutData layout, bool[,] reservedGround = null)
        {
            return CleanupLayout(layout, reservedGround, DefaultHighContainerChance);
        }

        public static int CleanupLayout(
            WarehouseLayoutData layout,
            bool[,] reservedGround,
            float highContainerChance)
        {
            int changed = 0;
            for (int i = 0; i < 4; i++)
            {
                int iterationChanges = ReplaceEnclosedBridges(layout, highContainerChance);
                iterationChanges += FillStructuralCavities(layout, reservedGround, highContainerChance);
                iterationChanges += PromoteLowContainersNearHighClusters(layout);
                iterationChanges += RepairCoverRotations(layout);
                iterationChanges += RemoveInvalidLaddersAndCovers(layout, BuildReachableGroundMask(layout));
                changed += iterationChanges;

                if (iterationChanges == 0)
                    break;
            }

            return changed;
        }

        public static int FillContainerHoles(WarehouseLayoutData layout, bool[,] reservedGround = null)
        {
            return FillStructuralCavities(layout, reservedGround, DefaultHighContainerChance);
        }

        public static int FillStructuralCavities(WarehouseLayoutData layout, bool[,] reservedGround = null)
        {
            return FillStructuralCavities(layout, reservedGround, DefaultHighContainerChance);
        }

        public static int FillStructuralCavities(
            WarehouseLayoutData layout,
            bool[,] reservedGround,
            float highContainerChance)
        {
            int filled = 0;
            for (int iteration = 0; iteration < 4; iteration++)
            {
                List<FillCandidate> candidates = new();
                for (int y = 0; y < layout.Height; y++)
                {
                    for (int x = 0; x < layout.Width; x++)
                    {
                        Vector2Int cell = new(x, y);
                        if (IsReserved(layout, reservedGround, cell) || HasAnyObjectAt(layout, cell))
                            continue;

                        WarehouseObjectKind kind;
                        if (CountSolidSides(layout, cell, IsContainerOrOutside) >= 3)
                            kind = ChooseFillContainerKind(cell, highContainerChance);
                        else if (CountSolidSides(layout, cell, IsTopWalkableOrOutside) >= 3)
                            kind = WarehouseObjectKind.Bridge;
                        else
                            continue;

                        WarehouseObjectPlacement probe = new()
                        {
                            Kind = kind,
                            Side = cell.y < layout.Height / 2 ? WarehouseSide.A : WarehouseSide.B,
                            Origin = cell,
                            Size = Vector2Int.one
                        };

                        if (WarehouseRepairPass.ObjectTouchesRadius(probe, layout.SpawnA, layout.SpawnClearRadius) ||
                            WarehouseRepairPass.ObjectTouchesRadius(probe, layout.SpawnB, layout.SpawnClearRadius) ||
                            WarehouseRepairPass.ObjectTouchesRadius(probe, layout.CapturePoint, layout.CaptureClearRadius))
                        {
                            continue;
                        }

                        candidates.Add(new FillCandidate(cell, kind));
                    }
                }

                for (int i = 0; i < candidates.Count; i++)
                {
                    FillCandidate candidate = candidates[i];
                    layout.Objects.Add(new WarehouseObjectPlacement
                    {
                        Kind = candidate.Kind,
                        Side = candidate.Cell.y < layout.Height / 2 ? WarehouseSide.A : WarehouseSide.B,
                        Origin = candidate.Cell,
                        Size = Vector2Int.one
                    });
                }

                filled += candidates.Count;
                if (candidates.Count == 0)
                    break;
            }

            return filled;
        }

        public static int ReplaceEnclosedBridges(WarehouseLayoutData layout, float highContainerChance)
        {
            int changed = 0;
            for (int i = 0; i < layout.Objects.Count; i++)
            {
                WarehouseObjectPlacement obj = layout.Objects[i];
                if (obj.Kind != WarehouseObjectKind.Bridge ||
                    !ShouldReplaceBridgeWithContainer(layout, obj.Origin))
                {
                    continue;
                }

                obj.Kind = ChooseFillContainerKind(obj.Origin, highContainerChance);
                obj.Surface = WarehousePlacementSurface.Ground;
                obj.Direction = default;
                obj.ConnectionMask = WarehouseDirectionMask.None;
                obj.BridgeConnectionType = WarehouseBridgeConnectionType.Straight;
                obj.RotationY = 0f;
                obj.VariantIndex = -1;
                changed++;
            }

            return changed;
        }

        public static int PromoteLowContainersNearHighClusters(WarehouseLayoutData layout)
        {
            List<WarehouseObjectPlacement> candidates = new();
            for (int i = 0; i < layout.Objects.Count; i++)
            {
                WarehouseObjectPlacement obj = layout.Objects[i];
                if (obj.Kind == WarehouseObjectKind.ContainerLow &&
                    CountSolidSides(layout, obj.Origin, IsHighContainerAtCell) >= 3)
                {
                    candidates.Add(obj);
                }
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                candidates[i].Kind = WarehouseObjectKind.ContainerHigh;
                candidates[i].Surface = WarehousePlacementSurface.Ground;
                candidates[i].Direction = default;
                candidates[i].ConnectionMask = WarehouseDirectionMask.None;
                candidates[i].BridgeConnectionType = WarehouseBridgeConnectionType.Straight;
                candidates[i].RotationY = 0f;
                candidates[i].VariantIndex = -1;
            }

            return candidates.Count;
        }

        private static bool ShouldReplaceBridgeWithContainer(WarehouseLayoutData layout, Vector2Int cell)
        {
            int containerOrOutsideSides = CountSolidSides(layout, cell, IsContainerOrOutside);
            if (containerOrOutsideSides >= 4)
                return true;

            return containerOrOutsideSides >= 3 &&
                   CountSolidSides(layout, cell, IsBridgeAtCell) > 0;
        }

        public static int RepairCoverRotations(WarehouseLayoutData layout)
        {
            int changed = 0;
            for (int i = 0; i < layout.Objects.Count; i++)
            {
                WarehouseObjectPlacement obj = layout.Objects[i];
                if (!obj.IsCover)
                    continue;

                if (IsCoverPlacementValid(layout, obj))
                    continue;

                if (TryChooseCoverRotation(layout, obj.Origin, obj.Surface, null, out float rotationY, obj))
                {
                    obj.RotationY = rotationY;
                    obj.Direction = RotationToDirection(rotationY);
                    changed++;
                }
            }

            return changed;
        }

        public static int RemoveInvalidLaddersAndCovers(WarehouseLayoutData layout)
        {
            return RemoveInvalidLaddersAndCovers(layout, BuildReachableGroundMask(layout));
        }

        public static int RemoveInvalidLaddersAndCovers(WarehouseLayoutData layout, bool[,] reachableGround)
        {
            int removed = 0;
            for (int i = layout.Objects.Count - 1; i >= 0; i--)
            {
                WarehouseObjectPlacement obj = layout.Objects[i];
                if (obj.IsLadder && !IsLadderPlacementValid(layout, obj, reachableGround) ||
                    obj.IsCover && !IsCoverPlacementValid(layout, obj))
                {
                    layout.Objects.RemoveAt(i);
                    removed++;
                }
            }

            return removed;
        }

        public static bool IsLadderPlacementValid(WarehouseLayoutData layout, WarehouseObjectPlacement ladder)
        {
            return IsLadderPlacementValid(layout, ladder, null);
        }

        public static bool IsLadderPlacementValid(
            WarehouseLayoutData layout,
            WarehouseObjectPlacement ladder,
            bool[,] reachableGround)
        {
            if (!ladder.IsLadder || !layout.IsInside(ladder.Origin))
                return false;

            Vector2Int forward = DirectionToVector(ladder.Direction);
            Vector2Int topCell = ladder.Origin + forward;
            Vector2Int approachCell = ladder.Origin - forward;
            return layout.IsInside(topCell) &&
                   layout.IsInside(approachCell) &&
                   HasContainerLowAt(layout, topCell) &&
                   !HasStructureAt(layout, ladder.Origin) &&
                   IsGroundCellEmpty(layout, approachCell) &&
                   (reachableGround == null || reachableGround[approachCell.x, approachCell.y]);
        }

        public static bool[,] BuildReachableGroundMask(WarehouseLayoutData layout)
        {
            bool[,] reachable = new bool[layout.Width, layout.Height];
            Queue<Vector2Int> queue = new();
            EnqueueGroundStart(layout, layout.SpawnA, reachable, queue);
            EnqueueGroundStart(layout, layout.SpawnB, reachable, queue);
            EnqueueGroundStart(layout, layout.CapturePoint, reachable, queue);

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                for (int i = 0; i < WarehouseGenerationUtility.CardinalDirections.Length; i++)
                {
                    Vector2Int next = current + WarehouseGenerationUtility.CardinalDirections[i];
                    if (!layout.IsInside(next) ||
                        reachable[next.x, next.y] ||
                        !IsGroundTraversalCell(layout, next) ||
                        IsGroundMovementBlockedByCover(layout, current, next))
                    {
                        continue;
                    }

                    reachable[next.x, next.y] = true;
                    queue.Enqueue(next);
                }
            }

            return reachable;
        }

        public static int SealUnreachableGroundPockets(
            WarehouseLayoutData layout,
            bool[,] reservedGround,
            int maxPocketCells)
        {
            if (maxPocketCells <= 0)
                return 0;

            bool[,] reachable = BuildPocketReachableGroundMask(layout);
            bool[,] visited = new bool[layout.Width, layout.Height];
            int sealedCells = 0;

            for (int y = 0; y < layout.Height; y++)
            {
                for (int x = 0; x < layout.Width; x++)
                {
                    Vector2Int cell = new(x, y);
                    if (visited[x, y] ||
                        reachable[x, y] ||
                        !IsGroundPocketTraversalCell(layout, cell))
                    {
                        continue;
                    }

                    List<Vector2Int> component = BuildGroundPocketComponent(layout, cell, visited);
                    if (component.Count > maxPocketCells)
                        continue;

                    for (int i = 0; i < component.Count; i++)
                    {
                        if (TrySealGroundPocketCell(layout, reservedGround, component[i]))
                            sealedCells++;
                    }
                }
            }

            return sealedCells;
        }

        public static bool TryChooseCoverRotation(
            WarehouseLayoutData layout,
            Vector2Int cell,
            WarehousePlacementSurface surface,
            System.Random random,
            out float rotationY)
        {
            return TryChooseCoverRotation(layout, cell, surface, random, out rotationY, null);
        }

        public static bool TryChooseCoverRotation(
            WarehouseLayoutData layout,
            Vector2Int cell,
            WarehousePlacementSurface surface,
            System.Random random,
            out float rotationY,
            WarehouseObjectPlacement ignore)
        {
            WarehouseDirection[] directions =
            {
                WarehouseDirection.North,
                WarehouseDirection.East,
                WarehouseDirection.South,
                WarehouseDirection.West
            };

            if (random != null)
                WarehouseGenerationUtility.Shuffle(directions, random);

            for (int i = 0; i < directions.Length; i++)
            {
                float candidate = DirectionToRotation(directions[i]);
                WarehouseObjectPlacement probe = new()
                {
                    Kind = WarehouseObjectKind.PartialCover,
                    Origin = cell,
                    Size = Vector2Int.one,
                    Surface = surface,
                    RotationY = candidate
                };

                if (IsCoverPlacementValid(layout, probe, ignore))
                {
                    rotationY = candidate;
                    return true;
                }
            }

            rotationY = 0f;
            return false;
        }

        public static bool IsCoverPlacementValid(WarehouseLayoutData layout, WarehouseObjectPlacement cover)
        {
            return IsCoverPlacementValid(layout, cover, cover);
        }

        private static bool IsCoverPlacementValid(
            WarehouseLayoutData layout,
            WarehouseObjectPlacement cover,
            WarehouseObjectPlacement ignore)
        {
            if (!cover.IsCover && cover.Kind != WarehouseObjectKind.PartialCover)
                return false;

            if (!layout.IsInside(cover.Origin) || HasLadderAt(layout, cover.Origin))
                return false;

            Vector2Int forwardCell = cover.Origin + RotationToVector(cover.RotationY);
            if (!layout.IsInside(forwardCell))
                return false;

            return cover.Surface == WarehousePlacementSurface.StructureTop
                ? IsTopCoverPlacementValid(layout, cover.Origin, forwardCell, ignore)
                : IsGroundCoverPlacementValid(layout, cover.Origin, forwardCell, ignore);
        }

        private static bool IsGroundCoverPlacementValid(
            WarehouseLayoutData layout,
            Vector2Int origin,
            Vector2Int forwardCell,
            WarehouseObjectPlacement ignore)
        {
            if (HasStructureAt(layout, origin) || HasCoverAt(layout, origin, WarehousePlacementSurface.Ground, ignore))
                return false;

            if (HasBridgeAt(layout, forwardCell))
                return true;

            return !HasStructureAt(layout, forwardCell) &&
                   !HasLadderAt(layout, forwardCell) &&
                   !HasAnyCoverAt(layout, forwardCell, ignore);
        }

        private static bool IsTopCoverPlacementValid(
            WarehouseLayoutData layout,
            Vector2Int origin,
            Vector2Int forwardCell,
            WarehouseObjectPlacement ignore)
        {
            if ((!HasContainerLowAt(layout, origin) && !HasBridgeAt(layout, origin)) ||
                HasContainerHighAt(layout, origin) ||
                HasCoverAt(layout, origin, WarehousePlacementSurface.StructureTop, ignore))
            {
                return false;
            }

            if (HasContainerHighAt(layout, forwardCell) || HasAnyCoverAt(layout, forwardCell, ignore))
                return false;

            if (HasContainerLowAt(layout, forwardCell) || HasBridgeAt(layout, forwardCell))
                return true;

            WarehouseObjectPlacement ladder = GetLadderAt(layout, forwardCell);
            if (ladder != null)
                return ladder.Origin + DirectionToVector(ladder.Direction) != origin;

            return !HasStructureAt(layout, forwardCell);
        }

        private static bool IsGroundCellEmpty(WarehouseLayoutData layout, Vector2Int cell)
        {
            return layout.IsInside(cell) &&
                   !HasStructureAt(layout, cell) &&
                   !HasLadderAt(layout, cell) &&
                   !HasCoverAt(layout, cell, WarehousePlacementSurface.Ground);
        }

        private static bool IsGroundTraversalCell(WarehouseLayoutData layout, Vector2Int cell)
        {
            return layout.IsInside(cell) &&
                   !HasStructureAt(layout, cell) &&
                   !HasLadderAt(layout, cell);
        }

        private static bool[,] BuildPocketReachableGroundMask(WarehouseLayoutData layout)
        {
            bool[,] reachable = new bool[layout.Width, layout.Height];
            Queue<Vector2Int> queue = new();
            EnqueuePocketStart(layout, layout.SpawnA, reachable, queue);
            EnqueuePocketStart(layout, layout.SpawnB, reachable, queue);
            EnqueuePocketStart(layout, layout.CapturePoint, reachable, queue);

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                for (int i = 0; i < WarehouseGenerationUtility.CardinalDirections.Length; i++)
                {
                    Vector2Int next = current + WarehouseGenerationUtility.CardinalDirections[i];
                    if (!layout.IsInside(next) ||
                        reachable[next.x, next.y] ||
                        !IsGroundPocketTraversalCell(layout, next) ||
                        IsGroundMovementBlockedByCover(layout, current, next))
                    {
                        continue;
                    }

                    reachable[next.x, next.y] = true;
                    queue.Enqueue(next);
                }
            }

            return reachable;
        }

        private static void EnqueuePocketStart(
            WarehouseLayoutData layout,
            Vector2 position,
            bool[,] reachable,
            Queue<Vector2Int> queue)
        {
            Vector2Int cell = GridPointToCell(position);
            if (!layout.IsInside(cell) || reachable[cell.x, cell.y] || !IsGroundPocketTraversalCell(layout, cell))
                return;

            reachable[cell.x, cell.y] = true;
            queue.Enqueue(cell);
        }

        private static List<Vector2Int> BuildGroundPocketComponent(
            WarehouseLayoutData layout,
            Vector2Int start,
            bool[,] visited)
        {
            List<Vector2Int> component = new();
            Queue<Vector2Int> queue = new();
            visited[start.x, start.y] = true;
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                component.Add(current);

                for (int i = 0; i < WarehouseGenerationUtility.CardinalDirections.Length; i++)
                {
                    Vector2Int next = current + WarehouseGenerationUtility.CardinalDirections[i];
                    if (!layout.IsInside(next) ||
                        visited[next.x, next.y] ||
                        !IsGroundPocketTraversalCell(layout, next) ||
                        IsGroundMovementBlockedByCover(layout, current, next))
                    {
                        continue;
                    }

                    visited[next.x, next.y] = true;
                    queue.Enqueue(next);
                }
            }

            return component;
        }

        private static bool IsGroundPocketTraversalCell(WarehouseLayoutData layout, Vector2Int cell)
        {
            return layout.IsInside(cell) && !HasContainerAt(layout, cell);
        }

        private static bool TrySealGroundPocketCell(
            WarehouseLayoutData layout,
            bool[,] reservedGround,
            Vector2Int cell)
        {
            if (!layout.IsInside(cell) ||
                IsReserved(layout, reservedGround, cell) ||
                HasContainerAt(layout, cell) ||
                HasBridgeAt(layout, cell))
            {
                return false;
            }

            WarehouseObjectPlacement probe = new()
            {
                Kind = WarehouseObjectKind.ContainerHigh,
                Side = cell.y < layout.Height / 2 ? WarehouseSide.A : WarehouseSide.B,
                Origin = cell,
                Size = Vector2Int.one
            };

            if (WarehouseRepairPass.ObjectTouchesRadius(probe, layout.SpawnA, layout.SpawnClearRadius) ||
                WarehouseRepairPass.ObjectTouchesRadius(probe, layout.SpawnB, layout.SpawnClearRadius) ||
                WarehouseRepairPass.ObjectTouchesRadius(probe, layout.CapturePoint, layout.CaptureClearRadius))
            {
                return false;
            }

            RemovePocketObjectsAt(layout, cell);
            layout.Objects.Add(probe);
            return true;
        }

        private static void RemovePocketObjectsAt(WarehouseLayoutData layout, Vector2Int cell)
        {
            for (int i = layout.Objects.Count - 1; i >= 0; i--)
            {
                WarehouseObjectPlacement obj = layout.Objects[i];
                if ((obj.IsLadder || obj.IsCover) && GetRect(obj).Contains(cell))
                    layout.Objects.RemoveAt(i);
            }
        }

        private static bool IsGroundMovementBlockedByCover(WarehouseLayoutData layout, Vector2Int from, Vector2Int to)
        {
            if (!layout.IsInside(from) || !layout.IsInside(to))
                return false;

            Vector2Int movement = to - from;
            if (Mathf.Abs(movement.x) + Mathf.Abs(movement.y) != 1)
                return false;

            return IsGroundCoverBlockingMovement(layout, from, movement) ||
                   IsGroundCoverBlockingMovement(layout, to, movement);
        }

        private static bool IsGroundCoverBlockingMovement(
            WarehouseLayoutData layout,
            Vector2Int cell,
            Vector2Int movement)
        {
            WarehouseObjectPlacement cover = GetCoverAt(layout, cell, WarehousePlacementSurface.Ground);
            if (cover == null)
                return false;

            Vector2Int direction = RotationToVector(cover.RotationY);
            return movement == direction || movement == -direction;
        }

        private static void EnqueueGroundStart(
            WarehouseLayoutData layout,
            Vector2 position,
            bool[,] reachable,
            Queue<Vector2Int> queue)
        {
            Vector2Int cell = GridPointToCell(position);
            if (!layout.IsInside(cell) || reachable[cell.x, cell.y] || !IsGroundTraversalCell(layout, cell))
                return;

            reachable[cell.x, cell.y] = true;
            queue.Enqueue(cell);
        }

        private static int CountSolidSides(
            WarehouseLayoutData layout,
            Vector2Int cell,
            System.Func<WarehouseLayoutData, Vector2Int, bool> predicate)
        {
            int count = 0;
            for (int i = 0; i < WarehouseGenerationUtility.CardinalDirections.Length; i++)
            {
                if (predicate(layout, cell + WarehouseGenerationUtility.CardinalDirections[i]))
                    count++;
            }

            return count;
        }

        private static bool IsContainerOrOutside(WarehouseLayoutData layout, Vector2Int cell)
        {
            return !layout.IsInside(cell) || HasContainerAt(layout, cell);
        }

        private static bool IsTopWalkableOrOutside(WarehouseLayoutData layout, Vector2Int cell)
        {
            return !layout.IsInside(cell) || HasContainerLowAt(layout, cell) || HasBridgeAt(layout, cell);
        }

        private static bool IsBridgeAtCell(WarehouseLayoutData layout, Vector2Int cell)
        {
            return layout.IsInside(cell) && HasBridgeAt(layout, cell);
        }

        private static bool IsHighContainerAtCell(WarehouseLayoutData layout, Vector2Int cell)
        {
            return layout.IsInside(cell) && HasContainerHighAt(layout, cell);
        }

        private static WarehouseObjectKind ChooseFillContainerKind(Vector2Int cell, float highContainerChance)
        {
            float chance = Mathf.Clamp01(highContainerChance);
            if (chance <= 0f)
                return WarehouseObjectKind.ContainerLow;

            int hash = cell.x * 73856093 ^ cell.y * 19349663 ^ 0x5bd1e995;
            hash &= 0x7fffffff;
            float value = hash / (float)int.MaxValue;
            return value < chance ? WarehouseObjectKind.ContainerHigh : WarehouseObjectKind.ContainerLow;
        }

        private static bool HasAnyObjectAt(WarehouseLayoutData layout, Vector2Int cell)
        {
            for (int i = 0; i < layout.Objects.Count; i++)
            {
                if (GetRect(layout.Objects[i]).Contains(cell))
                    return true;
            }

            return false;
        }

        private static bool HasStructureAt(WarehouseLayoutData layout, Vector2Int cell)
        {
            for (int i = 0; i < layout.Objects.Count; i++)
            {
                WarehouseObjectPlacement obj = layout.Objects[i];
                if (obj.IsStructure && GetRect(obj).Contains(cell))
                    return true;
            }

            return false;
        }

        private static bool HasContainerAt(WarehouseLayoutData layout, Vector2Int cell)
        {
            for (int i = 0; i < layout.Objects.Count; i++)
            {
                WarehouseObjectPlacement obj = layout.Objects[i];
                if (obj.IsContainer && GetRect(obj).Contains(cell))
                    return true;
            }

            return false;
        }

        private static bool HasContainerLowAt(WarehouseLayoutData layout, Vector2Int cell)
        {
            for (int i = 0; i < layout.Objects.Count; i++)
            {
                WarehouseObjectPlacement obj = layout.Objects[i];
                if (obj.Kind == WarehouseObjectKind.ContainerLow && GetRect(obj).Contains(cell))
                    return true;
            }

            return false;
        }

        private static bool HasContainerHighAt(WarehouseLayoutData layout, Vector2Int cell)
        {
            for (int i = 0; i < layout.Objects.Count; i++)
            {
                WarehouseObjectPlacement obj = layout.Objects[i];
                if (obj.Kind == WarehouseObjectKind.ContainerHigh && GetRect(obj).Contains(cell))
                    return true;
            }

            return false;
        }

        private static bool HasBridgeAt(WarehouseLayoutData layout, Vector2Int cell)
        {
            for (int i = 0; i < layout.Objects.Count; i++)
            {
                WarehouseObjectPlacement obj = layout.Objects[i];
                if (obj.Kind == WarehouseObjectKind.Bridge && GetRect(obj).Contains(cell))
                    return true;
            }

            return false;
        }

        private static bool HasLadderAt(WarehouseLayoutData layout, Vector2Int cell)
        {
            return GetLadderAt(layout, cell) != null;
        }

        private static WarehouseObjectPlacement GetLadderAt(WarehouseLayoutData layout, Vector2Int cell)
        {
            for (int i = 0; i < layout.Objects.Count; i++)
            {
                WarehouseObjectPlacement obj = layout.Objects[i];
                if (obj.IsLadder && GetRect(obj).Contains(cell))
                    return obj;
            }

            return null;
        }

        private static bool HasCoverAt(WarehouseLayoutData layout, Vector2Int cell, WarehousePlacementSurface surface)
        {
            return HasCoverAt(layout, cell, surface, null);
        }

        private static bool HasCoverAt(
            WarehouseLayoutData layout,
            Vector2Int cell,
            WarehousePlacementSurface surface,
            WarehouseObjectPlacement ignore)
        {
            return GetCoverAt(layout, cell, surface, ignore) != null;
        }

        private static WarehouseObjectPlacement GetCoverAt(
            WarehouseLayoutData layout,
            Vector2Int cell,
            WarehousePlacementSurface surface)
        {
            return GetCoverAt(layout, cell, surface, null);
        }

        private static WarehouseObjectPlacement GetCoverAt(
            WarehouseLayoutData layout,
            Vector2Int cell,
            WarehousePlacementSurface surface,
            WarehouseObjectPlacement ignore)
        {
            for (int i = 0; i < layout.Objects.Count; i++)
            {
                WarehouseObjectPlacement obj = layout.Objects[i];
                if (!ReferenceEquals(obj, ignore) &&
                    obj.IsCover &&
                    obj.Surface == surface &&
                    GetRect(obj).Contains(cell))
                    return obj;
            }

            return null;
        }

        private static bool HasAnyCoverAt(WarehouseLayoutData layout, Vector2Int cell)
        {
            return HasAnyCoverAt(layout, cell, null);
        }

        private static bool HasAnyCoverAt(
            WarehouseLayoutData layout,
            Vector2Int cell,
            WarehouseObjectPlacement ignore)
        {
            for (int i = 0; i < layout.Objects.Count; i++)
            {
                WarehouseObjectPlacement obj = layout.Objects[i];
                if (!ReferenceEquals(obj, ignore) && obj.IsCover && GetRect(obj).Contains(cell))
                    return true;
            }

            return false;
        }

        private static bool IsReserved(WarehouseLayoutData layout, bool[,] reservedGround, Vector2Int cell)
        {
            if (reservedGround == null)
                return false;

            Vector2Int sourceCell = cell.y < layout.Height / 2
                ? cell
                : new Vector2Int(layout.Width - cell.x - 1, layout.Height - cell.y - 1);
            return sourceCell.x >= 0 &&
                   sourceCell.x < reservedGround.GetLength(0) &&
                   sourceCell.y >= 0 &&
                   sourceCell.y < reservedGround.GetLength(1) &&
                   reservedGround[sourceCell.x, sourceCell.y];
        }

        private static Vector2Int RotationToVector(float rotationY)
        {
            float angle = Mathf.Repeat(rotationY, 360f);
            if (Mathf.Abs(Mathf.DeltaAngle(angle, 90f)) <= 45f)
                return Vector2Int.right;
            if (Mathf.Abs(Mathf.DeltaAngle(angle, 180f)) <= 45f)
                return Vector2Int.down;
            if (Mathf.Abs(Mathf.DeltaAngle(angle, 270f)) <= 45f)
                return Vector2Int.left;

            return Vector2Int.up;
        }

        private static Vector2Int DirectionToVector(WarehouseDirection direction)
        {
            return direction switch
            {
                WarehouseDirection.North => Vector2Int.up,
                WarehouseDirection.South => Vector2Int.down,
                WarehouseDirection.East => Vector2Int.right,
                WarehouseDirection.West => Vector2Int.left,
                _ => Vector2Int.zero
            };
        }

        private static float DirectionToRotation(WarehouseDirection direction)
        {
            return direction switch
            {
                WarehouseDirection.North => 0f,
                WarehouseDirection.South => 180f,
                WarehouseDirection.East => 90f,
                WarehouseDirection.West => 270f,
                _ => 0f
            };
        }

        private static WarehouseDirection RotationToDirection(float rotationY)
        {
            float angle = Mathf.Repeat(rotationY, 360f);
            if (Mathf.Abs(Mathf.DeltaAngle(angle, 90f)) <= 45f)
                return WarehouseDirection.East;
            if (Mathf.Abs(Mathf.DeltaAngle(angle, 180f)) <= 45f)
                return WarehouseDirection.South;
            if (Mathf.Abs(Mathf.DeltaAngle(angle, 270f)) <= 45f)
                return WarehouseDirection.West;

            return WarehouseDirection.North;
        }

        private static RectInt GetRect(WarehouseObjectPlacement obj)
        {
            return new RectInt(obj.Origin.x, obj.Origin.y, obj.Size.x, obj.Size.y);
        }

        private static Vector2Int GridPointToCell(Vector2 position)
        {
            return new Vector2Int(
                Mathf.Clamp(Mathf.FloorToInt(position.x), 0, int.MaxValue),
                Mathf.Clamp(Mathf.FloorToInt(position.y), 0, int.MaxValue));
        }

        private readonly struct FillCandidate
        {
            public readonly Vector2Int Cell;
            public readonly WarehouseObjectKind Kind;

            public FillCandidate(Vector2Int cell, WarehouseObjectKind kind)
            {
                Cell = cell;
                Kind = kind;
            }
        }
    }
}
