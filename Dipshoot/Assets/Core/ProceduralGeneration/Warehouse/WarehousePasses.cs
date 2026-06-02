using System.Collections.Generic;
using UnityEngine;

namespace Game.ProcGen.Warehouse
{
    public sealed class WarehouseLayoutPass : ProcGenPass
    {
        public override string Id => "warehouse-layout";

        public override void Declare(GenerationPassContract contract)
        {
            contract.Write(WarehouseKeys.Layout);
        }

        public override void Execute(GenerationContext context)
        {
            WarehouseRecipe recipe = context.GetRecipe<WarehouseRecipe>();
            System.Random random = context.CreateRandom(Id);
            int width = recipe.Width;
            int height = recipe.Height;
            int halfHeight = height / 2;
            int centerX = Mathf.Clamp(Mathf.FloorToInt(recipe.SpawnA.x), 1, width - 2);
            int leftLaneX = Mathf.Clamp(centerX - random.Next(3, 6), 1, width - 2);
            int rightLaneX = Mathf.Clamp(centerX + random.Next(3, 6), 1, width - 2);
            if (rightLaneX - leftLaneX < 4)
            {
                leftLaneX = Mathf.Clamp(centerX - 4, 1, width - 2);
                rightLaneX = Mathf.Clamp(centerX + 4, 1, width - 2);
            }

            int lowerConnectorY = Mathf.Clamp(random.Next(4, 7), 1, Mathf.Max(1, halfHeight - 2));
            int upperConnectorY = Mathf.Clamp(random.Next(10, 13), lowerConnectorY + 1, Mathf.Max(lowerConnectorY + 1, halfHeight - 1));
            context.Blackboard.Set(WarehouseKeys.Layout, new WarehouseLayoutData
            {
                Width = width,
                Height = height,
                CellSize = recipe.GridCellSize,
                CellSizeX = recipe.GridCellSizeX,
                CellSizeZ = recipe.GridCellSizeZ,
                SpawnA = recipe.SpawnA,
                SpawnB = recipe.SpawnB,
                CapturePoint = new Vector2(4f + (float)random.NextDouble() * 7f, recipe.CapturePoint.y),
                SpawnClearRadius = recipe.SpawnClearRadius,
                CaptureClearRadius = recipe.CaptureClearRadius,
                LeftLaneX = leftLaneX,
                RightLaneX = rightLaneX,
                LowerConnectorY = lowerConnectorY,
                UpperConnectorY = upperConnectorY
            });
        }
    }

    internal static class WarehouseGenerationUtility
    {
        private const int GroundCorridorWidth = 2;

        public static readonly Vector2Int[] CardinalDirections =
        {
            new(0, 1),
            new(0, -1),
            new(1, 0),
            new(-1, 0)
        };

        public static bool IsCellInSourceHalf(WarehouseLayoutData layout, Vector2Int cell)
        {
            return layout.IsInside(cell) && cell.y < layout.Height / 2;
        }

        public static bool[,] BuildReservedGroundMask(WarehouseLayoutData layout)
        {
            bool[,] reserved = new bool[layout.Width, layout.Height];
            int centerX = Mathf.Clamp(Mathf.FloorToInt(layout.SpawnA.x), 1, layout.Width - 2);
            int leftLaneX = layout.LeftLaneX > 0 ? Mathf.Clamp(layout.LeftLaneX, 1, layout.Width - 2) : Mathf.Clamp(centerX - 4, 1, layout.Width - 2);
            int rightLaneX = layout.RightLaneX > 0 ? Mathf.Clamp(layout.RightLaneX, 1, layout.Width - 2) : Mathf.Clamp(centerX + 4, 1, layout.Width - 2);
            int captureX = Mathf.Clamp(Mathf.FloorToInt(layout.CapturePoint.x), 1, layout.Width - 2);
            int mirroredCaptureX = Mathf.Clamp(layout.Width - captureX - 1, 1, layout.Width - 2);
            int lowerY = layout.LowerConnectorY > 0
                ? Mathf.Clamp(layout.LowerConnectorY, 1, Mathf.Max(1, layout.Height / 2 - 2))
                : Mathf.Clamp(Mathf.CeilToInt(layout.SpawnA.y + layout.SpawnClearRadius), 1, Mathf.Max(1, layout.Height / 2 - 2));
            int upperY = layout.UpperConnectorY > 0
                ? Mathf.Clamp(layout.UpperConnectorY, lowerY + 1, Mathf.Max(lowerY + 1, layout.Height / 2 - 1))
                : Mathf.Clamp(Mathf.FloorToInt(layout.CapturePoint.y - layout.CaptureClearRadius), lowerY + 1, Mathf.Max(lowerY + 1, layout.Height / 2 - 1));

            if (leftLaneX > rightLaneX)
                (leftLaneX, rightLaneX) = (rightLaneX, leftLaneX);

            for (int y = Mathf.FloorToInt(layout.SpawnA.y); y <= lowerY; y++)
                ReserveCorridorCell(layout, reserved, centerX, y, true);

            for (int y = lowerY; y <= upperY; y++)
            {
                ReserveCorridorCell(layout, reserved, leftLaneX, y, true);
                ReserveCorridorCell(layout, reserved, rightLaneX, y, true);
            }

            for (int x = leftLaneX; x <= rightLaneX; x++)
            {
                ReserveCorridorCell(layout, reserved, x, lowerY, false);
                ReserveCorridorCell(layout, reserved, x, upperY, false);
            }

            ReserveCaptureApproach(layout, reserved, leftLaneX, rightLaneX, captureX, upperY);
            ReserveCaptureApproach(layout, reserved, leftLaneX, rightLaneX, mirroredCaptureX, upperY);
            return reserved;
        }

        public static WarehouseDirectionMask DirectionToMask(WarehouseDirection direction)
        {
            return direction switch
            {
                WarehouseDirection.North => WarehouseDirectionMask.North,
                WarehouseDirection.South => WarehouseDirectionMask.South,
                WarehouseDirection.East => WarehouseDirectionMask.East,
                WarehouseDirection.West => WarehouseDirectionMask.West,
                _ => WarehouseDirectionMask.None
            };
        }

        public static bool HasDirection(WarehouseDirectionMask mask, WarehouseDirection direction)
        {
            return (mask & DirectionToMask(direction)) != 0;
        }

        public static int CountDirections(WarehouseDirectionMask mask)
        {
            int count = 0;
            if ((mask & WarehouseDirectionMask.North) != 0)
                count++;
            if ((mask & WarehouseDirectionMask.South) != 0)
                count++;
            if ((mask & WarehouseDirectionMask.East) != 0)
                count++;
            if ((mask & WarehouseDirectionMask.West) != 0)
                count++;

            return count;
        }

        public static WarehouseDirection PrimaryDirection(WarehouseDirectionMask mask)
        {
            if ((mask & WarehouseDirectionMask.North) != 0)
                return WarehouseDirection.North;
            if ((mask & WarehouseDirectionMask.East) != 0)
                return WarehouseDirection.East;
            if ((mask & WarehouseDirectionMask.South) != 0)
                return WarehouseDirection.South;

            return WarehouseDirection.West;
        }

        public static int CountOccupied(bool[,] occupied)
        {
            int count = 0;
            for (int y = 0; y < occupied.GetLength(1); y++)
            {
                for (int x = 0; x < occupied.GetLength(0); x++)
                {
                    if (occupied[x, y])
                        count++;
                }
            }

            return count;
        }

        public static void Shuffle<T>(IList<T> items, System.Random random)
        {
            for (int i = items.Count - 1; i > 0; i--)
            {
                int swapIndex = random.Next(i + 1);
                (items[i], items[swapIndex]) = (items[swapIndex], items[i]);
            }
        }

        private static void ReserveCorridorCell(WarehouseLayoutData layout, bool[,] reserved, int x, int y, bool vertical)
        {
            int halfWidth = GroundCorridorWidth / 2;
            for (int offset = 0; offset < GroundCorridorWidth; offset++)
            {
                int laneOffset = offset - halfWidth;
                int reserveX = vertical ? x + laneOffset : x;
                int reserveY = vertical ? y : y + laneOffset;
                ReserveCell(layout, reserved, reserveX, reserveY);
            }
        }

        public static void ReserveCell(WarehouseLayoutData layout, bool[,] reserved, int x, int y)
        {
            Vector2Int cell = new(x, y);
            if (IsCellInSourceHalf(layout, cell))
                reserved[x, y] = true;
        }

        private static void ReserveCaptureApproach(WarehouseLayoutData layout, bool[,] reserved, int leftLaneX, int rightLaneX, int captureX, int upperY)
        {
            int nearestLaneX = Mathf.Abs(captureX - leftLaneX) <= Mathf.Abs(captureX - rightLaneX)
                ? leftLaneX
                : rightLaneX;
            int minX = Mathf.Min(nearestLaneX, captureX);
            int maxX = Mathf.Max(nearestLaneX, captureX);
            for (int x = minX; x <= maxX; x++)
                ReserveCorridorCell(layout, reserved, x, upperY, false);

            for (int y = upperY; y < layout.Height / 2; y++)
                ReserveCorridorCell(layout, reserved, captureX, y, true);
        }
    }

    public sealed class WarehouseStructureGenerationPass : ProcGenPass
    {
        public override string Id => "warehouse-structure-generation";

        public override void Declare(GenerationPassContract contract)
        {
            contract.Read(WarehouseKeys.Layout);
            contract.Write(WarehouseKeys.Layout);
        }

        public override void Execute(GenerationContext context)
        {
            WarehouseRecipe recipe = context.GetRecipe<WarehouseRecipe>();
            WarehouseLayoutData layout = context.Blackboard.GetRequired(WarehouseKeys.Layout);
            System.Random random = context.CreateRandom(Id);
            bool[,] reservedGround = WarehouseGenerationUtility.BuildReservedGroundMask(layout);
            bool[,] occupied = new bool[layout.Width, layout.Height];
            int targetStructureCells = GetTargetSourceHalfStructureCells(recipe, layout, reservedGround, occupied);

            AddAnchorContainers(layout, reservedGround, occupied);
            FillClusteredContainers(layout, recipe, random, reservedGround, occupied, targetStructureCells);
            AddBridges(layout, recipe, random, reservedGround, occupied);
            AddLadders(layout, recipe, random, reservedGround, occupied);
            int inaccessibleComponents = EnsureTopComponentAccess(layout, recipe, random, reservedGround, occupied);

            int placed = WarehouseGenerationUtility.CountOccupied(occupied);
            CountSourceHalfStructures(layout, out int lowContainers, out int highContainers, out int bridges);
            if (placed < targetStructureCells)
                context.Diagnostics.Warning($"Placed {placed}/{targetStructureCells} source-half structure cells.", Id);
            else
                context.Diagnostics.Info($"Placed {placed} source-half structure cells before symmetry.", Id);

            context.Diagnostics.Info($"Source-half structures: low={lowContainers}, high={highContainers}, bridges={bridges}.", Id);
            if (inaccessibleComponents > 0)
                context.Diagnostics.Warning($"Could not add ladder access for {inaccessibleComponents} top components.", Id);
        }

        private static int GetTargetSourceHalfStructureCells(
            WarehouseRecipe recipe,
            WarehouseLayoutData layout,
            bool[,] reservedGround,
            bool[,] occupied)
        {
            int totalTargetCells = Mathf.RoundToInt(layout.Width * layout.Height * recipe.InitialStructureRatio);
            int sourceHalfTargetCells = Mathf.Max(0, totalTargetCells / 2);
            int validCells = 0;

            for (int y = 0; y < layout.Height / 2; y++)
            {
                for (int x = 0; x < layout.Width; x++)
                {
                    if (CanPlaceStructure(layout, reservedGround, occupied, new Vector2Int(x, y)))
                        validCells++;
                }
            }

            return Mathf.Min(sourceHalfTargetCells, validCells);
        }

        private static void AddAnchorContainers(WarehouseLayoutData layout, bool[,] reservedGround, bool[,] occupied)
        {
            TryAddContainer(layout, reservedGround, occupied, WarehouseObjectKind.ContainerLow, new Vector2Int(1, 5));
            TryAddContainer(layout, reservedGround, occupied, WarehouseObjectKind.ContainerLow, new Vector2Int(layout.Width - 2, 5));
            TryAddContainer(layout, reservedGround, occupied, WarehouseObjectKind.ContainerLow, new Vector2Int(4, 8));
            TryAddContainer(layout, reservedGround, occupied, WarehouseObjectKind.ContainerLow, new Vector2Int(9, 10));
            TryAddContainer(layout, reservedGround, occupied, WarehouseObjectKind.ContainerHigh, new Vector2Int(7, 6));
            TryAddContainer(layout, reservedGround, occupied, WarehouseObjectKind.ContainerLow, new Vector2Int(2, 12));
            TryAddContainer(layout, reservedGround, occupied, WarehouseObjectKind.ContainerLow, new Vector2Int(layout.Width - 4, 12));
        }

        private static void FillClusteredContainers(
            WarehouseLayoutData layout,
            WarehouseRecipe recipe,
            System.Random random,
            bool[,] reservedGround,
            bool[,] occupied,
            int targetStructureCells)
        {
            int placed = WarehouseGenerationUtility.CountOccupied(occupied);
            int attempts = 0;
            while (placed < targetStructureCells && attempts < recipe.StructurePlacementAttemptCount)
            {
                attempts++;
                Vector2Int clusterCenter = new(
                    random.Next(1, layout.Width - 1),
                    random.Next(3, Mathf.Max(4, layout.Height / 2)));
                int clusterSize = random.Next(4, 11);

                for (int i = 0; i < clusterSize && placed < targetStructureCells; i++)
                {
                    Vector2Int cell = clusterCenter + new Vector2Int(random.Next(-2, 3), random.Next(-2, 3));
                    WarehouseObjectKind kind = random.NextDouble() < recipe.TallContainerProbability
                        ? WarehouseObjectKind.ContainerHigh
                        : WarehouseObjectKind.ContainerLow;

                    if (TryAddContainer(layout, reservedGround, occupied, kind, cell))
                        placed++;
                }
            }

            if (placed >= targetStructureCells)
                return;

            List<Vector2Int> candidates = new();
            for (int y = 0; y < layout.Height / 2; y++)
            {
                for (int x = 0; x < layout.Width; x++)
                {
                    Vector2Int cell = new(x, y);
                    if (CanPlaceStructure(layout, reservedGround, occupied, cell))
                        candidates.Add(cell);
                }
            }

            WarehouseGenerationUtility.Shuffle(candidates, random);
            for (int i = 0; i < candidates.Count && placed < targetStructureCells; i++)
            {
                WarehouseObjectKind kind = random.NextDouble() < recipe.TallContainerProbability
                    ? WarehouseObjectKind.ContainerHigh
                    : WarehouseObjectKind.ContainerLow;

                if (TryAddContainer(layout, reservedGround, occupied, kind, candidates[i]))
                    placed++;
            }
        }

        private static void AddBridges(
            WarehouseLayoutData layout,
            WarehouseRecipe recipe,
            System.Random random,
            bool[,] reservedGround,
            bool[,] occupied)
        {
            if (recipe.MaxBridgeCellCount <= 0)
                return;

            int targetBridges = Mathf.Clamp(
                Mathf.RoundToInt(WarehouseGenerationUtility.CountOccupied(occupied) * recipe.BridgeProbability),
                Mathf.Min(2, recipe.MaxBridgeCellCount),
                recipe.MaxBridgeCellCount);
            int placed = 0;
            int targetPairs = Mathf.Min(recipe.MaxBridgePairCount, targetBridges / 2);
            if (targetPairs > 0)
                placed += AddBridgePairCandidates(layout, recipe, random, reservedGround, occupied, targetPairs) * 2;

            List<Vector2Int> corridorCandidates = new();
            List<Vector2Int> otherCandidates = new();
            for (int y = 0; y < layout.Height / 2; y++)
            {
                for (int x = 0; x < layout.Width; x++)
                {
                    Vector2Int cell = new(x, y);
                    if (CanPlaceBridge(layout, recipe, reservedGround, occupied, cell))
                    {
                        if (reservedGround[x, y])
                            corridorCandidates.Add(cell);
                        else
                            otherCandidates.Add(cell);
                    }
                }
            }

            WarehouseGenerationUtility.Shuffle(corridorCandidates, random);
            WarehouseGenerationUtility.Shuffle(otherCandidates, random);
            placed += AddBridgeCandidates(layout, recipe, reservedGround, occupied, corridorCandidates, targetBridges - placed);
            if (placed < targetBridges)
                AddBridgeCandidates(layout, recipe, reservedGround, occupied, otherCandidates, targetBridges - placed);
        }

        private static int AddBridgePairCandidates(
            WarehouseLayoutData layout,
            WarehouseRecipe recipe,
            System.Random random,
            bool[,] reservedGround,
            bool[,] occupied,
            int maxPairs)
        {
            List<BridgePairCandidate> corridorCandidates = new();
            List<BridgePairCandidate> otherCandidates = new();
            for (int y = 0; y < layout.Height / 2; y++)
            {
                for (int x = 0; x < layout.Width; x++)
                {
                    Vector2Int cell = new(x, y);
                    AddBridgePairCandidate(layout, recipe, reservedGround, occupied, corridorCandidates, otherCandidates, cell, horizontal: true);
                    AddBridgePairCandidate(layout, recipe, reservedGround, occupied, corridorCandidates, otherCandidates, cell, horizontal: false);
                }
            }

            WarehouseGenerationUtility.Shuffle(corridorCandidates, random);
            WarehouseGenerationUtility.Shuffle(otherCandidates, random);
            int placed = TryAddBridgePairCandidates(layout, recipe, reservedGround, occupied, corridorCandidates, maxPairs);
            if (placed < maxPairs)
                placed += TryAddBridgePairCandidates(layout, recipe, reservedGround, occupied, otherCandidates, maxPairs - placed);

            return placed;
        }

        private static void AddBridgePairCandidate(
            WarehouseLayoutData layout,
            WarehouseRecipe recipe,
            bool[,] reservedGround,
            bool[,] occupied,
            List<BridgePairCandidate> corridorCandidates,
            List<BridgePairCandidate> otherCandidates,
            Vector2Int cell,
            bool horizontal)
        {
            if (!CanPlaceBridgePair(layout, recipe, reservedGround, occupied, cell, horizontal))
                return;

            Vector2Int second = cell + (horizontal ? Vector2Int.right : Vector2Int.up);
            if (reservedGround[cell.x, cell.y] || reservedGround[second.x, second.y])
                corridorCandidates.Add(new BridgePairCandidate(cell, horizontal));
            else
                otherCandidates.Add(new BridgePairCandidate(cell, horizontal));
        }

        private static int TryAddBridgePairCandidates(
            WarehouseLayoutData layout,
            WarehouseRecipe recipe,
            bool[,] reservedGround,
            bool[,] occupied,
            List<BridgePairCandidate> candidates,
            int maxPairs)
        {
            int placed = 0;
            for (int i = 0; i < candidates.Count && placed < maxPairs; i++)
            {
                BridgePairCandidate candidate = candidates[i];
                if (TryAddBridgePair(layout, recipe, reservedGround, occupied, candidate.First, candidate.Horizontal))
                    placed++;
            }

            return placed;
        }

        private static int AddBridgeCandidates(
            WarehouseLayoutData layout,
            WarehouseRecipe recipe,
            bool[,] reservedGround,
            bool[,] occupied,
            List<Vector2Int> candidates,
            int maxCount)
        {
            int placed = 0;
            for (int i = 0; i < candidates.Count && placed < maxCount; i++)
            {
                if (TryAddBridge(layout, recipe, reservedGround, occupied, candidates[i]))
                    placed++;
            }

            return placed;
        }

        private static bool TryAddBridgePair(
            WarehouseLayoutData layout,
            WarehouseRecipe recipe,
            bool[,] reservedGround,
            bool[,] occupied,
            Vector2Int first,
            bool horizontal)
        {
            if (!CanPlaceBridgePair(layout, recipe, reservedGround, occupied, first, horizontal))
                return false;

            Vector2Int second = first + (horizontal ? Vector2Int.right : Vector2Int.up);
            AddBridge(layout, first, GetStraightBridgeMask(horizontal), WarehouseBridgeConnectionType.Straight);
            AddBridge(layout, second, GetStraightBridgeMask(horizontal), WarehouseBridgeConnectionType.Straight);
            occupied[first.x, first.y] = true;
            occupied[second.x, second.y] = true;
            return true;
        }

        private static bool TryAddBridge(
            WarehouseLayoutData layout,
            WarehouseRecipe recipe,
            bool[,] reservedGround,
            bool[,] occupied,
            Vector2Int cell)
        {
            if (!CanPlaceBridge(layout, recipe, reservedGround, occupied, cell))
                return false;

            WarehouseDirectionMask mask = GetBridgeConnectionMask(layout, cell);
            WarehouseBridgeConnectionType type = WarehouseGenerationUtility.CountDirections(mask) == 3
                ? WarehouseBridgeConnectionType.ThreeWay
                : WarehouseBridgeConnectionType.Straight;
            AddBridge(layout, cell, mask, type);
            occupied[cell.x, cell.y] = true;
            return true;
        }

        private static void AddBridge(
            WarehouseLayoutData layout,
            Vector2Int cell,
            WarehouseDirectionMask connectionMask,
            WarehouseBridgeConnectionType connectionType)
        {
            layout.Objects.Add(new WarehouseObjectPlacement
            {
                Kind = WarehouseObjectKind.Bridge,
                Side = WarehouseSide.A,
                Origin = cell,
                Size = Vector2Int.one,
                Direction = WarehouseGenerationUtility.PrimaryDirection(connectionMask),
                ConnectionMask = connectionMask,
                BridgeConnectionType = connectionType,
                RotationY = 0f
            });
        }

        private static bool CanPlaceBridge(
            WarehouseLayoutData layout,
            WarehouseRecipe recipe,
            bool[,] reservedGround,
            bool[,] occupied,
            Vector2Int cell)
        {
            if (!CanPlaceBridgeCell(layout, recipe, reservedGround, occupied, cell))
                return false;

            return IsValidBridgeConnectionMask(GetBridgeConnectionMask(layout, cell));
        }

        private static bool CanPlaceBridgePair(
            WarehouseLayoutData layout,
            WarehouseRecipe recipe,
            bool[,] reservedGround,
            bool[,] occupied,
            Vector2Int first,
            bool horizontal)
        {
            Vector2Int second = first + (horizontal ? Vector2Int.right : Vector2Int.up);
            if (!CanPlaceBridgeCell(layout, recipe, reservedGround, occupied, first) ||
                !CanPlaceBridgeCell(layout, recipe, reservedGround, occupied, second))
            {
                return false;
            }

            return horizontal
                ? HasBridgeConnectorAt(layout, first + Vector2Int.left) && HasBridgeConnectorAt(layout, second + Vector2Int.right)
                : HasBridgeConnectorAt(layout, first + Vector2Int.down) && HasBridgeConnectorAt(layout, second + Vector2Int.up);
        }

        private static bool CanPlaceBridgeCell(
            WarehouseLayoutData layout,
            WarehouseRecipe recipe,
            bool[,] reservedGround,
            bool[,] occupied,
            Vector2Int cell)
        {
            if (!WarehouseGenerationUtility.IsCellInSourceHalf(layout, cell) ||
                occupied[cell.x, cell.y] ||
                CountContainerSides(layout, cell) >= recipe.MaxBridgeBlockedSides)
            {
                return false;
            }

            WarehouseObjectPlacement probe = new()
            {
                Kind = WarehouseObjectKind.Bridge,
                Side = WarehouseSide.A,
                Origin = cell,
                Size = Vector2Int.one
            };

            return !WarehouseRepairPass.ObjectTouchesRadius(probe, layout.SpawnA, layout.SpawnClearRadius) &&
                   !WarehouseRepairPass.ObjectTouchesRadius(probe, layout.CapturePoint, layout.CaptureClearRadius);
        }

        private static WarehouseDirectionMask GetStraightBridgeMask(bool horizontal)
        {
            return horizontal
                ? WarehouseDirectionMask.East | WarehouseDirectionMask.West
                : WarehouseDirectionMask.North | WarehouseDirectionMask.South;
        }

        private static WarehouseDirectionMask GetBridgeConnectionMask(WarehouseLayoutData layout, Vector2Int cell)
        {
            WarehouseDirectionMask mask = WarehouseDirectionMask.None;
            if (HasBridgeConnectorAt(layout, cell + Vector2Int.up))
                mask |= WarehouseDirectionMask.North;
            if (HasBridgeConnectorAt(layout, cell + Vector2Int.down))
                mask |= WarehouseDirectionMask.South;
            if (HasBridgeConnectorAt(layout, cell + Vector2Int.right))
                mask |= WarehouseDirectionMask.East;
            if (HasBridgeConnectorAt(layout, cell + Vector2Int.left))
                mask |= WarehouseDirectionMask.West;

            return mask;
        }

        private static bool IsValidBridgeConnectionMask(WarehouseDirectionMask mask)
        {
            if (WarehouseGenerationUtility.CountDirections(mask) == 3)
                return true;

            bool horizontal = (mask & (WarehouseDirectionMask.East | WarehouseDirectionMask.West)) ==
                              (WarehouseDirectionMask.East | WarehouseDirectionMask.West);
            bool vertical = (mask & (WarehouseDirectionMask.North | WarehouseDirectionMask.South)) ==
                            (WarehouseDirectionMask.North | WarehouseDirectionMask.South);
            return horizontal || vertical;
        }

        private static int CountContainerSides(WarehouseLayoutData layout, Vector2Int cell)
        {
            int count = 0;
            for (int i = 0; i < WarehouseGenerationUtility.CardinalDirections.Length; i++)
            {
                if (HasContainerAt(layout, cell + WarehouseGenerationUtility.CardinalDirections[i]))
                    count++;
            }

            return count;
        }

        private static bool HasBridgeConnectorAt(WarehouseLayoutData layout, Vector2Int cell)
        {
            return HasPlayableTopAt(layout, cell);
        }

        private static bool HasContainerAt(WarehouseLayoutData layout, Vector2Int cell)
        {
            if (!layout.IsInside(cell))
                return false;

            for (int i = 0; i < layout.Objects.Count; i++)
            {
                WarehouseObjectPlacement obj = layout.Objects[i];
                if (!obj.IsContainer)
                    continue;

                RectInt rect = new(obj.Origin.x, obj.Origin.y, obj.Size.x, obj.Size.y);
                if (rect.Contains(cell))
                    return true;
            }

            return false;
        }

        private static bool HasPlayableTopAt(WarehouseLayoutData layout, Vector2Int cell)
        {
            if (!layout.IsInside(cell))
                return false;

            for (int i = 0; i < layout.Objects.Count; i++)
            {
                WarehouseObjectPlacement obj = layout.Objects[i];
                if (!obj.IsTopWalkableSource)
                    continue;

                RectInt rect = new(obj.Origin.x, obj.Origin.y, obj.Size.x, obj.Size.y);
                if (rect.Contains(cell))
                    return true;
            }

            return false;
        }

        private static void CountSourceHalfStructures(
            WarehouseLayoutData layout,
            out int lowContainers,
            out int highContainers,
            out int bridges)
        {
            lowContainers = 0;
            highContainers = 0;
            bridges = 0;
            for (int i = 0; i < layout.Objects.Count; i++)
            {
                WarehouseObjectPlacement obj = layout.Objects[i];
                if (obj.Side != WarehouseSide.A)
                    continue;

                if (obj.Kind == WarehouseObjectKind.ContainerLow)
                    lowContainers++;
                else if (obj.Kind == WarehouseObjectKind.ContainerHigh)
                    highContainers++;
                else if (obj.Kind == WarehouseObjectKind.Bridge)
                    bridges++;
            }
        }

        private static bool TryAddContainer(
            WarehouseLayoutData layout,
            bool[,] reservedGround,
            bool[,] occupied,
            WarehouseObjectKind kind,
            Vector2Int cell,
            float rotationY = 0f)
        {
            if (!CanPlaceStructure(layout, reservedGround, occupied, cell))
                return false;

            layout.Objects.Add(new WarehouseObjectPlacement
            {
                Kind = kind,
                Side = WarehouseSide.A,
                Origin = cell,
                Size = Vector2Int.one,
                RotationY = rotationY
            });
            occupied[cell.x, cell.y] = true;
            return true;
        }

        private static bool CanPlaceStructure(
            WarehouseLayoutData layout,
            bool[,] reservedGround,
            bool[,] occupied,
            Vector2Int cell)
        {
            if (!WarehouseGenerationUtility.IsCellInSourceHalf(layout, cell) ||
                reservedGround[cell.x, cell.y] ||
                occupied[cell.x, cell.y])
            {
                return false;
            }

            WarehouseObjectPlacement probe = new()
            {
                Kind = WarehouseObjectKind.ContainerLow,
                Side = WarehouseSide.A,
                Origin = cell,
                Size = Vector2Int.one
            };

            return !WarehouseRepairPass.ObjectTouchesRadius(probe, layout.SpawnA, layout.SpawnClearRadius) &&
                   !WarehouseRepairPass.ObjectTouchesRadius(probe, layout.CapturePoint, layout.CaptureClearRadius);
        }

        private static void AddLadders(
            WarehouseLayoutData layout,
            WarehouseRecipe recipe,
            System.Random random,
            bool[,] reservedGround,
            bool[,] structureOccupied)
        {
            List<Vector2Int> structureCells = new();
            for (int y = 0; y < layout.Height / 2; y++)
            {
                for (int x = 0; x < layout.Width; x++)
                {
                    Vector2Int cell = new(x, y);
                    if (structureOccupied[x, y] && HasContainerLowAt(layout, cell))
                        structureCells.Add(cell);
                }
            }

            WarehouseGenerationUtility.Shuffle(structureCells, random);
            bool[,] ladderOccupied = new bool[layout.Width, layout.Height];
            int targetLadders = Mathf.Max(2, structureCells.Count / recipe.LadderSpacing);
            int placed = 0;
            for (int i = 0; i < structureCells.Count && placed < targetLadders; i++)
            {
                if (TryAddLadderForStructure(layout, recipe, random, reservedGround, structureOccupied, ladderOccupied, structureCells[i]))
                    placed++;
            }
        }

        private static int EnsureTopComponentAccess(
            WarehouseLayoutData layout,
            WarehouseRecipe recipe,
            System.Random random,
            bool[,] reservedGround,
            bool[,] structureOccupied)
        {
            bool[,] ladderOccupied = BuildLadderOccupancy(layout);
            List<List<Vector2Int>> components = BuildTopComponents(layout, sourceHalfOnly: true);
            int inaccessible = 0;
            for (int i = 0; i < components.Count; i++)
            {
                List<Vector2Int> component = components[i];
                if (component.Count < recipe.MinTopComponentSize || HasLadderAccess(layout, component))
                    continue;

                if (!TryAddLadderForComponent(layout, recipe, random, reservedGround, structureOccupied, ladderOccupied, component))
                    inaccessible++;
            }

            return inaccessible;
        }

        private static bool TryAddLadderForComponent(
            WarehouseLayoutData layout,
            WarehouseRecipe recipe,
            System.Random random,
            bool[,] reservedGround,
            bool[,] structureOccupied,
            bool[,] ladderOccupied,
            List<Vector2Int> component)
        {
            List<Vector2Int> lowContainerCells = new();
            for (int i = 0; i < component.Count; i++)
            {
                Vector2Int cell = component[i];
                if (HasContainerLowAt(layout, cell))
                    lowContainerCells.Add(cell);
            }

            WarehouseGenerationUtility.Shuffle(lowContainerCells, random);
            for (int i = 0; i < lowContainerCells.Count; i++)
            {
                if (TryAddLadderForStructure(layout, recipe, random, reservedGround, structureOccupied, ladderOccupied, lowContainerCells[i]))
                    return true;
            }

            return false;
        }

        private static bool HasLadderAccess(WarehouseLayoutData layout, List<Vector2Int> component)
        {
            HashSet<Vector2Int> cells = new(component);
            for (int i = 0; i < layout.Objects.Count; i++)
            {
                WarehouseObjectPlacement obj = layout.Objects[i];
                if (!obj.IsLadder || obj.Side != WarehouseSide.A)
                    continue;

                if (cells.Contains(obj.Origin + DirectionToVector(obj.Direction)))
                    return true;
            }

            return false;
        }

        private static List<List<Vector2Int>> BuildTopComponents(WarehouseLayoutData layout, bool sourceHalfOnly)
        {
            bool[,] topWalkable = new bool[layout.Width, layout.Height];
            for (int i = 0; i < layout.Objects.Count; i++)
            {
                WarehouseObjectPlacement obj = layout.Objects[i];
                if (!obj.IsTopWalkableSource || (sourceHalfOnly && obj.Side != WarehouseSide.A))
                    continue;

                for (int y = 0; y < obj.Size.y; y++)
                {
                    for (int x = 0; x < obj.Size.x; x++)
                    {
                        Vector2Int cell = new(obj.Origin.x + x, obj.Origin.y + y);
                        if (layout.IsInside(cell) && (!sourceHalfOnly || WarehouseGenerationUtility.IsCellInSourceHalf(layout, cell)))
                            topWalkable[cell.x, cell.y] = true;
                    }
                }
            }

            bool[,] visited = new bool[layout.Width, layout.Height];
            List<List<Vector2Int>> components = new();
            Queue<Vector2Int> queue = new();
            for (int y = 0; y < layout.Height; y++)
            {
                for (int x = 0; x < layout.Width; x++)
                {
                    if (!topWalkable[x, y] || visited[x, y])
                        continue;

                    List<Vector2Int> component = new();
                    Vector2Int start = new(x, y);
                    visited[x, y] = true;
                    queue.Enqueue(start);
                    while (queue.Count > 0)
                    {
                        Vector2Int cell = queue.Dequeue();
                        component.Add(cell);
                        for (int i = 0; i < WarehouseGenerationUtility.CardinalDirections.Length; i++)
                        {
                            Vector2Int next = cell + WarehouseGenerationUtility.CardinalDirections[i];
                            if (!layout.IsInside(next) || !topWalkable[next.x, next.y] || visited[next.x, next.y])
                                continue;

                            visited[next.x, next.y] = true;
                            queue.Enqueue(next);
                        }
                    }

                    components.Add(component);
                }
            }

            return components;
        }

        private static bool[,] BuildLadderOccupancy(WarehouseLayoutData layout)
        {
            bool[,] occupied = new bool[layout.Width, layout.Height];
            for (int i = 0; i < layout.Objects.Count; i++)
            {
                WarehouseObjectPlacement obj = layout.Objects[i];
                if (obj.Side == WarehouseSide.A && obj.IsLadder && WarehouseGenerationUtility.IsCellInSourceHalf(layout, obj.Origin))
                    occupied[obj.Origin.x, obj.Origin.y] = true;
            }

            return occupied;
        }

        private static bool TryAddLadderForStructure(
            WarehouseLayoutData layout,
            WarehouseRecipe recipe,
            System.Random random,
            bool[,] reservedGround,
            bool[,] structureOccupied,
            bool[,] ladderOccupied,
            Vector2Int structureCell)
        {
            WarehouseDirection[] directions =
            {
                WarehouseDirection.North,
                WarehouseDirection.South,
                WarehouseDirection.East,
                WarehouseDirection.West
            };
            WarehouseGenerationUtility.Shuffle(directions, random);

            for (int i = 0; i < directions.Length; i++)
            {
                WarehouseDirection direction = directions[i];
                Vector2Int groundCell = structureCell - DirectionToVector(direction);
                if (!WarehouseGenerationUtility.IsCellInSourceHalf(layout, groundCell) ||
                    reservedGround[groundCell.x, groundCell.y] ||
                    structureOccupied[groundCell.x, groundCell.y] ||
                    ladderOccupied[groundCell.x, groundCell.y] ||
                    !HasGroundApproach(layout, structureOccupied, ladderOccupied, groundCell, structureCell))
                {
                    continue;
                }

                AddLadder(layout, recipe, random, groundCell, direction);
                ladderOccupied[groundCell.x, groundCell.y] = true;
                return true;
            }

            return false;
        }

        private static bool HasGroundApproach(
            WarehouseLayoutData layout,
            bool[,] structureOccupied,
            bool[,] ladderOccupied,
            Vector2Int ladderCell,
            Vector2Int topCell)
        {
            Vector2Int approach = ladderCell - (topCell - ladderCell);
            return WarehouseGenerationUtility.IsCellInSourceHalf(layout, approach) &&
                   !structureOccupied[approach.x, approach.y] &&
                   !ladderOccupied[approach.x, approach.y];
        }

        private static bool HasContainerLowAt(WarehouseLayoutData layout, Vector2Int cell)
        {
            for (int i = 0; i < layout.Objects.Count; i++)
            {
                WarehouseObjectPlacement obj = layout.Objects[i];
                if (obj.Kind != WarehouseObjectKind.ContainerLow)
                    continue;

                RectInt rect = new(obj.Origin.x, obj.Origin.y, obj.Size.x, obj.Size.y);
                if (rect.Contains(cell))
                    return true;
            }

            return false;
        }

        private static void AddLadder(
            WarehouseLayoutData layout,
            WarehouseRecipe recipe,
            System.Random random,
            Vector2Int cell,
            WarehouseDirection direction)
        {
            WarehouseObjectPlacement placement = new()
            {
                Kind = WarehouseObjectKind.Ladder,
                Side = WarehouseSide.A,
                Origin = cell,
                Size = Vector2Int.one,
                Direction = direction,
                RotationY = DirectionToRotation(direction)
            };

            placement.VariantIndex = WarehouseVariantSelector.ChooseClimbAccessVariantIndex(recipe, layout, placement, random);
            layout.Objects.Add(placement);
        }

        private static Vector2Int DirectionToVector(WarehouseDirection direction)
        {
            return direction switch
            {
                WarehouseDirection.North => new Vector2Int(0, 1),
                WarehouseDirection.South => new Vector2Int(0, -1),
                WarehouseDirection.East => new Vector2Int(1, 0),
                WarehouseDirection.West => new Vector2Int(-1, 0),
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

        private readonly struct BridgePairCandidate
        {
            public readonly Vector2Int First;
            public readonly bool Horizontal;

            public BridgePairCandidate(Vector2Int first, bool horizontal)
            {
                First = first;
                Horizontal = horizontal;
            }
        }
    }

    public sealed class WarehouseCoverGenerationPass : ProcGenPass
    {
        public override string Id => "warehouse-cover-generation";

        public override void Declare(GenerationPassContract contract)
        {
            contract.Read(WarehouseKeys.Layout);
            contract.Write(WarehouseKeys.Layout);
        }

        public override void Execute(GenerationContext context)
        {
            WarehouseRecipe recipe = context.GetRecipe<WarehouseRecipe>();
            WarehouseLayoutData layout = context.Blackboard.GetRequired(WarehouseKeys.Layout);
            System.Random random = context.CreateRandom(Id);
            bool[,] structureOccupied = BuildStructureOccupancy(layout);
            bool[,] playableTopOccupied = BuildPlayableTopOccupancy(layout);
            bool[,] ladderOccupied = BuildLadderOccupancy(layout);
            bool[,] groundCoverOccupied = new bool[layout.Width, layout.Height];
            bool[,] topCoverOccupied = new bool[layout.Width, layout.Height];
            int targetCoverCells = GetTargetSourceHalfCoverCells(recipe, layout);

            TryAddCover(layout, recipe, random, structureOccupied, playableTopOccupied, ladderOccupied, groundCoverOccupied, topCoverOccupied, WarehouseObjectKind.FullCover, new Vector2Int(6, 13), WarehousePlacementSurface.Ground, 0f);
            TryAddCover(layout, recipe, random, structureOccupied, playableTopOccupied, ladderOccupied, groundCoverOccupied, topCoverOccupied, WarehouseObjectKind.FullCover, new Vector2Int(8, 13), WarehousePlacementSurface.Ground, 0f);
            TryAddCover(layout, recipe, random, structureOccupied, playableTopOccupied, ladderOccupied, groundCoverOccupied, topCoverOccupied, WarehouseObjectKind.PartialCover, new Vector2Int(3, 9), WarehousePlacementSurface.StructureTop, 90f);
            TryAddCover(layout, recipe, random, structureOccupied, playableTopOccupied, ladderOccupied, groundCoverOccupied, topCoverOccupied, WarehouseObjectKind.PartialCover, new Vector2Int(11, 9), WarehousePlacementSurface.StructureTop, 90f);

            int placed = CountSourceHalfCovers(layout);
            int attempts = 0;
            while (placed < targetCoverCells && attempts < recipe.CoverPlacementAttemptCount)
            {
                attempts++;
                Vector2Int cell = new(
                    random.Next(1, layout.Width - 1),
                    random.Next(3, Mathf.Max(4, layout.Height / 2)));
                WarehouseObjectKind kind = random.NextDouble() < recipe.FullCoverProbability
                    ? WarehouseObjectKind.FullCover
                    : WarehouseObjectKind.PartialCover;
                WarehousePlacementSurface surface = ChooseCoverSurface(recipe, random, playableTopOccupied, cell);

                if (TryAddCover(layout, recipe, random, structureOccupied, playableTopOccupied, ladderOccupied, groundCoverOccupied, topCoverOccupied, kind, cell, surface, random.Next(0, 4) * 90f))
                    placed++;
            }

            if (placed < targetCoverCells)
            {
                List<CoverCandidate> candidates = new();
                for (int y = 0; y < layout.Height / 2; y++)
                {
                    for (int x = 0; x < layout.Width; x++)
                    {
                        Vector2Int cell = new(x, y);
                        if (CanPlaceCover(layout, structureOccupied, playableTopOccupied, ladderOccupied, groundCoverOccupied, topCoverOccupied, cell, WarehousePlacementSurface.Ground))
                            candidates.Add(new CoverCandidate(cell, WarehousePlacementSurface.Ground));

                        if (CanPlaceCover(layout, structureOccupied, playableTopOccupied, ladderOccupied, groundCoverOccupied, topCoverOccupied, cell, WarehousePlacementSurface.StructureTop))
                            candidates.Add(new CoverCandidate(cell, WarehousePlacementSurface.StructureTop));
                    }
                }

                WarehouseGenerationUtility.Shuffle(candidates, random);
                for (int i = 0; i < candidates.Count && placed < targetCoverCells; i++)
                {
                    CoverCandidate candidate = candidates[i];
                    WarehouseObjectKind kind = random.NextDouble() < recipe.FullCoverProbability
                        ? WarehouseObjectKind.FullCover
                        : WarehouseObjectKind.PartialCover;

                    if (TryAddCover(layout, recipe, random, structureOccupied, playableTopOccupied, ladderOccupied, groundCoverOccupied, topCoverOccupied, kind, candidate.Cell, candidate.Surface, random.Next(0, 4) * 90f))
                        placed++;
                }
            }

            context.Diagnostics.Info($"Placed {placed} source-half cover cells before symmetry: ground={CountOccupied(groundCoverOccupied)}, top={CountOccupied(topCoverOccupied)}.", Id);
        }

        private static int GetTargetSourceHalfCoverCells(WarehouseRecipe recipe, WarehouseLayoutData layout)
        {
            int target = Mathf.RoundToInt(layout.Width * layout.Height * recipe.TargetCoverRatio / 2f);
            return Mathf.Max(target, recipe.ExtraCoverPairCount);
        }

        private static bool[,] BuildStructureOccupancy(WarehouseLayoutData layout)
        {
            bool[,] occupied = new bool[layout.Width, layout.Height];
            for (int i = 0; i < layout.Objects.Count; i++)
            {
                WarehouseObjectPlacement obj = layout.Objects[i];
                if (obj.Side != WarehouseSide.A || !obj.IsStructure)
                    continue;

                for (int y = 0; y < obj.Size.y; y++)
                {
                    for (int x = 0; x < obj.Size.x; x++)
                    {
                        Vector2Int cell = new(obj.Origin.x + x, obj.Origin.y + y);
                        if (WarehouseGenerationUtility.IsCellInSourceHalf(layout, cell))
                            occupied[cell.x, cell.y] = true;
                    }
                }
            }

            return occupied;
        }

        private static bool[,] BuildPlayableTopOccupancy(WarehouseLayoutData layout)
        {
            bool[,] occupied = new bool[layout.Width, layout.Height];
            for (int i = 0; i < layout.Objects.Count; i++)
            {
                WarehouseObjectPlacement obj = layout.Objects[i];
                if (obj.Side != WarehouseSide.A || !obj.IsTopWalkableSource)
                    continue;

                for (int y = 0; y < obj.Size.y; y++)
                {
                    for (int x = 0; x < obj.Size.x; x++)
                    {
                        Vector2Int cell = new(obj.Origin.x + x, obj.Origin.y + y);
                        if (WarehouseGenerationUtility.IsCellInSourceHalf(layout, cell))
                            occupied[cell.x, cell.y] = true;
                    }
                }
            }

            return occupied;
        }

        private static bool[,] BuildLadderOccupancy(WarehouseLayoutData layout)
        {
            bool[,] occupied = new bool[layout.Width, layout.Height];
            for (int i = 0; i < layout.Objects.Count; i++)
            {
                WarehouseObjectPlacement obj = layout.Objects[i];
                if (obj.Side != WarehouseSide.A || !obj.IsLadder)
                    continue;

                if (WarehouseGenerationUtility.IsCellInSourceHalf(layout, obj.Origin))
                    occupied[obj.Origin.x, obj.Origin.y] = true;
            }

            return occupied;
        }

        private static WarehousePlacementSurface ChooseCoverSurface(
            WarehouseRecipe recipe,
            System.Random random,
            bool[,] playableTopOccupied,
            Vector2Int cell)
        {
            if (cell.x < 0 || cell.x >= playableTopOccupied.GetLength(0) || cell.y < 0 || cell.y >= playableTopOccupied.GetLength(1))
                return WarehousePlacementSurface.Ground;

            return playableTopOccupied[cell.x, cell.y] && random.NextDouble() < recipe.TopCoverProbability
                ? WarehousePlacementSurface.StructureTop
                : WarehousePlacementSurface.Ground;
        }

        private static int CountSourceHalfCovers(WarehouseLayoutData layout)
        {
            int count = 0;
            for (int i = 0; i < layout.Objects.Count; i++)
            {
                WarehouseObjectPlacement obj = layout.Objects[i];
                if (obj.Side == WarehouseSide.A && obj.IsCover)
                    count++;
            }

            return count;
        }

        private static bool TryAddCover(
            WarehouseLayoutData layout,
            WarehouseRecipe recipe,
            System.Random random,
            bool[,] structureOccupied,
            bool[,] playableTopOccupied,
            bool[,] ladderOccupied,
            bool[,] groundCoverOccupied,
            bool[,] topCoverOccupied,
            WarehouseObjectKind kind,
            Vector2Int cell,
            WarehousePlacementSurface surface,
            float rotationY = 0f)
        {
            if (!CanPlaceCover(layout, structureOccupied, playableTopOccupied, ladderOccupied, groundCoverOccupied, topCoverOccupied, cell, surface))
                return false;

            if (!WarehousePlacementRules.TryChooseCoverRotation(layout, cell, surface, random, out rotationY))
                return false;

            WarehouseObjectPlacement placement = new()
            {
                Kind = kind,
                Side = WarehouseSide.A,
                Origin = cell,
                Size = Vector2Int.one,
                Surface = surface,
                RotationY = rotationY,
                Direction = RotationToDirection(rotationY)
            };

            placement.VariantIndex = WarehouseVariantSelector.ChooseCoverVariantIndex(recipe, layout, placement, random);
            if (placement.VariantIndex < 0)
                placement.VariantIndex = ChooseLegacyCoverVariantIndex(recipe, random, kind);

            layout.Objects.Add(placement);
            if (surface == WarehousePlacementSurface.StructureTop)
                topCoverOccupied[cell.x, cell.y] = true;
            else
                groundCoverOccupied[cell.x, cell.y] = true;

            return true;
        }

        private static int ChooseLegacyCoverVariantIndex(WarehouseRecipe recipe, System.Random random, WarehouseObjectKind kind)
        {
            WarehousePrefabVariant[] variants = recipe.GetCoverVariants(kind);
            if (variants == null || variants.Length == 0)
                return -1;

            float totalWeight = 0f;
            for (int i = 0; i < variants.Length; i++)
            {
                if (variants[i] != null && variants[i].Prefab != null)
                    totalWeight += Mathf.Max(0f, variants[i].Weight);
            }

            if (totalWeight <= 0f)
                return -1;

            double roll = random.NextDouble() * totalWeight;
            float cumulative = 0f;
            for (int i = 0; i < variants.Length; i++)
            {
                if (variants[i] == null || variants[i].Prefab == null)
                    continue;

                cumulative += Mathf.Max(0f, variants[i].Weight);
                if (roll <= cumulative)
                    return i;
            }

            return -1;
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

        private static bool CanPlaceCover(
            WarehouseLayoutData layout,
            bool[,] structureOccupied,
            bool[,] playableTopOccupied,
            bool[,] ladderOccupied,
            bool[,] groundCoverOccupied,
            bool[,] topCoverOccupied,
            Vector2Int cell,
            WarehousePlacementSurface surface)
        {
            if (!WarehouseGenerationUtility.IsCellInSourceHalf(layout, cell))
                return false;

            bool hasStructure = structureOccupied[cell.x, cell.y];
            bool hasPlayableTop = playableTopOccupied[cell.x, cell.y];
            bool hasLadder = ladderOccupied[cell.x, cell.y];
            if (surface == WarehousePlacementSurface.StructureTop)
            {
                if (!hasPlayableTop || hasLadder || topCoverOccupied[cell.x, cell.y])
                    return false;
            }
            else if (hasStructure || hasLadder || groundCoverOccupied[cell.x, cell.y])
            {
                return false;
            }

            WarehouseObjectPlacement probe = new()
            {
                Kind = WarehouseObjectKind.PartialCover,
                Side = WarehouseSide.A,
                Origin = cell,
                Size = Vector2Int.one,
                Surface = surface
            };

            return !WarehouseRepairPass.ObjectTouchesRadius(probe, layout.SpawnA, layout.SpawnClearRadius);
        }

        private static int CountOccupied(bool[,] occupied)
        {
            int count = 0;
            for (int y = 0; y < occupied.GetLength(1); y++)
            {
                for (int x = 0; x < occupied.GetLength(0); x++)
                {
                    if (occupied[x, y])
                        count++;
                }
            }

            return count;
        }

        private readonly struct CoverCandidate
        {
            public readonly Vector2Int Cell;
            public readonly WarehousePlacementSurface Surface;

            public CoverCandidate(Vector2Int cell, WarehousePlacementSurface surface)
            {
                Cell = cell;
                Surface = surface;
            }
        }
    }

    public sealed class WarehouseSymmetryPass : ProcGenPass
    {
        public override string Id => "warehouse-symmetry";

        public override void Declare(GenerationPassContract contract)
        {
            contract.Read(WarehouseKeys.Layout);
            contract.Write(WarehouseKeys.Layout);
        }

        public override void Execute(GenerationContext context)
        {
            WarehouseRecipe recipe = context.GetRecipe<WarehouseRecipe>();
            if (recipe.SymmetryMode != WarehouseSymmetryMode.MirroredHalf)
            {
                context.Diagnostics.Warning("IndependentHalves is reserved for future generation. Using current objects as-is.", Id);
                return;
            }

            WarehouseLayoutData layout = context.Blackboard.GetRequired(WarehouseKeys.Layout);
            int count = layout.Objects.Count;
            for (int i = 0; i < count; i++)
            {
                WarehouseObjectPlacement source = layout.Objects[i];
                layout.Objects.Add(new WarehouseObjectPlacement
                {
                    Kind = source.Kind,
                    Side = WarehouseSide.B,
                    Origin = new Vector2Int(
                        layout.Width - source.Origin.x - source.Size.x,
                        layout.Height - source.Origin.y - source.Size.y),
                    Size = source.Size,
                    Surface = source.Surface,
                    Direction = MirrorDirection(source.Direction),
                    ConnectionMask = MirrorConnectionMask(source.ConnectionMask),
                    BridgeConnectionType = source.BridgeConnectionType,
                    RotationY = Mathf.Repeat(source.RotationY + 180f, 360f),
                    VariantIndex = source.VariantIndex
                });
            }
        }

        private static WarehouseDirectionMask MirrorConnectionMask(WarehouseDirectionMask mask)
        {
            WarehouseDirectionMask mirrored = WarehouseDirectionMask.None;
            if ((mask & WarehouseDirectionMask.North) != 0)
                mirrored |= WarehouseDirectionMask.South;
            if ((mask & WarehouseDirectionMask.South) != 0)
                mirrored |= WarehouseDirectionMask.North;
            if ((mask & WarehouseDirectionMask.East) != 0)
                mirrored |= WarehouseDirectionMask.West;
            if ((mask & WarehouseDirectionMask.West) != 0)
                mirrored |= WarehouseDirectionMask.East;

            return mirrored;
        }

        private static WarehouseDirection MirrorDirection(WarehouseDirection direction)
        {
            return direction switch
            {
                WarehouseDirection.North => WarehouseDirection.South,
                WarehouseDirection.South => WarehouseDirection.North,
                WarehouseDirection.East => WarehouseDirection.West,
                WarehouseDirection.West => WarehouseDirection.East,
                _ => direction
            };
        }
    }

    public sealed class WarehouseRepairPass : ProcGenPass
    {
        public override string Id => "warehouse-repair";

        public override void Declare(GenerationPassContract contract)
        {
            contract.Read(WarehouseKeys.Layout);
            contract.Write(WarehouseKeys.Layout);
        }

        public override void Execute(GenerationContext context)
        {
            WarehouseRecipe recipe = context.GetRecipe<WarehouseRecipe>();
            WarehouseLayoutData layout = context.Blackboard.GetRequired(WarehouseKeys.Layout);
            bool[,] reservedGround = WarehouseGenerationUtility.BuildReservedGroundMask(layout);
            int totalRemoved = 0;
            int removed;

            do
            {
                removed = 0;
                for (int i = layout.Objects.Count - 1; i >= 0; i--)
                {
                    WarehouseObjectPlacement obj = layout.Objects[i];
                    if (!IsObjectInside(layout, obj) ||
                        ViolatesSpawnZone(layout, obj) ||
                        ViolatesCaptureZone(layout, obj) ||
                        ViolatesGroundCorridor(layout, obj, reservedGround) ||
                        ViolatesBridgeConnectivity(layout, recipe, obj) ||
                        ViolatesLadderCorridor(layout, obj, reservedGround) ||
                        ViolatesLadderTarget(layout, obj) ||
                        ViolatesCoverSurface(layout, obj) ||
                        OverlapsPreviousStructure(layout, obj, i) ||
                        OverlapsPreviousCover(layout, obj, i))
                    {
                        layout.Objects.RemoveAt(i);
                        removed++;
                    }
                }

                totalRemoved += removed;
            }
            while (removed > 0);

            if (totalRemoved > 0)
                context.Diagnostics.Info($"Removed {totalRemoved} invalid warehouse objects.", Id);
        }

        private static bool ViolatesSpawnZone(WarehouseLayoutData layout, WarehouseObjectPlacement obj)
        {
            if (!obj.IsStructure && !obj.IsCover)
                return false;

            return ObjectTouchesRadius(obj, layout.SpawnA, layout.SpawnClearRadius) ||
                   ObjectTouchesRadius(obj, layout.SpawnB, layout.SpawnClearRadius);
        }

        private static bool ViolatesCaptureZone(WarehouseLayoutData layout, WarehouseObjectPlacement obj)
        {
            return obj.IsStructure && ObjectTouchesRadius(obj, layout.CapturePoint, layout.CaptureClearRadius);
        }

        private static bool ViolatesGroundCorridor(WarehouseLayoutData layout, WarehouseObjectPlacement obj, bool[,] reservedGround)
        {
            if (!obj.IsGroundBlocker)
                return false;

            for (int y = 0; y < obj.Size.y; y++)
            {
                for (int x = 0; x < obj.Size.x; x++)
                {
                    Vector2Int sourceCell = ToSourceHalfCell(layout, new Vector2Int(obj.Origin.x + x, obj.Origin.y + y));
                    if (WarehouseGenerationUtility.IsCellInSourceHalf(layout, sourceCell) && reservedGround[sourceCell.x, sourceCell.y])
                        return true;
                }
            }

            return false;
        }

        private static bool ViolatesLadderCorridor(WarehouseLayoutData layout, WarehouseObjectPlacement obj, bool[,] reservedGround)
        {
            if (!obj.IsLadder)
                return false;

            Vector2Int sourceHalfCell = obj.Side == WarehouseSide.A
                ? obj.Origin
                : new Vector2Int(layout.Width - obj.Origin.x - obj.Size.x, layout.Height - obj.Origin.y - obj.Size.y);

            return WarehouseGenerationUtility.IsCellInSourceHalf(layout, sourceHalfCell) &&
                   reservedGround[sourceHalfCell.x, sourceHalfCell.y];
        }

        private static bool ViolatesBridgeConnectivity(WarehouseLayoutData layout, WarehouseRecipe recipe, WarehouseObjectPlacement obj)
        {
            if (obj.Kind != WarehouseObjectKind.Bridge)
                return false;

            if (CountContainerSides(layout, obj.Origin) >= recipe.MaxBridgeBlockedSides)
                return true;

            WarehouseDirectionMask mask = GetBridgeConnectionMask(obj);
            obj.ConnectionMask = mask;
            int connectionCount = WarehouseGenerationUtility.CountDirections(mask);
            if (connectionCount == 2)
            {
                obj.BridgeConnectionType = WarehouseBridgeConnectionType.Straight;
                if (!IsStraightBridgeMask(mask))
                    return true;
            }
            else if (connectionCount == 3)
            {
                obj.BridgeConnectionType = WarehouseBridgeConnectionType.ThreeWay;
            }
            else
            {
                return true;
            }

            return !HasConnectedSide(layout, obj, mask, WarehouseDirection.North, Vector2Int.up) ||
                   !HasConnectedSide(layout, obj, mask, WarehouseDirection.South, Vector2Int.down) ||
                   !HasConnectedSide(layout, obj, mask, WarehouseDirection.East, Vector2Int.right) ||
                   !HasConnectedSide(layout, obj, mask, WarehouseDirection.West, Vector2Int.left);
        }

        private static bool ViolatesLadderTarget(WarehouseLayoutData layout, WarehouseObjectPlacement obj)
        {
            return obj.IsLadder && !WarehousePlacementRules.IsLadderPlacementValid(layout, obj);
        }

        private static bool ViolatesCoverSurface(WarehouseLayoutData layout, WarehouseObjectPlacement obj)
        {
            if (!obj.IsCover)
                return false;

            if (WarehousePlacementRules.IsCoverPlacementValid(layout, obj))
                return false;

            if (WarehousePlacementRules.TryChooseCoverRotation(layout, obj.Origin, obj.Surface, null, out float rotationY, obj))
            {
                obj.RotationY = rotationY;
                obj.Direction = RotationToDirection(rotationY);
                return false;
            }

            return true;
        }

        private static bool OverlapsPreviousStructure(WarehouseLayoutData layout, WarehouseObjectPlacement obj, int index)
        {
            if (!obj.IsStructure)
                return false;

            RectInt rect = GetRect(obj);
            for (int i = 0; i < index; i++)
            {
                WarehouseObjectPlacement other = layout.Objects[i];
                if (other.IsStructure && rect.Overlaps(GetRect(other)))
                    return true;
            }

            return false;
        }

        private static bool OverlapsPreviousCover(WarehouseLayoutData layout, WarehouseObjectPlacement obj, int index)
        {
            if (!obj.IsCover)
                return false;

            RectInt rect = GetRect(obj);
            for (int i = 0; i < index; i++)
            {
                WarehouseObjectPlacement other = layout.Objects[i];
                if (other.IsCover && other.Surface == obj.Surface && rect.Overlaps(GetRect(other)))
                    return true;
            }

            return false;
        }

        private static bool HasStructureAt(WarehouseLayoutData layout, Vector2Int cell)
        {
            for (int i = 0; i < layout.Objects.Count; i++)
            {
                WarehouseObjectPlacement obj = layout.Objects[i];
                if (!obj.IsStructure)
                    continue;

                if (GetRect(obj).Contains(cell))
                    return true;
            }

            return false;
        }

        private static bool HasContainerLowAt(WarehouseLayoutData layout, Vector2Int cell)
        {
            for (int i = 0; i < layout.Objects.Count; i++)
            {
                WarehouseObjectPlacement obj = layout.Objects[i];
                if (obj.Kind != WarehouseObjectKind.ContainerLow)
                    continue;

                if (GetRect(obj).Contains(cell))
                    return true;
            }

            return false;
        }

        private static bool HasPlayableTopAt(WarehouseLayoutData layout, Vector2Int cell)
        {
            for (int i = 0; i < layout.Objects.Count; i++)
            {
                WarehouseObjectPlacement obj = layout.Objects[i];
                if (!obj.IsTopWalkableSource)
                    continue;

                if (GetRect(obj).Contains(cell))
                    return true;
            }

            return false;
        }

        private static bool HasLadderAt(WarehouseLayoutData layout, Vector2Int cell)
        {
            for (int i = 0; i < layout.Objects.Count; i++)
            {
                WarehouseObjectPlacement obj = layout.Objects[i];
                if (obj.IsLadder && GetRect(obj).Contains(cell))
                    return true;
            }

            return false;
        }

        private static bool HasWalkableGroundApproach(WarehouseLayoutData layout, WarehouseObjectPlacement ladder, Vector2Int topCell)
        {
            for (int i = 0; i < WarehouseGenerationUtility.CardinalDirections.Length; i++)
            {
                Vector2Int approach = ladder.Origin + WarehouseGenerationUtility.CardinalDirections[i];
                if (approach == topCell || !layout.IsInside(approach))
                    continue;

                if (!HasGroundBlockerAt(layout, approach) && !HasLadderAtExcept(layout, approach, ladder))
                    return true;
            }

            return false;
        }

        private static bool HasGroundBlockerAt(WarehouseLayoutData layout, Vector2Int cell)
        {
            for (int i = 0; i < layout.Objects.Count; i++)
            {
                WarehouseObjectPlacement obj = layout.Objects[i];
                if (!obj.IsGroundBlocker)
                    continue;

                if (GetRect(obj).Contains(cell))
                    return true;
            }

            return false;
        }

        private static bool HasLadderAtExcept(WarehouseLayoutData layout, Vector2Int cell, WarehouseObjectPlacement except)
        {
            for (int i = 0; i < layout.Objects.Count; i++)
            {
                WarehouseObjectPlacement obj = layout.Objects[i];
                if (ReferenceEquals(obj, except) || !obj.IsLadder)
                    continue;

                if (GetRect(obj).Contains(cell))
                    return true;
            }

            return false;
        }

        private static int CountContainerSides(WarehouseLayoutData layout, Vector2Int cell)
        {
            int count = 0;
            for (int i = 0; i < WarehouseGenerationUtility.CardinalDirections.Length; i++)
            {
                if (HasContainerAt(layout, cell + WarehouseGenerationUtility.CardinalDirections[i]))
                    count++;
            }

            return count;
        }

        private static bool HasContainerAt(WarehouseLayoutData layout, Vector2Int cell)
        {
            for (int i = 0; i < layout.Objects.Count; i++)
            {
                WarehouseObjectPlacement obj = layout.Objects[i];
                if (!obj.IsContainer)
                    continue;

                if (GetRect(obj).Contains(cell))
                    return true;
            }

            return false;
        }

        private static bool HasBridgeConnectorAt(WarehouseLayoutData layout, WarehouseObjectPlacement self, Vector2Int cell)
        {
            if (!layout.IsInside(cell))
                return false;

            for (int i = 0; i < layout.Objects.Count; i++)
            {
                WarehouseObjectPlacement obj = layout.Objects[i];
                if (ReferenceEquals(obj, self) || !obj.IsTopWalkableSource)
                    continue;

                if (GetRect(obj).Contains(cell))
                    return true;
            }

            return false;
        }

        private static bool HasConnectedSide(
            WarehouseLayoutData layout,
            WarehouseObjectPlacement bridge,
            WarehouseDirectionMask mask,
            WarehouseDirection direction,
            Vector2Int offset)
        {
            return !WarehouseGenerationUtility.HasDirection(mask, direction) ||
                   HasBridgeConnectorAt(layout, bridge, bridge.Origin + offset);
        }

        private static WarehouseDirectionMask GetBridgeConnectionMask(WarehouseObjectPlacement obj)
        {
            if (obj.ConnectionMask != WarehouseDirectionMask.None)
                return obj.ConnectionMask;

            bool horizontal = Mathf.Abs(Mathf.DeltaAngle(obj.RotationY, 0f)) <= 45f ||
                              Mathf.Abs(Mathf.DeltaAngle(obj.RotationY, 180f)) <= 45f;
            return horizontal
                ? WarehouseDirectionMask.East | WarehouseDirectionMask.West
                : WarehouseDirectionMask.North | WarehouseDirectionMask.South;
        }

        private static bool IsStraightBridgeMask(WarehouseDirectionMask mask)
        {
            return mask == (WarehouseDirectionMask.East | WarehouseDirectionMask.West) ||
                   mask == (WarehouseDirectionMask.North | WarehouseDirectionMask.South);
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

        private static Vector2Int ToSourceHalfCell(WarehouseLayoutData layout, Vector2Int cell)
        {
            return cell.y < layout.Height / 2
                ? cell
                : new Vector2Int(layout.Width - cell.x - 1, layout.Height - cell.y - 1);
        }

        private static bool IsObjectInside(WarehouseLayoutData layout, WarehouseObjectPlacement obj)
        {
            RectInt rect = GetRect(obj);
            return rect.xMin >= 0 && rect.yMin >= 0 && rect.xMax <= layout.Width && rect.yMax <= layout.Height;
        }

        public static bool ObjectTouchesRadius(WarehouseObjectPlacement obj, Vector2 point, float radius)
        {
            float radiusSqr = radius * radius;
            if ((obj.Center - point).sqrMagnitude <= radiusSqr)
                return true;

            for (int y = 0; y < obj.Size.y; y++)
            {
                for (int x = 0; x < obj.Size.x; x++)
                {
                    Vector2 cellCenter = new Vector2(obj.Origin.x + x + 0.5f, obj.Origin.y + y + 0.5f);
                    if ((cellCenter - point).sqrMagnitude <= radiusSqr)
                        return true;
                }
            }

            return false;
        }

        private static RectInt GetRect(WarehouseObjectPlacement obj)
        {
            return new RectInt(obj.Origin.x, obj.Origin.y, obj.Size.x, obj.Size.y);
        }
    }

    public sealed class WarehouseNavigationPass : ProcGenPass
    {
        public override string Id => "warehouse-navigation";

        public override void Declare(GenerationPassContract contract)
        {
            contract.Read(WarehouseKeys.Layout);
            contract.Write(WarehouseKeys.Navigation);
        }

        public override void Execute(GenerationContext context)
        {
            WarehouseRecipe recipe = context.GetRecipe<WarehouseRecipe>();
            WarehouseLayoutData layout = context.Blackboard.GetRequired(WarehouseKeys.Layout);
            context.Blackboard.Set(WarehouseKeys.Navigation, new WarehouseNavigationGrid(
                layout,
                recipe.PartialCoverPathCost,
                recipe.FullCoverPathCost));
        }
    }

    public sealed class WarehouseValidationPass : ProcGenPass
    {
        public override string Id => "warehouse-validation";

        public override void Declare(GenerationPassContract contract)
        {
            contract.Read(WarehouseKeys.Layout);
            contract.Write(WarehouseKeys.Layout);
        }

        public override void Execute(GenerationContext context)
        {
            WarehouseRecipe recipe = context.GetRecipe<WarehouseRecipe>();
            WarehouseLayoutData layout = context.Blackboard.GetRequired(WarehouseKeys.Layout);
            bool[,] reservedGround = WarehouseGenerationUtility.BuildReservedGroundMask(layout);
            int changes = WarehousePlacementRules.CleanupLayout(layout, reservedGround, recipe.TallContainerProbability);
            if (changes > 0)
                context.Diagnostics.Info($"Applied {changes} final warehouse validation changes.", Id);
        }
    }

    public sealed class WarehouseGroundPocketSealPass : ProcGenPass
    {
        public override string Id => "warehouse-ground-pocket-seal";

        public override void Declare(GenerationPassContract contract)
        {
            contract.Read(WarehouseKeys.Layout);
            contract.Write(WarehouseKeys.Layout);
        }

        public override void Execute(GenerationContext context)
        {
            WarehouseRecipe recipe = context.GetRecipe<WarehouseRecipe>();
            if (recipe.GroundPocketSealIterationCount <= 0 || recipe.MaxGroundPocketSealCellCount <= 0)
                return;

            WarehouseLayoutData layout = context.Blackboard.GetRequired(WarehouseKeys.Layout);
            bool[,] reservedGround = WarehouseGenerationUtility.BuildReservedGroundMask(layout);
            int changes = 0;
            for (int i = 0; i < recipe.GroundPocketSealIterationCount; i++)
            {
                int iterationChanges = WarehousePlacementRules.SealUnreachableGroundPockets(
                    layout,
                    reservedGround,
                    recipe.MaxGroundPocketSealCellCount);
                changes += iterationChanges;

                if (iterationChanges == 0)
                    break;
            }

            if (changes > 0)
                context.Diagnostics.Info($"Sealed {changes} unreachable warehouse ground pocket cells.", Id);
        }
    }

    public sealed class WarehouseFitnessPass : ProcGenPass
    {
        public override string Id => "warehouse-fitness";

        public override void Declare(GenerationPassContract contract)
        {
            contract.Read(WarehouseKeys.Layout);
            contract.Read(WarehouseKeys.Navigation);
            contract.Write(WarehouseKeys.Fitness);
        }

        public override void Execute(GenerationContext context)
        {
            WarehouseRecipe recipe = context.GetRecipe<WarehouseRecipe>();
            WarehouseLayoutData layout = context.Blackboard.GetRequired(WarehouseKeys.Layout);
            WarehouseNavigationGrid navigation = context.Blackboard.GetRequired(WarehouseKeys.Navigation);
            WarehouseFitnessReport report = new WarehouseFitnessReport();

            ScoreClearZones(layout, report);
            ScoreBridgeQuality(layout, report);
            ScoreCoverClusters(recipe, layout, report);
            ScoreCaptureCoverCount(recipe, layout, report);
            ScoreTopAccessibility(recipe, layout, report);
            ScoreStructureDensity(recipe, layout, report);
            ScoreGroundPath("Spawn A", navigation.FindGroundPathCost(layout.SpawnA, layout.CapturePoint), report);
            ScoreGroundPath("Spawn B", navigation.FindGroundPathCost(layout.SpawnB, layout.CapturePoint), report);
            ScorePath("Spawn A", navigation.FindPathCost(layout.SpawnA, layout.CapturePoint), report, out int pathA);
            ScorePath("Spawn B", navigation.FindPathCost(layout.SpawnB, layout.CapturePoint), report, out int pathB);
            report.PathCostA = pathA;
            report.PathCostB = pathB;
            ScoreWeightedPathCost("Spawn A", recipe, pathA, report);
            ScoreWeightedPathCost("Spawn B", recipe, pathB, report);

            if (pathA >= 0 && pathB >= 0)
            {
                float diff = Mathf.Abs(pathA - pathB);
                if (diff > recipe.AllowedPathCostDifference)
                    AddPenalty(report, diff * 10f, $"Path cost difference is {diff}.");
            }

            if (navigation.HasLineOfSight(layout.SpawnA, layout.CapturePoint))
                AddPenalty(report, 100f, "Spawn A has direct line of sight to capture.");

            if (navigation.HasLineOfSight(layout.SpawnB, layout.CapturePoint))
                AddPenalty(report, 100f, "Spawn B has direct line of sight to capture.");

            context.Blackboard.Set(WarehouseKeys.Fitness, report);
            if (report.PenaltyScore > 0f)
                context.Diagnostics.Warning($"Warehouse penalty score: {report.PenaltyScore}.", Id);
        }

        private static void ScoreClearZones(WarehouseLayoutData layout, WarehouseFitnessReport report)
        {
            for (int i = 0; i < layout.Objects.Count; i++)
            {
                WarehouseObjectPlacement obj = layout.Objects[i];
                if (obj.IsStructure || obj.IsCover)
                {
                    if (WarehouseRepairPass.ObjectTouchesRadius(obj, layout.SpawnA, layout.SpawnClearRadius))
                        AddPenalty(report, 1000f, $"{obj.Kind} inside Spawn A clear zone.");

                    if (WarehouseRepairPass.ObjectTouchesRadius(obj, layout.SpawnB, layout.SpawnClearRadius))
                        AddPenalty(report, 1000f, $"{obj.Kind} inside Spawn B clear zone.");
                }

                if (obj.IsStructure &&
                    WarehouseRepairPass.ObjectTouchesRadius(obj, layout.CapturePoint, layout.CaptureClearRadius))
                {
                    AddPenalty(report, 1000f, $"{obj.Kind} inside capture clear zone.");
                }
            }
        }

        private static void ScoreBridgeQuality(WarehouseLayoutData layout, WarehouseFitnessReport report)
        {
            for (int i = 0; i < layout.Objects.Count; i++)
            {
                WarehouseObjectPlacement obj = layout.Objects[i];
                if (obj.Kind != WarehouseObjectKind.Bridge)
                    continue;

                int blockedSides = 0;
                for (int j = 0; j < WarehouseGenerationUtility.CardinalDirections.Length; j++)
                {
                    Vector2Int side = obj.Origin + WarehouseGenerationUtility.CardinalDirections[j];
                    if (HasContainerAt(layout, side) || HasLadderPointingTo(layout, side, obj.Origin))
                        blockedSides++;
                }

                if (blockedSides >= WarehouseGenerationUtility.CardinalDirections.Length)
                    AddPenalty(report, 500f, "Bridge is surrounded by containers or ladders.");
            }
        }

        private static void ScoreCoverClusters(
            WarehouseRecipe recipe,
            WarehouseLayoutData layout,
            WarehouseFitnessReport report)
        {
            float radius = recipe.CoverClusterRadiusValue;
            if (radius <= 0f)
                return;

            List<WarehouseObjectPlacement> covers = new();
            for (int i = 0; i < layout.Objects.Count; i++)
            {
                WarehouseObjectPlacement obj = layout.Objects[i];
                if (obj.IsCover)
                    covers.Add(obj);
            }

            float radiusSqr = radius * radius;
            int excessNeighbors = 0;
            for (int i = 0; i < covers.Count; i++)
            {
                int neighbors = 0;
                for (int j = 0; j < covers.Count; j++)
                {
                    if (i == j)
                        continue;

                    if ((covers[i].Center - covers[j].Center).sqrMagnitude <= radiusSqr)
                        neighbors++;
                }

                excessNeighbors += Mathf.Max(0, neighbors - recipe.MaxCoverNeighborCount);
            }

            if (excessNeighbors > 0)
                AddPenalty(report, excessNeighbors * recipe.CoverClusterPenaltyWeight, $"Too dense cover clusters: {excessNeighbors} excess neighbors.");
        }

        private static void ScoreCaptureCoverCount(
            WarehouseRecipe recipe,
            WarehouseLayoutData layout,
            WarehouseFitnessReport report)
        {
            float radius = recipe.CaptureCoverRadiusValue;
            if (radius <= 0f)
                return;

            int covers = CountCoversNearPoint(layout, layout.CapturePoint, radius);
            if (covers < recipe.MinCaptureCovers)
            {
                AddPenalty(
                    report,
                    (recipe.MinCaptureCovers - covers) * recipe.CaptureCoverPenaltyWeight,
                    $"Too few covers near capture: {covers}/{recipe.MinCaptureCovers}.");
            }
            else if (covers > recipe.MaxCaptureCovers)
            {
                AddPenalty(
                    report,
                    (covers - recipe.MaxCaptureCovers) * recipe.CaptureCoverPenaltyWeight,
                    $"Too many covers near capture: {covers}/{recipe.MaxCaptureCovers}.");
            }
        }

        private static int CountCoversNearPoint(WarehouseLayoutData layout, Vector2 point, float radius)
        {
            float radiusSqr = radius * radius;
            int count = 0;
            for (int i = 0; i < layout.Objects.Count; i++)
            {
                WarehouseObjectPlacement obj = layout.Objects[i];
                if (obj.IsCover && (obj.Center - point).sqrMagnitude <= radiusSqr)
                    count++;
            }

            return count;
        }

        private static void ScoreTopAccessibility(
            WarehouseRecipe recipe,
            WarehouseLayoutData layout,
            WarehouseFitnessReport report)
        {
            List<List<Vector2Int>> components = BuildTopComponents(layout);
            int inaccessible = 0;
            for (int i = 0; i < components.Count; i++)
            {
                List<Vector2Int> component = components[i];
                if (component.Count >= recipe.MinTopComponentSize && !HasLadderAccess(layout, component))
                    inaccessible++;
            }

            if (inaccessible > 0)
                AddPenalty(report, inaccessible * 300f, $"Top components without ladder access: {inaccessible}.");
        }

        private static bool HasContainerAt(WarehouseLayoutData layout, Vector2Int cell)
        {
            for (int i = 0; i < layout.Objects.Count; i++)
            {
                WarehouseObjectPlacement obj = layout.Objects[i];
                if (!obj.IsContainer)
                    continue;

                RectInt rect = new(obj.Origin.x, obj.Origin.y, obj.Size.x, obj.Size.y);
                if (rect.Contains(cell))
                    return true;
            }

            return false;
        }

        private static bool HasLadderPointingTo(WarehouseLayoutData layout, Vector2Int ladderCell, Vector2Int targetCell)
        {
            for (int i = 0; i < layout.Objects.Count; i++)
            {
                WarehouseObjectPlacement obj = layout.Objects[i];
                if (!obj.IsLadder || obj.Origin != ladderCell)
                    continue;

                if (obj.Origin + WarehouseNavigationGrid.DirectionToVector(obj.Direction) == targetCell)
                    return true;
            }

            return false;
        }

        private static List<List<Vector2Int>> BuildTopComponents(WarehouseLayoutData layout)
        {
            bool[,] topWalkable = new bool[layout.Width, layout.Height];
            for (int i = 0; i < layout.Objects.Count; i++)
            {
                WarehouseObjectPlacement obj = layout.Objects[i];
                if (!obj.IsTopWalkableSource)
                    continue;

                for (int y = 0; y < obj.Size.y; y++)
                {
                    for (int x = 0; x < obj.Size.x; x++)
                    {
                        Vector2Int cell = new(obj.Origin.x + x, obj.Origin.y + y);
                        if (layout.IsInside(cell))
                            topWalkable[cell.x, cell.y] = true;
                    }
                }
            }

            bool[,] visited = new bool[layout.Width, layout.Height];
            List<List<Vector2Int>> components = new();
            Queue<Vector2Int> queue = new();
            for (int y = 0; y < layout.Height; y++)
            {
                for (int x = 0; x < layout.Width; x++)
                {
                    if (!topWalkable[x, y] || visited[x, y])
                        continue;

                    List<Vector2Int> component = new();
                    visited[x, y] = true;
                    queue.Enqueue(new Vector2Int(x, y));
                    while (queue.Count > 0)
                    {
                        Vector2Int cell = queue.Dequeue();
                        component.Add(cell);
                        for (int i = 0; i < WarehouseGenerationUtility.CardinalDirections.Length; i++)
                        {
                            Vector2Int next = cell + WarehouseGenerationUtility.CardinalDirections[i];
                            if (!layout.IsInside(next) || !topWalkable[next.x, next.y] || visited[next.x, next.y])
                                continue;

                            visited[next.x, next.y] = true;
                            queue.Enqueue(next);
                        }
                    }

                    components.Add(component);
                }
            }

            return components;
        }

        private static bool HasLadderAccess(WarehouseLayoutData layout, List<Vector2Int> component)
        {
            HashSet<Vector2Int> cells = new(component);
            for (int i = 0; i < layout.Objects.Count; i++)
            {
                WarehouseObjectPlacement obj = layout.Objects[i];
                if (obj.IsLadder && cells.Contains(obj.Origin + WarehouseNavigationGrid.DirectionToVector(obj.Direction)))
                    return true;
            }

            return false;
        }

        private static void ScoreStructureDensity(
            WarehouseRecipe recipe,
            WarehouseLayoutData layout,
            WarehouseFitnessReport report)
        {
            int structureCells = CountStructureCells(layout);
            int totalCells = layout.Width * layout.Height;
            int targetCells = Mathf.RoundToInt(totalCells * recipe.TargetStructureRatio);
            int minCells = Mathf.RoundToInt(totalCells * recipe.MinStructureRatio);
            int maxCells = Mathf.RoundToInt(totalCells * recipe.MaxStructureRatio);

            report.StructureCells = structureCells;
            report.TargetStructureCells = targetCells;
            report.MinStructureCells = minCells;
            report.MaxStructureCells = maxCells;

            if (structureCells < minCells)
                AddPenalty(report, (minCells - structureCells) * recipe.StructureDensityPenaltyWeight, $"Too few structure cells: {structureCells}/{targetCells}.");
            else if (structureCells > maxCells)
                AddPenalty(report, (structureCells - maxCells) * recipe.StructureDensityPenaltyWeight, $"Too many structure cells: {structureCells}/{targetCells}.");
        }

        private static int CountStructureCells(WarehouseLayoutData layout)
        {
            bool[,] occupied = new bool[layout.Width, layout.Height];
            for (int i = 0; i < layout.Objects.Count; i++)
            {
                WarehouseObjectPlacement obj = layout.Objects[i];
                if (!obj.IsStructure)
                    continue;

                for (int y = 0; y < obj.Size.y; y++)
                {
                    for (int x = 0; x < obj.Size.x; x++)
                    {
                        Vector2Int cell = new(obj.Origin.x + x, obj.Origin.y + y);
                        if (layout.IsInside(cell))
                            occupied[cell.x, cell.y] = true;
                    }
                }
            }

            int count = 0;
            for (int y = 0; y < layout.Height; y++)
            {
                for (int x = 0; x < layout.Width; x++)
                {
                    if (occupied[x, y])
                        count++;
                }
            }

            return count;
        }

        private static void ScorePath(string label, int cost, WarehouseFitnessReport report, out int pathCost)
        {
            pathCost = cost;
            if (cost >= 0)
                return;

            AddPenalty(report, 2000f, $"No path from {label} to capture.");
        }

        private static void ScoreWeightedPathCost(
            string label,
            WarehouseRecipe recipe,
            int cost,
            WarehouseFitnessReport report)
        {
            if (cost < 0 || cost <= recipe.MaxAllowedWeightedPathCost)
                return;

            AddPenalty(
                report,
                (cost - recipe.MaxAllowedWeightedPathCost) * recipe.PathCostPenaltyWeight,
                $"{label} weighted path cost is {cost}.");
        }

        private static void ScoreGroundPath(string label, int cost, WarehouseFitnessReport report)
        {
            if (cost >= 0)
                return;

            AddPenalty(report, 2500f, $"No ground path from {label} to capture.");
        }

        private static void AddPenalty(WarehouseFitnessReport report, float penalty, string violation)
        {
            report.PenaltyScore += penalty;
            report.Violations.Add(violation);
        }
    }

    public sealed class WarehouseBuildPlanPass : ProcGenPass
    {
        public override string Id => "warehouse-build-plan";

        public override void Declare(GenerationPassContract contract)
        {
            contract.Read(WarehouseKeys.Layout);
            contract.Read(WarehouseKeys.Fitness);
            contract.Write(WarehouseKeys.BuildPlan);
        }

        public override void Execute(GenerationContext context)
        {
            context.Blackboard.Set(WarehouseKeys.BuildPlan, new WarehouseBuildPlan
            {
                Layout = context.Blackboard.GetRequired(WarehouseKeys.Layout),
                Fitness = context.Blackboard.GetRequired(WarehouseKeys.Fitness)
            });
        }
    }
}
