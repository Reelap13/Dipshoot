using System.Collections.Generic;
using UnityEngine;

namespace Game.ProcGen.Warehouse
{
    public sealed class WarehouseWfcPass : ProcGenPass
    {
        private const int TileCount = 9;
        private const float InvalidPenalty = 10000f;

        public override string Id => "warehouse-wfc";

        public override void Declare(GenerationPassContract contract)
        {
            contract.Write(WarehouseKeys.Layout);
            contract.Write(WarehouseKeys.Navigation);
            contract.Write(WarehouseKeys.Fitness);
            contract.Write(WarehouseKeys.BuildPlan);
        }

        public override void Execute(GenerationContext context)
        {
            WarehouseWfcRecipe recipe = context.GetRecipe<WarehouseWfcRecipe>();
            System.Random random = context.CreateRandom(Id);
            WfcCandidate best = new() { Score = float.MaxValue };

            for (int attempt = 0; attempt < recipe.AttemptCount; attempt++)
            {
                WarehouseLayoutData meta = CreateLayout(recipe, random);
                if (!TryCollapse(recipe, meta, random, out WfcTile[,] tiles))
                    continue;

                WarehouseLayoutData layout = BuildLayout(recipe, meta, tiles, random);
                WarehouseFitnessReport report = Evaluate(recipe, layout);
                if (report.PenaltyScore < best.Score)
                {
                    best.Score = report.PenaltyScore;
                    best.Layout = layout;
                    best.Report = report;
                }
            }

            bool fallbackUsed = best.Layout == null;
            if (fallbackUsed)
            {
                best.Layout = CreateFallbackLayout(recipe, random);
                best.Report = Evaluate(recipe, best.Layout);
            }

            WarehousePlacementRules.CleanupLayout(best.Layout, null, recipe.TallContainerProbability);
            SealGroundPockets(recipe, best.Layout);
            best.Report = Evaluate(recipe, best.Layout);
            if (fallbackUsed)
            {
                best.Report.PenaltyScore += recipe.FailedPenalty;
                best.Report.Violations.Add("WFC fallback layout used.");
            }

            best.Score = best.Report.PenaltyScore;
            WarehouseContainerPalettePass.Apply(recipe, best.Layout, context.CreateRandom("warehouse-container-palette"));
            WarehouseDecorationGenerator.Populate(recipe, best.Layout, context.CreateRandom("warehouse-decoration"));
            WarehouseNavigationGrid navigation = new(
                best.Layout,
                recipe.PartialCoverPathCost,
                recipe.FullCoverPathCost);
            context.Blackboard.Set(WarehouseKeys.Layout, best.Layout);
            context.Blackboard.Set(WarehouseKeys.Navigation, navigation);
            context.Blackboard.Set(WarehouseKeys.Fitness, best.Report);
            context.Blackboard.Set(WarehouseKeys.BuildPlan, new WarehouseBuildPlan
            {
                Layout = best.Layout,
                Fitness = best.Report
            });

            context.Diagnostics.Info($"WFC best score={best.Score}, objects={best.Layout.Objects.Count}.", Id);
        }

        private static void SealGroundPockets(WarehouseWfcRecipe recipe, WarehouseLayoutData layout)
        {
            if (recipe.GroundPocketSealIterationCount <= 0 || recipe.MaxGroundPocketSealCellCount <= 0)
                return;

            bool[,] reservedGround = WarehouseGenerationUtility.BuildReservedGroundMask(layout);
            for (int i = 0; i < recipe.GroundPocketSealIterationCount; i++)
            {
                int changes = WarehousePlacementRules.SealUnreachableGroundPockets(
                    layout,
                    reservedGround,
                    recipe.MaxGroundPocketSealCellCount);
                if (changes == 0)
                    break;
            }
        }

        private static WarehouseLayoutData CreateLayout(WarehouseWfcRecipe recipe, System.Random random)
        {
            int width = recipe.Width;
            int height = recipe.Height;
            int centerX = Mathf.Clamp(Mathf.FloorToInt(recipe.SpawnA.x), 1, width - 2);
            int lowerY = Mathf.Clamp(random.Next(4, 7), 1, Mathf.Max(1, height / 2 - 2));
            int upperY = Mathf.Clamp(random.Next(10, 13), lowerY + 1, Mathf.Max(lowerY + 1, height / 2 - 1));

            return new WarehouseLayoutData
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
                LeftLaneX = Mathf.Clamp(centerX - random.Next(3, 6), 1, width - 2),
                RightLaneX = Mathf.Clamp(centerX + random.Next(3, 6), 1, width - 2),
                LowerConnectorY = lowerY,
                UpperConnectorY = upperY
            };
        }

        private static bool TryCollapse(
            WarehouseWfcRecipe recipe,
            WarehouseLayoutData layout,
            System.Random random,
            out WfcTile[,] tiles)
        {
            int width = layout.Width;
            int height = layout.Height / 2;
            int[,] domains = CreateInitialDomains(recipe, layout, random);
            Queue<Vector2Int> queue = new();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                    queue.Enqueue(new Vector2Int(x, y));
            }

            if (!Propagate(recipe, domains, queue))
            {
                tiles = null;
                return false;
            }

            while (TryFindLowestEntropyCell(recipe, domains, random, out Vector2Int cell))
            {
                domains[cell.x, cell.y] = TileToMask(ChooseTile(recipe, domains[cell.x, cell.y], random));
                queue.Enqueue(cell);
                if (!Propagate(recipe, domains, queue))
                {
                    tiles = null;
                    return false;
                }
            }

            tiles = new WfcTile[width, height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                    tiles[x, y] = FirstTile(domains[x, y]);
            }

            return true;
        }

        private static int[,] CreateInitialDomains(
            WarehouseWfcRecipe recipe,
            WarehouseLayoutData layout,
            System.Random random)
        {
            int width = layout.Width;
            int height = layout.Height / 2;
            int[,] domains = new int[width, height];
            bool[,] reserved = BuildWfcReservedGroundMask(recipe, layout, random);
            int fullMask = AllMask();
            int clearMask = TileToMask(WfcTile.Empty);
            int reservedMask = TileToMask(WfcTile.Empty) | TileToMask(WfcTile.BridgeHorizontal) | TileToMask(WfcTile.BridgeVertical);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    WarehouseObjectPlacement probe = new()
                    {
                        Kind = WarehouseObjectKind.ContainerLow,
                        Side = WarehouseSide.A,
                        Origin = new Vector2Int(x, y),
                        Size = Vector2Int.one
                    };

                    if (WarehouseRepairPass.ObjectTouchesRadius(probe, layout.SpawnA, layout.SpawnClearRadius) ||
                        WarehouseRepairPass.ObjectTouchesRadius(probe, layout.CapturePoint, layout.CaptureClearRadius))
                    {
                        domains[x, y] = clearMask;
                    }
                    else if (reserved[x, y])
                    {
                        domains[x, y] = reservedMask;
                    }
                    else
                    {
                        domains[x, y] = fullMask;
                    }

                    domains[x, y] &= GetBoundaryMask(x, y, width, height);
                }
            }

            return domains;
        }

        private static int GetBoundaryMask(int x, int y, int width, int height)
        {
            int mask = AllMask();
            if (x == 0)
                mask &= ~TileToMask(WfcTile.LadderWest) & ~TileToMask(WfcTile.BridgeHorizontal);
            if (x == width - 1)
                mask &= ~TileToMask(WfcTile.LadderEast) & ~TileToMask(WfcTile.BridgeHorizontal);
            if (y == 0)
                mask &= ~TileToMask(WfcTile.LadderSouth) & ~TileToMask(WfcTile.BridgeVertical);
            if (y == height - 1)
                mask &= ~TileToMask(WfcTile.LadderNorth) & ~TileToMask(WfcTile.BridgeVertical);

            return mask;
        }

        private static bool[,] BuildWfcReservedGroundMask(
            WarehouseWfcRecipe recipe,
            WarehouseLayoutData layout,
            System.Random random)
        {
            bool[,] reserved = new bool[layout.Width, layout.Height / 2];
            int centerX = Mathf.Clamp(Mathf.FloorToInt(layout.SpawnA.x), 1, layout.Width - 2);
            int captureX = Mathf.Clamp(Mathf.FloorToInt(layout.CapturePoint.x), 1, layout.Width - 2);
            int mirrorCaptureX = Mathf.Clamp(layout.Width - captureX - 1, 1, layout.Width - 2);
            int startY = Mathf.Clamp(Mathf.FloorToInt(layout.SpawnA.y), 0, reserved.GetLength(1) - 1);
            int endY = reserved.GetLength(1) - 1;
            ReserveNoisyPath(reserved, recipe, random, centerX, startY, captureX, endY);
            ReserveNoisyPath(reserved, recipe, random, centerX, startY, mirrorCaptureX, endY);
            return reserved;
        }

        private static void ReserveNoisyPath(
            bool[,] reserved,
            WarehouseWfcRecipe recipe,
            System.Random random,
            int startX,
            int startY,
            int endX,
            int endY)
        {
            int waypointCount = random.Next(recipe.ReservedPathMinWaypoints, recipe.ReservedPathMaxWaypoints + 1);
            List<Vector2Int> points = new(waypointCount + 2)
            {
                new(Mathf.Clamp(startX, 1, reserved.GetLength(0) - 1), Mathf.Clamp(startY, 0, reserved.GetLength(1) - 1))
            };

            int lastY = startY;
            for (int i = 1; i <= waypointCount; i++)
            {
                float t = i / (float)(waypointCount + 1);
                int y = Mathf.RoundToInt(Mathf.Lerp(startY, endY, t)) + random.Next(-1, 2);
                int minY = Mathf.Min(endY, lastY + 1);
                int maxY = Mathf.Max(minY, endY - 1);
                y = Mathf.Clamp(y, minY, maxY);

                int x = Mathf.RoundToInt(Mathf.Lerp(startX, endX, t));
                x += random.Next(-recipe.ReservedPathJitterCells, recipe.ReservedPathJitterCells + 1);
                x = Mathf.Clamp(x, 1, reserved.GetLength(0) - 1);

                points.Add(new Vector2Int(x, y));
                lastY = y;
            }

            points.Add(new Vector2Int(
                Mathf.Clamp(endX, 1, reserved.GetLength(0) - 1),
                Mathf.Clamp(endY, 0, reserved.GetLength(1) - 1)));

            for (int i = 1; i < points.Count; i++)
                ReserveBentSegment(reserved, random, points[i - 1], points[i]);
        }

        private static void ReserveBentSegment(bool[,] reserved, System.Random random, Vector2Int from, Vector2Int to)
        {
            if (random.NextDouble() < 0.5)
            {
                ReserveVertical(reserved, from.x, from.y, to.y);
                ReserveHorizontal(reserved, from.x, to.x, to.y);
                return;
            }

            ReserveHorizontal(reserved, from.x, to.x, from.y);
            ReserveVertical(reserved, to.x, from.y, to.y);
        }

        private static void ReserveHorizontal(bool[,] reserved, int x0, int x1, int y)
        {
            int minX = Mathf.Min(x0, x1);
            int maxX = Mathf.Max(x0, x1);
            for (int x = minX; x <= maxX; x++)
                ReserveWide(reserved, x, y);
        }

        private static void ReserveVertical(bool[,] reserved, int x, int y0, int y1)
        {
            int minY = Mathf.Min(y0, y1);
            int maxY = Mathf.Max(y0, y1);
            for (int y = minY; y <= maxY; y++)
                ReserveWide(reserved, x, y);
        }

        private static void ReserveWide(bool[,] reserved, int x, int y)
        {
            for (int dx = -1; dx <= 0; dx++)
            {
                int rx = x + dx;
                if (rx >= 0 && rx < reserved.GetLength(0) && y >= 0 && y < reserved.GetLength(1))
                    reserved[rx, y] = true;
            }
        }

        private static bool Propagate(WarehouseWfcRecipe recipe, int[,] domains, Queue<Vector2Int> queue)
        {
            int steps = 0;
            int width = domains.GetLength(0);
            int height = domains.GetLength(1);
            while (queue.Count > 0)
            {
                if (++steps > recipe.PropagationStepLimit)
                    return false;

                Vector2Int cell = queue.Dequeue();
                int sourceMask = domains[cell.x, cell.y];
                if (sourceMask == 0)
                    return false;

                for (int i = 0; i < WarehouseGenerationUtility.CardinalDirections.Length; i++)
                {
                    Vector2Int dir = WarehouseGenerationUtility.CardinalDirections[i];
                    Vector2Int next = cell + dir;
                    if (next.x < 0 || next.x >= width || next.y < 0 || next.y >= height)
                        continue;

                    int oldMask = domains[next.x, next.y];
                    int newMask = ConstrainNeighbourMask(sourceMask, oldMask, dir);
                    if (newMask == 0)
                        return false;

                    if (newMask != oldMask)
                    {
                        domains[next.x, next.y] = newMask;
                        queue.Enqueue(next);
                    }
                }
            }

            return true;
        }

        private static int ConstrainNeighbourMask(int sourceMask, int neighbourMask, Vector2Int direction)
        {
            int result = 0;
            for (int tileIndex = 0; tileIndex < TileCount; tileIndex++)
            {
                WfcTile neighbour = (WfcTile)tileIndex;
                int neighbourBit = 1 << tileIndex;
                if ((neighbourMask & neighbourBit) == 0)
                    continue;

                bool compatible = false;
                for (int sourceIndex = 0; sourceIndex < TileCount; sourceIndex++)
                {
                    int sourceBit = 1 << sourceIndex;
                    if ((sourceMask & sourceBit) == 0)
                        continue;

                    WfcTile source = (WfcTile)sourceIndex;
                    if (ArePairCompatible(source, neighbour, direction) &&
                        ArePairCompatible(neighbour, source, -direction))
                    {
                        compatible = true;
                        break;
                    }
                }

                if (compatible)
                    result |= neighbourBit;
            }

            return result;
        }

        private static bool ArePairCompatible(WfcTile source, WfcTile neighbour, Vector2Int direction)
        {
            return source switch
            {
                WfcTile.LadderNorth => direction != Vector2Int.up || neighbour == WfcTile.ContainerLow,
                WfcTile.LadderSouth => direction != Vector2Int.down || neighbour == WfcTile.ContainerLow,
                WfcTile.LadderEast => direction != Vector2Int.right || neighbour == WfcTile.ContainerLow,
                WfcTile.LadderWest => direction != Vector2Int.left || neighbour == WfcTile.ContainerLow,
                WfcTile.BridgeHorizontal => direction != Vector2Int.left && direction != Vector2Int.right || IsHorizontalBridgeConnector(neighbour),
                WfcTile.BridgeVertical => direction != Vector2Int.up && direction != Vector2Int.down || IsVerticalBridgeConnector(neighbour),
                _ => true
            };
        }

        private static bool IsHorizontalBridgeConnector(WfcTile tile)
        {
            return tile == WfcTile.ContainerLow || tile == WfcTile.BridgeHorizontal;
        }

        private static bool IsVerticalBridgeConnector(WfcTile tile)
        {
            return tile == WfcTile.ContainerLow || tile == WfcTile.BridgeVertical;
        }

        private static bool TryFindLowestEntropyCell(
            WarehouseWfcRecipe recipe,
            int[,] domains,
            System.Random random,
            out Vector2Int cell)
        {
            float best = float.MaxValue;
            cell = default;
            bool found = false;
            for (int y = 0; y < domains.GetLength(1); y++)
            {
                for (int x = 0; x < domains.GetLength(0); x++)
                {
                    int count = CountBits(domains[x, y]);
                    if (count <= 1)
                        continue;

                    float entropy = count + (float)random.NextDouble() * recipe.EntropyNoiseValue;
                    if (entropy < best)
                    {
                        best = entropy;
                        cell = new Vector2Int(x, y);
                        found = true;
                    }
                }
            }

            return found;
        }

        private static WfcTile ChooseTile(WarehouseWfcRecipe recipe, int mask, System.Random random)
        {
            float total = 0f;
            for (int i = 0; i < TileCount; i++)
            {
                if ((mask & (1 << i)) != 0)
                    total += GetTileWeight(recipe, (WfcTile)i);
            }

            double roll = random.NextDouble() * total;
            float current = 0f;
            for (int i = 0; i < TileCount; i++)
            {
                if ((mask & (1 << i)) == 0)
                    continue;

                current += GetTileWeight(recipe, (WfcTile)i);
                if (roll <= current)
                    return (WfcTile)i;
            }

            return WfcTile.Empty;
        }

        private static float GetTileWeight(WarehouseWfcRecipe recipe, WfcTile tile)
        {
            return tile switch
            {
                WfcTile.Empty => recipe.EmptyTileWeight,
                WfcTile.ContainerLow => recipe.ContainerLowTileWeight,
                WfcTile.ContainerHigh => recipe.ContainerHighTileWeight,
                WfcTile.BridgeHorizontal => recipe.BridgeTileWeight,
                WfcTile.BridgeVertical => recipe.BridgeTileWeight,
                WfcTile.LadderNorth => recipe.LadderTileWeight,
                WfcTile.LadderSouth => recipe.LadderTileWeight,
                WfcTile.LadderEast => recipe.LadderTileWeight,
                WfcTile.LadderWest => recipe.LadderTileWeight,
                _ => 1f
            };
        }

        private static WarehouseLayoutData BuildLayout(
            WarehouseWfcRecipe recipe,
            WarehouseLayoutData meta,
            WfcTile[,] tiles,
            System.Random random)
        {
            WarehouseLayoutData layout = CloneLayoutMeta(meta);
            for (int y = 0; y < tiles.GetLength(1); y++)
            {
                for (int x = 0; x < tiles.GetLength(0); x++)
                    AddTileObject(recipe, layout, random, tiles[x, y], new Vector2Int(x, y));
            }

            AddCovers(recipe, layout, random);
            MirrorSourceHalf(layout);
            return layout;
        }

        private static void AddTileObject(
            WarehouseWfcRecipe recipe,
            WarehouseLayoutData layout,
            System.Random random,
            WfcTile tile,
            Vector2Int cell)
        {
            WarehouseObjectPlacement obj = tile switch
            {
                WfcTile.ContainerLow => new WarehouseObjectPlacement { Kind = WarehouseObjectKind.ContainerLow },
                WfcTile.ContainerHigh => new WarehouseObjectPlacement { Kind = WarehouseObjectKind.ContainerHigh },
                WfcTile.BridgeHorizontal => CreateBridge(horizontal: true),
                WfcTile.BridgeVertical => CreateBridge(horizontal: false),
                WfcTile.LadderNorth => CreateLadder(WarehouseDirection.North),
                WfcTile.LadderSouth => CreateLadder(WarehouseDirection.South),
                WfcTile.LadderEast => CreateLadder(WarehouseDirection.East),
                WfcTile.LadderWest => CreateLadder(WarehouseDirection.West),
                _ => null
            };

            if (obj == null)
                return;

            obj.Side = WarehouseSide.A;
            obj.Origin = cell;
            obj.Size = Vector2Int.one;
            if (obj.IsLadder)
                obj.VariantIndex = WarehouseVariantSelector.ChooseClimbAccessVariantIndex(recipe, layout, obj, random);
            else if (obj.Kind == WarehouseObjectKind.Bridge)
                obj.VariantIndex = WarehouseVariantSelector.ChooseBridgeVariantIndex(recipe, layout, obj, random);

            layout.Objects.Add(obj);
        }

        private static WarehouseObjectPlacement CreateLadder(WarehouseDirection direction)
        {
            return new WarehouseObjectPlacement
            {
                Kind = WarehouseObjectKind.Ladder,
                Direction = direction,
                RotationY = DirectionToRotation(direction)
            };
        }

        private static WarehouseObjectPlacement CreateBridge(bool horizontal)
        {
            WarehouseDirectionMask mask = horizontal
                ? WarehouseDirectionMask.East | WarehouseDirectionMask.West
                : WarehouseDirectionMask.North | WarehouseDirectionMask.South;

            return new WarehouseObjectPlacement
            {
                Kind = WarehouseObjectKind.Bridge,
                Direction = WarehouseGenerationUtility.BridgeDirectionFromMask(mask),
                ConnectionMask = mask,
                BridgeConnectionType = WarehouseBridgeConnectionType.Straight,
                RotationY = WarehouseGenerationUtility.BridgeRotationFromMask(mask)
            };
        }

        private static void AddCovers(WarehouseWfcRecipe recipe, WarehouseLayoutData layout, System.Random random)
        {
            bool[,] structure = new bool[layout.Width, layout.Height / 2];
            bool[,] top = new bool[layout.Width, layout.Height / 2];
            bool[,] ladder = new bool[layout.Width, layout.Height / 2];
            bool[,] groundCover = new bool[layout.Width, layout.Height / 2];
            bool[,] topCover = new bool[layout.Width, layout.Height / 2];
            for (int i = 0; i < layout.Objects.Count; i++)
            {
                WarehouseObjectPlacement obj = layout.Objects[i];
                if (obj.Origin.y >= layout.Height / 2)
                    continue;

                if (obj.IsStructure)
                    structure[obj.Origin.x, obj.Origin.y] = true;
                if (obj.IsTopWalkableSource)
                    top[obj.Origin.x, obj.Origin.y] = true;
                if (obj.IsLadder)
                    ladder[obj.Origin.x, obj.Origin.y] = true;
            }

            int target = Mathf.RoundToInt(layout.Width * layout.Height * recipe.TargetCoverRatio / 2f);
            List<CoverCandidate> candidates = new();
            for (int y = 0; y < layout.Height / 2; y++)
            {
                for (int x = 0; x < layout.Width; x++)
                {
                    Vector2Int cell = new(x, y);
                    WarehouseObjectPlacement probe = new()
                    {
                        Kind = WarehouseObjectKind.PartialCover,
                        Side = WarehouseSide.A,
                        Origin = cell,
                        Size = Vector2Int.one
                    };

                    if (WarehouseRepairPass.ObjectTouchesRadius(probe, layout.SpawnA, layout.SpawnClearRadius))
                        continue;

                    if (!structure[x, y] && !ladder[x, y])
                        candidates.Add(new CoverCandidate(cell, WarehousePlacementSurface.Ground));
                    if (top[x, y] && !ladder[x, y])
                        candidates.Add(new CoverCandidate(cell, WarehousePlacementSurface.StructureTop));
                }
            }

            WarehouseGenerationUtility.Shuffle(candidates, random);
            int placed = 0;
            for (int i = 0; i < candidates.Count && placed < target; i++)
            {
                CoverCandidate candidate = candidates[i];
                bool isTop = candidate.Surface == WarehousePlacementSurface.StructureTop;
                if (isTop && topCover[candidate.Cell.x, candidate.Cell.y] ||
                    !isTop && groundCover[candidate.Cell.x, candidate.Cell.y])
                {
                    continue;
                }

                WarehouseObjectKind kind = random.NextDouble() < recipe.FullCoverProbability
                    ? WarehouseObjectKind.FullCover
                    : WarehouseObjectKind.PartialCover;

                if (!WarehousePlacementRules.TryChooseCoverRotation(layout, candidate.Cell, candidate.Surface, random, recipe.CoverRandomRotationProbability, out float rotationY))
                    continue;

                WarehouseObjectPlacement placement = new()
                {
                    Kind = kind,
                    Side = WarehouseSide.A,
                    Origin = candidate.Cell,
                    Size = Vector2Int.one,
                    Surface = candidate.Surface,
                    Direction = RotationToDirection(rotationY),
                    RotationY = rotationY
                };

                placement.VariantIndex = WarehouseVariantSelector.ChooseCoverVariantIndex(recipe, layout, placement, random);
                if (placement.VariantIndex < 0)
                    placement.VariantIndex = ChooseCoverVariantIndex(recipe, random, kind);

                layout.Objects.Add(placement);

                if (isTop)
                    topCover[candidate.Cell.x, candidate.Cell.y] = true;
                else
                    groundCover[candidate.Cell.x, candidate.Cell.y] = true;

                placed++;
            }
        }

        private static WarehouseFitnessReport Evaluate(WarehouseWfcRecipe recipe, WarehouseLayoutData layout)
        {
            WarehouseFitnessReport report = new();
            WarehouseNavigationGrid navigation = new(
                layout,
                recipe.PartialCoverPathCost,
                recipe.FullCoverPathCost);
            int pathA = navigation.FindPathCost(layout.SpawnA, layout.CapturePoint);
            int pathB = navigation.FindPathCost(layout.SpawnB, layout.CapturePoint);
            int groundA = navigation.FindGroundPathCost(layout.SpawnA, layout.CapturePoint);
            int groundB = navigation.FindGroundPathCost(layout.SpawnB, layout.CapturePoint);
            report.PathCostA = pathA;
            report.PathCostB = pathB;

            if (pathA < 0)
                AddPenalty(report, InvalidPenalty, "No path from Spawn A.");
            if (pathB < 0)
                AddPenalty(report, InvalidPenalty, "No path from Spawn B.");
            if (groundA < 0)
                AddPenalty(report, InvalidPenalty, "No ground path from Spawn A.");
            if (groundB < 0)
                AddPenalty(report, InvalidPenalty, "No ground path from Spawn B.");
            ScoreWeightedPathCost(recipe, pathA, report, "Spawn A weighted path is too expensive.");
            ScoreWeightedPathCost(recipe, pathB, report, "Spawn B weighted path is too expensive.");
            if (pathA >= 0 && pathB >= 0)
                AddPenalty(report, Mathf.Max(0f, Mathf.Abs(pathA - pathB) - recipe.AllowedPathCostDifference) * 12f, "Path cost difference.");

            List<Vector2Int> groundPath = new();
            if (navigation.TryFindGroundPath(layout.SpawnA, layout.CapturePoint, groundPath))
                ScoreGroundPathShape(recipe, groundPath, report, "Spawn A ground path shape.");
            if (navigation.TryFindGroundPath(layout.SpawnB, layout.CapturePoint, groundPath))
                ScoreGroundPathShape(recipe, groundPath, report, "Spawn B ground path shape.");

            int structures = 0;
            int covers = 0;
            int captureCovers = 0;
            float captureCoverRadiusSqr = recipe.CaptureCoverRadiusValue * recipe.CaptureCoverRadiusValue;
            for (int i = 0; i < layout.Objects.Count; i++)
            {
                WarehouseObjectPlacement obj = layout.Objects[i];
                if (obj.IsStructure)
                    structures++;
                if (obj.IsCover)
                {
                    covers++;
                    if (recipe.CaptureCoverRadiusValue > 0f &&
                        (obj.Center - layout.CapturePoint).sqrMagnitude <= captureCoverRadiusSqr)
                    {
                        captureCovers++;
                    }
                }

                if ((obj.IsStructure || obj.IsCover) &&
                    (WarehouseRepairPass.ObjectTouchesRadius(obj, layout.SpawnA, layout.SpawnClearRadius) ||
                     WarehouseRepairPass.ObjectTouchesRadius(obj, layout.SpawnB, layout.SpawnClearRadius)))
                {
                    AddPenalty(report, InvalidPenalty, "Spawn clear zone violation.");
                }

                if (obj.IsStructure && WarehouseRepairPass.ObjectTouchesRadius(obj, layout.CapturePoint, layout.CaptureClearRadius))
                    AddPenalty(report, InvalidPenalty, "Capture clear zone violation.");
            }

            int totalCells = layout.Width * layout.Height;
            report.StructureCells = structures;
            report.TargetStructureCells = Mathf.RoundToInt(totalCells * recipe.TargetStructureRatio);
            report.MinStructureCells = Mathf.RoundToInt(totalCells * recipe.MinStructureRatio);
            report.MaxStructureCells = Mathf.RoundToInt(totalCells * recipe.MaxStructureRatio);
            if (structures < report.MinStructureCells)
                AddPenalty(report, (report.MinStructureCells - structures) * recipe.StructureDensityPenaltyWeight, "Too few structures.");
            if (structures > report.MaxStructureCells)
                AddPenalty(report, (structures - report.MaxStructureCells) * recipe.StructureDensityPenaltyWeight, "Too many structures.");

            int targetCovers = Mathf.RoundToInt(totalCells * recipe.TargetCoverRatio);
            AddPenalty(report, Mathf.Abs(covers - targetCovers) * 8f, "Cover count differs from target.");
            if (recipe.CaptureCoverRadiusValue > 0f && captureCovers < recipe.MinCaptureCovers)
                AddPenalty(report, (recipe.MinCaptureCovers - captureCovers) * recipe.CaptureCoverPenaltyWeight, "Too few covers near capture.");
            else if (recipe.CaptureCoverRadiusValue > 0f && captureCovers > recipe.MaxCaptureCovers)
                AddPenalty(report, (captureCovers - recipe.MaxCaptureCovers) * recipe.CaptureCoverPenaltyWeight, "Too many covers near capture.");
            return report;
        }

        private static void ScoreWeightedPathCost(
            WarehouseWfcRecipe recipe,
            int cost,
            WarehouseFitnessReport report,
            string violation)
        {
            if (cost < 0 || cost <= recipe.MaxAllowedWeightedPathCost)
                return;

            AddPenalty(report, (cost - recipe.MaxAllowedWeightedPathCost) * recipe.PathCostPenaltyWeight, violation);
        }

        private static void ScoreGroundPathShape(
            WarehouseWfcRecipe recipe,
            List<Vector2Int> path,
            WarehouseFitnessReport report,
            string label)
        {
            if (path.Count < 2)
                return;

            int turns = 0;
            int maxStraight = 1;
            int straight = 1;
            Vector2Int previousDirection = Vector2Int.zero;
            for (int i = 1; i < path.Count; i++)
            {
                Vector2Int direction = path[i] - path[i - 1];
                if (previousDirection == Vector2Int.zero)
                {
                    previousDirection = direction;
                    continue;
                }

                if (direction == previousDirection)
                {
                    straight++;
                }
                else
                {
                    turns++;
                    previousDirection = direction;
                    straight = 1;
                }

                maxStraight = Mathf.Max(maxStraight, straight);
            }

            int minTurns = recipe.MinGroundPathTurnCount;
            if (turns < minTurns)
                AddPenalty(report, (minTurns - turns) * recipe.GroundPathTurnPenaltyWeight, label);

            int maxStraightLimit = recipe.MaxStraightGroundSegmentCells;
            if (maxStraight > maxStraightLimit)
                AddPenalty(report, (maxStraight - maxStraightLimit) * recipe.GroundPathStraightPenaltyWeight, label);

            float directDistance = Vector2.Distance(path[0], path[path.Count - 1]);
            float directness = (path.Count - 1) / Mathf.Max(1f, directDistance);
            float minDirectness = recipe.MinGroundPathDirectnessValue;
            if (directness < minDirectness)
                AddPenalty(report, (minDirectness - directness) * recipe.GroundPathDirectnessPenaltyWeight, label);
        }

        private static WarehouseLayoutData CreateFallbackLayout(WarehouseWfcRecipe recipe, System.Random random)
        {
            WarehouseLayoutData layout = CreateLayout(recipe, random);
            int centerX = Mathf.FloorToInt(layout.SpawnA.x);
            for (int y = 4; y < layout.Height / 2; y += 3)
            {
                layout.Objects.Add(new WarehouseObjectPlacement
                {
                    Kind = WarehouseObjectKind.ContainerLow,
                    Side = WarehouseSide.A,
                    Origin = new Vector2Int(Mathf.Clamp(centerX - 4, 1, layout.Width - 2), y),
                    Size = Vector2Int.one
                });
                layout.Objects.Add(new WarehouseObjectPlacement
                {
                    Kind = WarehouseObjectKind.ContainerLow,
                    Side = WarehouseSide.A,
                    Origin = new Vector2Int(Mathf.Clamp(centerX + 4, 1, layout.Width - 2), y),
                    Size = Vector2Int.one
                });
            }

            MirrorSourceHalf(layout);
            return layout;
        }

        private static WarehouseLayoutData CloneLayoutMeta(WarehouseLayoutData source)
        {
            return new WarehouseLayoutData
            {
                Width = source.Width,
                Height = source.Height,
                CellSize = source.CellSize,
                CellSizeX = source.CellSizeX,
                CellSizeZ = source.CellSizeZ,
                SpawnA = source.SpawnA,
                SpawnB = source.SpawnB,
                CapturePoint = source.CapturePoint,
                SpawnClearRadius = source.SpawnClearRadius,
                CaptureClearRadius = source.CaptureClearRadius,
                LeftLaneX = source.LeftLaneX,
                RightLaneX = source.RightLaneX,
                LowerConnectorY = source.LowerConnectorY,
                UpperConnectorY = source.UpperConnectorY
            };
        }

        private static void MirrorSourceHalf(WarehouseLayoutData layout)
        {
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
                    VariantIndex = source.VariantIndex,
                    PaletteIndex = source.PaletteIndex
                });
            }
        }

        private static int ChooseCoverVariantIndex(WarehouseRecipe recipe, System.Random random, WarehouseObjectKind kind)
        {
            WarehousePrefabVariant[] variants = recipe.GetCoverVariants(kind);
            if (variants == null || variants.Length == 0)
                return -1;

            float total = 0f;
            for (int i = 0; i < variants.Length; i++)
            {
                if (variants[i] != null && variants[i].Prefab != null)
                    total += Mathf.Max(0f, variants[i].Weight);
            }

            if (total <= 0f)
                return -1;

            double roll = random.NextDouble() * total;
            float current = 0f;
            for (int i = 0; i < variants.Length; i++)
            {
                if (variants[i] == null || variants[i].Prefab == null)
                    continue;

                current += Mathf.Max(0f, variants[i].Weight);
                if (roll <= current)
                    return i;
            }

            return -1;
        }

        private static int AllMask()
        {
            int mask = 0;
            for (int i = 0; i < TileCount; i++)
                mask |= 1 << i;

            return mask;
        }

        private static int TileToMask(WfcTile tile)
        {
            return 1 << (int)tile;
        }

        private static WfcTile FirstTile(int mask)
        {
            for (int i = 0; i < TileCount; i++)
            {
                if ((mask & (1 << i)) != 0)
                    return (WfcTile)i;
            }

            return WfcTile.Empty;
        }

        private static int CountBits(int mask)
        {
            int count = 0;
            while (mask != 0)
            {
                count += mask & 1;
                mask >>= 1;
            }

            return count;
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

        private static void AddPenalty(WarehouseFitnessReport report, float penalty, string violation)
        {
            if (penalty <= 0f)
                return;

            report.PenaltyScore += penalty;
            report.Violations.Add(violation);
        }

        private enum WfcTile
        {
            Empty,
            ContainerLow,
            ContainerHigh,
            BridgeHorizontal,
            BridgeVertical,
            LadderNorth,
            LadderSouth,
            LadderEast,
            LadderWest
        }

        private sealed class WfcCandidate
        {
            public WarehouseLayoutData Layout;
            public WarehouseFitnessReport Report;
            public float Score;
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
}
