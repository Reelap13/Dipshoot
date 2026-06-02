using System.Collections.Generic;
using UnityEngine;

namespace Game.ProcGen.Warehouse
{
    public sealed class WarehouseEvolutionPass : ProcGenPass
    {
        private const float NoPathPenalty = 10000f;
        private const float NoGroundPathPenalty = 8000f;
        private const float InvalidObjectPenalty = 1500f;
        private const int CenterBandDepth = 5;

        public override string Id => "warehouse-evolution";

        public override void Declare(GenerationPassContract contract)
        {
            contract.Write(WarehouseKeys.Layout);
            contract.Write(WarehouseKeys.Navigation);
            contract.Write(WarehouseKeys.Fitness);
            contract.Write(WarehouseKeys.BuildPlan);
        }

        public override void Execute(GenerationContext context)
        {
            WarehouseEvolutionRecipe recipe = context.GetRecipe<WarehouseEvolutionRecipe>();
            System.Random random = context.CreateRandom(Id);
            List<WarehouseGenome> population = BuildInitialPopulation(recipe, random);
            Dictionary<int, CachedFitness> fitnessCache = new();

            EvaluatePopulation(recipe, population, fitnessCache);
            for (int generation = 0; generation < recipe.GenerationCount; generation++)
            {
                population.Sort(CompareGenomes);
                List<WarehouseGenome> next = new(recipe.PopulationCount);
                for (int i = 0; i < recipe.EliteKeepCount; i++)
                    next.Add(CloneGenome(population[i]));

                int childLimit = recipe.PopulationCount - recipe.RandomImmigrantKeepCount;
                while (next.Count < childLimit)
                {
                    WarehouseGenome parentA = TournamentSelect(population, recipe, random);
                    WarehouseGenome parentB = TournamentSelect(population, recipe, random);
                    WarehouseGenome child = Crossover(parentA, parentB, random);
                    bool aggressive = generation < recipe.GenerationCount / 3;
                    if (random.NextDouble() < recipe.MutationProbability || next.Count >= recipe.EliteKeepCount)
                        Mutate(child, recipe, random, aggressive);

                    next.Add(child);
                }

                while (next.Count < recipe.PopulationCount)
                    next.Add(CreateRandomGenome(recipe, random));

                population = next;
                EvaluatePopulation(recipe, population, fitnessCache);
            }

            population.Sort(CompareGenomes);
            WarehouseGenome best = population[0];
            WarehouseLayoutData layout = BuildFinalLayout(best);
            WarehousePlacementRules.CleanupLayout(layout, null, recipe.TallContainerProbability);
            SealGroundPockets(recipe, layout);
            WarehouseFitnessReport report = EvaluateLayout(recipe, layout);
            best.Score = report.PenaltyScore;
            WarehouseNavigationGrid navigation = new(
                layout,
                recipe.PartialCoverPathCost,
                recipe.FullCoverPathCost);

            context.Blackboard.Set(WarehouseKeys.Layout, layout);
            context.Blackboard.Set(WarehouseKeys.Navigation, navigation);
            context.Blackboard.Set(WarehouseKeys.Fitness, report);
            context.Blackboard.Set(WarehouseKeys.BuildPlan, new WarehouseBuildPlan
            {
                Layout = layout,
                Fitness = report
            });

            context.Diagnostics.Info($"Evolution best score={best.Score}, cache={fitnessCache.Count}, objects={layout.Objects.Count}.", Id);
        }

        private static void SealGroundPockets(WarehouseEvolutionRecipe recipe, WarehouseLayoutData layout)
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

        private static List<WarehouseGenome> BuildInitialPopulation(
            WarehouseEvolutionRecipe recipe,
            System.Random random)
        {
            List<WarehouseGenome> population = new(recipe.PopulationCount);
            for (int i = 0; i < recipe.PopulationCount; i++)
                population.Add(CreateRandomGenome(recipe, random));

            return population;
        }

        private static WarehouseGenome CreateRandomGenome(WarehouseEvolutionRecipe recipe, System.Random random)
        {
            int width = recipe.Width;
            int height = recipe.Height;
            int centerX = Mathf.Clamp(Mathf.FloorToInt(recipe.SpawnA.x), 1, width - 2);
            int lowerY = Mathf.Clamp(random.Next(4, 7), 1, Mathf.Max(1, height / 2 - 2));
            int upperY = Mathf.Clamp(random.Next(10, 13), lowerY + 1, Mathf.Max(lowerY + 1, height / 2 - 1));

            WarehouseGenome genome = new()
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

            AddRandomContainers(genome, recipe, random);
            AddRandomBridges(genome, recipe, random);
            AddRandomLadders(genome, recipe, random);
            AddRandomCovers(genome, recipe, random);
            RepairGenome(recipe, genome);
            return genome;
        }

        private static void AddRandomContainers(
            WarehouseGenome genome,
            WarehouseEvolutionRecipe recipe,
            System.Random random)
        {
            List<Vector2Int> cells = BuildSourceCells(genome);
            WarehouseGenerationUtility.Shuffle(cells, random);
            int target = Mathf.RoundToInt(genome.Width * genome.Height * recipe.InitialStructureRatio * 0.5f);
            bool[,] occupied = new bool[genome.Width, genome.Height / 2];
            int placed = 0;

            for (int i = 0; i < cells.Count && placed < target; i++)
            {
                Vector2Int cell = cells[i];
                WarehouseObjectKind kind = random.NextDouble() < recipe.TallContainerProbability
                    ? WarehouseObjectKind.ContainerHigh
                    : WarehouseObjectKind.ContainerLow;
                WarehouseObjectPlacement obj = new()
                {
                    Kind = kind,
                    Side = WarehouseSide.A,
                    Origin = cell,
                    Size = Vector2Int.one,
                    RotationY = 0f
                };

                if (occupied[cell.x, cell.y] || !CanPlaceInitialStructure(genome, obj))
                    continue;

                genome.Objects.Add(obj);
                occupied[cell.x, cell.y] = true;
                placed++;
            }
        }

        private static void AddRandomBridges(
            WarehouseGenome genome,
            WarehouseEvolutionRecipe recipe,
            System.Random random)
        {
            List<BridgeCandidate> candidates = new();
            SourceGridCache grid = new(genome);
            for (int y = 0; y < genome.Height / 2; y++)
            {
                for (int x = 0; x < genome.Width; x++)
                {
                    Vector2Int cell = new(x, y);
                    AddBridgeCandidate(genome, grid, candidates, cell, horizontal: true);
                    AddBridgeCandidate(genome, grid, candidates, cell, horizontal: false);
                }
            }

            WarehouseGenerationUtility.Shuffle(candidates, random);
            int target = random.Next(0, recipe.MaxBridgeCellCount + 1);
            bool[,] occupied = BuildStructureOccupied(genome);
            int placed = 0;
            for (int i = 0; i < candidates.Count && placed < target; i++)
            {
                BridgeCandidate candidate = candidates[i];
                if (occupied[candidate.Cell.x, candidate.Cell.y])
                    continue;

                WarehouseObjectPlacement obj = new()
                {
                    Kind = WarehouseObjectKind.Bridge,
                    Side = WarehouseSide.A,
                    Origin = candidate.Cell,
                    Size = Vector2Int.one,
                    Direction = candidate.Horizontal ? WarehouseDirection.East : WarehouseDirection.North,
                    ConnectionMask = GetStraightBridgeMask(candidate.Horizontal),
                    BridgeConnectionType = WarehouseBridgeConnectionType.Straight,
                    RotationY = 0f
                };

                if (!CanPlaceInitialStructure(genome, obj))
                    continue;

                genome.Objects.Add(obj);
                occupied[candidate.Cell.x, candidate.Cell.y] = true;
                placed++;
            }
        }

        private static void AddRandomLadders(
            WarehouseGenome genome,
            WarehouseEvolutionRecipe recipe,
            System.Random random)
        {
            List<WarehouseObjectPlacement> candidates = new();
            SourceGridCache grid = new(genome);
            for (int y = 0; y < genome.Height / 2; y++)
            {
                for (int x = 0; x < genome.Width; x++)
                {
                    if (!grid.ContainerLow[x, y])
                        continue;

                    Vector2Int topCell = new(x, y);
                    for (int i = 0; i < WarehouseGenerationUtility.CardinalDirections.Length; i++)
                    {
                        Vector2Int direction = WarehouseGenerationUtility.CardinalDirections[i];
                        Vector2Int groundCell = topCell - direction;
                        if (!IsSourceCell(genome, groundCell) ||
                            grid.Structure[groundCell.x, groundCell.y] ||
                            grid.Ladder[groundCell.x, groundCell.y])
                        {
                            continue;
                        }

                        candidates.Add(new WarehouseObjectPlacement
                        {
                            Kind = WarehouseObjectKind.Ladder,
                            Side = WarehouseSide.A,
                            Origin = groundCell,
                            Size = Vector2Int.one,
                            Direction = VectorToDirection(direction),
                            RotationY = DirectionToRotation(VectorToDirection(direction))
                        });
                    }
                }
            }

            WarehouseGenerationUtility.Shuffle(candidates, random);
            int lowContainers = CountOccupied(grid.ContainerLow);
            int target = Mathf.Max(1, lowContainers / recipe.LadderSpacing);
            bool[,] ladder = new bool[genome.Width, genome.Height / 2];
            int placed = 0;
            for (int i = 0; i < candidates.Count && placed < target; i++)
            {
                WarehouseObjectPlacement obj = candidates[i];
                if (ladder[obj.Origin.x, obj.Origin.y] ||
                    WarehouseRepairPass.ObjectTouchesRadius(obj, genome.SpawnA, genome.SpawnClearRadius))
                {
                    continue;
                }

                genome.Objects.Add(obj);
                ladder[obj.Origin.x, obj.Origin.y] = true;
                placed++;
            }
        }

        private static void AddRandomCovers(
            WarehouseGenome genome,
            WarehouseEvolutionRecipe recipe,
            System.Random random)
        {
            SourceGridCache grid = new(genome);
            List<CoverCandidate> candidates = new();
            for (int y = 0; y < genome.Height / 2; y++)
            {
                for (int x = 0; x < genome.Width; x++)
                {
                    Vector2Int cell = new(x, y);
                    if (!grid.Structure[x, y] && !grid.Ladder[x, y])
                        candidates.Add(new CoverCandidate(cell, WarehousePlacementSurface.Ground));
                    if (grid.TopWalkable[x, y] && !grid.Ladder[x, y])
                        candidates.Add(new CoverCandidate(cell, WarehousePlacementSurface.StructureTop));
                }
            }

            WarehouseGenerationUtility.Shuffle(candidates, random);
            int target = Mathf.RoundToInt(genome.Width * genome.Height * recipe.TargetCoverRatio * 0.5f);
            bool[,] groundCover = new bool[genome.Width, genome.Height / 2];
            bool[,] topCover = new bool[genome.Width, genome.Height / 2];
            int placed = 0;
            for (int i = 0; i < candidates.Count && placed < target; i++)
            {
                CoverCandidate candidate = candidates[i];
                bool top = candidate.Surface == WarehousePlacementSurface.StructureTop;
                if (top && topCover[candidate.Cell.x, candidate.Cell.y] ||
                    !top && groundCover[candidate.Cell.x, candidate.Cell.y])
                {
                    continue;
                }

                WarehouseObjectKind kind = random.NextDouble() < recipe.FullCoverProbability
                    ? WarehouseObjectKind.FullCover
                    : WarehouseObjectKind.PartialCover;
                float rotationY = random.Next(0, 4) * 90f;
                WarehouseObjectPlacement obj = new()
                {
                    Kind = kind,
                    Side = WarehouseSide.A,
                    Origin = candidate.Cell,
                    Size = Vector2Int.one,
                    Surface = candidate.Surface,
                    Direction = RotationToDirection(rotationY),
                    RotationY = rotationY,
                    VariantIndex = HasNewCoverVariants(recipe) ? -1 : ChooseCoverVariantIndex(recipe, random, kind)
                };

                if (WarehouseRepairPass.ObjectTouchesRadius(obj, genome.SpawnA, genome.SpawnClearRadius))
                    continue;

                genome.Objects.Add(obj);
                if (top)
                    topCover[candidate.Cell.x, candidate.Cell.y] = true;
                else
                    groundCover[candidate.Cell.x, candidate.Cell.y] = true;
                placed++;
            }
        }

        private static List<Vector2Int> BuildSourceCells(WarehouseGenome genome)
        {
            List<Vector2Int> cells = new(genome.Width * genome.Height / 2);
            for (int y = 0; y < genome.Height / 2; y++)
            {
                for (int x = 0; x < genome.Width; x++)
                    cells.Add(new Vector2Int(x, y));
            }

            return cells;
        }

        private static bool CanPlaceInitialStructure(WarehouseGenome genome, WarehouseObjectPlacement obj)
        {
            return IsInsideSource(genome, obj) &&
                   !WarehouseRepairPass.ObjectTouchesRadius(obj, genome.SpawnA, genome.SpawnClearRadius) &&
                   !(obj.IsStructure && WarehouseRepairPass.ObjectTouchesRadius(obj, genome.CapturePoint, genome.CaptureClearRadius));
        }

        private static bool[,] BuildStructureOccupied(WarehouseGenome genome)
        {
            bool[,] occupied = new bool[genome.Width, genome.Height / 2];
            for (int i = 0; i < genome.Objects.Count; i++)
            {
                WarehouseObjectPlacement obj = genome.Objects[i];
                if (obj.IsStructure && IsSourceCell(genome, obj.Origin))
                    occupied[obj.Origin.x, obj.Origin.y] = true;
            }

            return occupied;
        }

        private static void AddBridgeCandidate(
            WarehouseGenome genome,
            SourceGridCache grid,
            List<BridgeCandidate> candidates,
            Vector2Int cell,
            bool horizontal)
        {
            if (!IsSourceCell(genome, cell) || grid.Structure[cell.x, cell.y])
                return;

            Vector2Int axis = horizontal ? Vector2Int.right : Vector2Int.up;
            Vector2Int before = cell - axis;
            Vector2Int after = cell + axis;
            if (IsSourceCell(genome, before) &&
                IsSourceCell(genome, after) &&
                grid.ContainerLow[before.x, before.y] &&
                grid.ContainerLow[after.x, after.y])
            {
                candidates.Add(new BridgeCandidate(cell, horizontal));
            }
        }

        private static void EvaluatePopulation(
            WarehouseEvolutionRecipe recipe,
            List<WarehouseGenome> population,
            Dictionary<int, CachedFitness> fitnessCache)
        {
            Dictionary<int, int> hashCounts = new();
            for (int i = 0; i < population.Count; i++)
            {
                RepairGenome(recipe, population[i]);
                FitnessResult result = EvaluateGenome(recipe, population[i], fitnessCache);
                population[i].Score = result.Score;
                population[i].Fitness = result.Report;
                population[i].Hash = result.Hash;

                hashCounts.TryGetValue(result.Hash, out int count);
                hashCounts[result.Hash] = count + 1;
                if (count > 0)
                {
                    float penalty = recipe.DuplicatePenalty * count;
                    population[i].Score += penalty;
                    population[i].Fitness.PenaltyScore += penalty;
                    population[i].Fitness.Violations.Add("Duplicate evolution genome.");
                }
            }
        }

        private static FitnessResult EvaluateGenome(
            WarehouseEvolutionRecipe recipe,
            WarehouseGenome genome,
            Dictionary<int, CachedFitness> fitnessCache)
        {
            int hash = ComputeHash(genome);
            if (fitnessCache.TryGetValue(hash, out CachedFitness cached))
                return new FitnessResult(hash, cached.Score, CloneReport(cached.Report));

            WarehouseLayoutData layout = BuildFinalLayout(genome);
            WarehouseFitnessReport report = EvaluateLayout(recipe, layout);
            fitnessCache[hash] = new CachedFitness(report.PenaltyScore, CloneReport(report));
            return new FitnessResult(hash, report.PenaltyScore, report);
        }

        private static WarehouseFitnessReport EvaluateLayout(
            WarehouseEvolutionRecipe recipe,
            WarehouseLayoutData layout)
        {
            WarehouseFitnessReport report = new();
            WarehouseGridCache grid = new(layout);
            ScoreClearZones(layout, report);
            ScoreDensity(recipe, layout, grid, report);

            if (report.PenaltyScore < NoPathPenalty)
            {
                WarehouseNavigationGrid navigation = new(
                    layout,
                    recipe.PartialCoverPathCost,
                    recipe.FullCoverPathCost);
                ScorePaths(recipe, layout, navigation, report);
                ScoreTopAccess(recipe, layout, grid, report);
                ScoreBridgeQuality(recipe, layout, grid, report);
                ScoreChokepoints(layout, grid, report);
                ScoreCoverQuality(recipe, layout, report);
            }

            return report;
        }

        private static void ScoreClearZones(WarehouseLayoutData layout, WarehouseFitnessReport report)
        {
            for (int i = 0; i < layout.Objects.Count; i++)
            {
                WarehouseObjectPlacement obj = layout.Objects[i];
                if ((obj.IsStructure || obj.IsCover) &&
                    (WarehouseRepairPass.ObjectTouchesRadius(obj, layout.SpawnA, layout.SpawnClearRadius) ||
                     WarehouseRepairPass.ObjectTouchesRadius(obj, layout.SpawnB, layout.SpawnClearRadius)))
                {
                    AddPenalty(report, InvalidObjectPenalty, "Object inside spawn clear zone.");
                }

                if (obj.IsStructure && WarehouseRepairPass.ObjectTouchesRadius(obj, layout.CapturePoint, layout.CaptureClearRadius))
                    AddPenalty(report, InvalidObjectPenalty, "Structure inside capture clear zone.");
            }
        }

        private static void ScoreDensity(
            WarehouseEvolutionRecipe recipe,
            WarehouseLayoutData layout,
            WarehouseGridCache grid,
            WarehouseFitnessReport report)
        {
            int totalCells = layout.Width * layout.Height;
            int structureCells = CountOccupied(grid.Structure);
            int coverCells = CountCovers(layout);
            int targetStructureCells = Mathf.RoundToInt(totalCells * recipe.TargetStructureRatio);
            int minStructureCells = Mathf.RoundToInt(totalCells * recipe.MinStructureRatio);
            int maxStructureCells = Mathf.RoundToInt(totalCells * recipe.MaxStructureRatio);
            int targetCoverCells = Mathf.RoundToInt(totalCells * recipe.TargetCoverRatio);

            report.StructureCells = structureCells;
            report.TargetStructureCells = targetStructureCells;
            report.MinStructureCells = minStructureCells;
            report.MaxStructureCells = maxStructureCells;

            if (structureCells < minStructureCells)
                AddPenalty(report, (minStructureCells - structureCells) * recipe.StructureDensityPenaltyWeight, "Too few structure cells.");
            else if (structureCells > maxStructureCells)
                AddPenalty(report, (structureCells - maxStructureCells) * recipe.StructureDensityPenaltyWeight, "Too many structure cells.");

            AddPenalty(report, Mathf.Abs(coverCells - targetCoverCells) * 8f, "Cover count differs from target.");
        }

        private static void ScorePaths(
            WarehouseEvolutionRecipe recipe,
            WarehouseLayoutData layout,
            WarehouseNavigationGrid navigation,
            WarehouseFitnessReport report)
        {
            int pathA = navigation.FindPathCost(layout.SpawnA, layout.CapturePoint);
            int pathB = navigation.FindPathCost(layout.SpawnB, layout.CapturePoint);
            int groundA = navigation.FindGroundPathCost(layout.SpawnA, layout.CapturePoint);
            int groundB = navigation.FindGroundPathCost(layout.SpawnB, layout.CapturePoint);
            report.PathCostA = pathA;
            report.PathCostB = pathB;

            if (pathA < 0)
                AddPenalty(report, NoPathPenalty, "No path from Spawn A.");
            if (pathB < 0)
                AddPenalty(report, NoPathPenalty, "No path from Spawn B.");
            if (groundA < 0)
                AddPenalty(report, NoGroundPathPenalty, "No ground path from Spawn A.");
            if (groundB < 0)
                AddPenalty(report, NoGroundPathPenalty, "No ground path from Spawn B.");

            ScoreWeightedPathCost(recipe, pathA, report, "Spawn A weighted path is too expensive.");
            ScoreWeightedPathCost(recipe, pathB, report, "Spawn B weighted path is too expensive.");

            if (pathA >= 0 && pathB >= 0)
            {
                float diff = Mathf.Abs(pathA - pathB);
                if (diff > recipe.AllowedPathCostDifference)
                    AddPenalty(report, diff * 12f, "Path cost difference is too high.");
            }

            if (navigation.HasLineOfSight(layout.SpawnA, layout.CapturePoint))
                AddPenalty(report, 250f, "Spawn A has direct capture line of sight.");
            if (navigation.HasLineOfSight(layout.SpawnB, layout.CapturePoint))
                AddPenalty(report, 250f, "Spawn B has direct capture line of sight.");
        }

        private static void ScoreWeightedPathCost(
            WarehouseEvolutionRecipe recipe,
            int cost,
            WarehouseFitnessReport report,
            string violation)
        {
            if (cost < 0 || cost <= recipe.MaxAllowedWeightedPathCost)
                return;

            AddPenalty(report, (cost - recipe.MaxAllowedWeightedPathCost) * recipe.PathCostPenaltyWeight, violation);
        }

        private static void ScoreTopAccess(
            WarehouseEvolutionRecipe recipe,
            WarehouseLayoutData layout,
            WarehouseGridCache grid,
            WarehouseFitnessReport report)
        {
            List<List<Vector2Int>> components = BuildTopComponents(layout, grid);
            int inaccessible = 0;
            for (int i = 0; i < components.Count; i++)
            {
                if (components[i].Count >= recipe.MinTopComponentSize && !HasLadderAccess(layout, components[i]))
                    inaccessible++;
            }

            if (inaccessible > 0)
                AddPenalty(report, inaccessible * 350f, "Top components without ladder access.");
        }

        private static void ScoreBridgeQuality(
            WarehouseEvolutionRecipe recipe,
            WarehouseLayoutData layout,
            WarehouseGridCache grid,
            WarehouseFitnessReport report)
        {
            int sourceBridgeCount = 0;
            for (int i = 0; i < layout.Objects.Count; i++)
            {
                WarehouseObjectPlacement obj = layout.Objects[i];
                if (obj.Side == WarehouseSide.A && obj.Kind == WarehouseObjectKind.Bridge)
                    sourceBridgeCount++;

                if (obj.Kind == WarehouseObjectKind.Bridge && CountContainerSides(grid, obj.Origin) >= recipe.MaxBridgeBlockedSides)
                    AddPenalty(report, 600f, "Bridge is too enclosed.");
            }

            if (sourceBridgeCount > recipe.MaxBridgeCellCount)
                AddPenalty(report, (sourceBridgeCount - recipe.MaxBridgeCellCount) * 250f, "Too many bridge cells.");
        }

        private static void ScoreChokepoints(WarehouseLayoutData layout, WarehouseGridCache grid, WarehouseFitnessReport report)
        {
            int narrowCells = 0;
            for (int y = 1; y < layout.Height - 1; y++)
            {
                for (int x = 1; x < layout.Width - 1; x++)
                {
                    Vector2Int cell = new(x, y);
                    if (!IsGroundWalkable(grid, cell) ||
                        IsNearPoint(cell, layout.SpawnA, layout.SpawnClearRadius + 1f) ||
                        IsNearPoint(cell, layout.SpawnB, layout.SpawnClearRadius + 1f) ||
                        IsNearPoint(cell, layout.CapturePoint, layout.CaptureClearRadius + 1f))
                    {
                        continue;
                    }

                    int neighbours = CountGroundNeighbours(grid, cell);
                    bool horizontalOpen = IsGroundWalkable(grid, cell + Vector2Int.left) && IsGroundWalkable(grid, cell + Vector2Int.right);
                    bool verticalOpen = IsGroundWalkable(grid, cell + Vector2Int.down) && IsGroundWalkable(grid, cell + Vector2Int.up);
                    if (neighbours <= 2 && (horizontalOpen || verticalOpen))
                        narrowCells++;
                }
            }

            if (narrowCells > 12)
                AddPenalty(report, (narrowCells - 12) * 15f, "Too many narrow ground choke cells.");
        }

        private static void ScoreCoverQuality(WarehouseEvolutionRecipe recipe, WarehouseLayoutData layout, WarehouseFitnessReport report)
        {
            List<WarehouseObjectPlacement> covers = new();
            int captureCovers = 0;
            for (int i = 0; i < layout.Objects.Count; i++)
            {
                WarehouseObjectPlacement obj = layout.Objects[i];
                if (!obj.IsCover)
                    continue;

                covers.Add(obj);
                if (recipe.CaptureCoverRadiusValue > 0f &&
                    (obj.Center - layout.CapturePoint).sqrMagnitude <= recipe.CaptureCoverRadiusValue * recipe.CaptureCoverRadiusValue)
                {
                    captureCovers++;
                }
            }

            if (recipe.CaptureCoverRadiusValue > 0f && captureCovers < recipe.MinCaptureCovers)
                AddPenalty(report, (recipe.MinCaptureCovers - captureCovers) * recipe.CaptureCoverPenaltyWeight, "Too few covers near capture.");
            else if (recipe.CaptureCoverRadiusValue > 0f && captureCovers > recipe.MaxCaptureCovers)
                AddPenalty(report, (captureCovers - recipe.MaxCaptureCovers) * recipe.CaptureCoverPenaltyWeight, "Too many covers near capture.");

            float radius = recipe.CoverClusterRadiusValue;
            if (radius <= 0f)
                return;

            float radiusSqr = radius * radius;
            int excess = 0;
            for (int i = 0; i < covers.Count; i++)
            {
                int neighbours = 0;
                for (int j = 0; j < covers.Count; j++)
                {
                    if (i != j && (covers[i].Center - covers[j].Center).sqrMagnitude <= radiusSqr)
                        neighbours++;
                }

                excess += Mathf.Max(0, neighbours - recipe.MaxCoverNeighborCount);
            }

            if (excess > 0)
                AddPenalty(report, excess * recipe.CoverClusterPenaltyWeight, "Cover clusters are too dense.");
        }

        private static void Mutate(
            WarehouseGenome genome,
            WarehouseEvolutionRecipe recipe,
            System.Random random,
            bool aggressive)
        {
            int maxSteps = aggressive ? recipe.AggressiveMutationStepCount : recipe.MaxMutationStepCount;
            int steps = random.Next(recipe.MinMutationStepCount, maxSteps + 1);
            for (int i = 0; i < steps; i++)
            {
                double roll = random.NextDouble();
                if (roll < 0.25)
                    AddStructureMutation(genome, random);
                else if (roll < 0.42)
                    RemoveStructureMutation(genome, random);
                else if (roll < 0.65)
                    ChangeStructureMutation(genome, random);
                else if (roll < 0.80)
                    AddCoverMutation(genome, recipe, random);
                else if (roll < 0.92)
                    RemoveCoverMutation(genome, random);
                else
                    MutateMacro(genome, random);
            }
        }

        private static void AddStructureMutation(WarehouseGenome genome, System.Random random)
        {
            WarehouseObjectKind kind = ChooseStructureKind(random);
            Vector2Int cell = RandomSourceCell(genome, random);
            WarehouseDirection direction = RandomDirection(random);
            bool bridgeHorizontal = direction == WarehouseDirection.East || direction == WarehouseDirection.West;
            genome.Objects.Add(new WarehouseObjectPlacement
            {
                Kind = kind,
                Side = WarehouseSide.A,
                Origin = cell,
                Size = Vector2Int.one,
                Direction = direction,
                ConnectionMask = kind == WarehouseObjectKind.Bridge
                    ? GetStraightBridgeMask(bridgeHorizontal)
                    : WarehouseDirectionMask.None,
                BridgeConnectionType = WarehouseBridgeConnectionType.Straight,
                RotationY = kind == WarehouseObjectKind.Ladder
                    ? DirectionToRotation(direction)
                    : 0f
            });
        }

        private static void RemoveStructureMutation(WarehouseGenome genome, System.Random random)
        {
            List<int> indices = new();
            for (int i = 0; i < genome.Objects.Count; i++)
            {
                if (genome.Objects[i].IsStructure || genome.Objects[i].IsLadder)
                    indices.Add(i);
            }

            if (indices.Count > 0)
                genome.Objects.RemoveAt(indices[random.Next(indices.Count)]);
        }

        private static void ChangeStructureMutation(WarehouseGenome genome, System.Random random)
        {
            List<WarehouseObjectPlacement> candidates = new();
            for (int i = 0; i < genome.Objects.Count; i++)
            {
                if (genome.Objects[i].IsStructure || genome.Objects[i].IsLadder)
                    candidates.Add(genome.Objects[i]);
            }

            if (candidates.Count == 0)
                return;

            WarehouseObjectPlacement obj = candidates[random.Next(candidates.Count)];
            double roll = random.NextDouble();
            if (roll < 0.45)
                obj.Kind = ChooseStructureKind(random);
            else if (roll < 0.75)
                obj.Origin += new Vector2Int(random.Next(-2, 3), random.Next(-2, 3));

            obj.Direction = RandomDirection(random);
            if (obj.Kind == WarehouseObjectKind.Bridge)
                obj.ConnectionMask = GetStraightBridgeMask(obj.Direction == WarehouseDirection.East || obj.Direction == WarehouseDirection.West);
            else
                obj.ConnectionMask = WarehouseDirectionMask.None;

            obj.BridgeConnectionType = WarehouseBridgeConnectionType.Straight;
            obj.RotationY = obj.Kind == WarehouseObjectKind.Ladder
                ? DirectionToRotation(obj.Direction)
                : 0f;
            obj.Surface = WarehousePlacementSurface.Ground;
        }

        private static void AddCoverMutation(
            WarehouseGenome genome,
            WarehouseEvolutionRecipe recipe,
            System.Random random)
        {
            WarehouseObjectKind kind = random.NextDouble() < recipe.FullCoverProbability
                ? WarehouseObjectKind.FullCover
                : WarehouseObjectKind.PartialCover;
            float rotationY = random.Next(0, 4) * 90f;
            genome.Objects.Add(new WarehouseObjectPlacement
            {
                Kind = kind,
                Side = WarehouseSide.A,
                Origin = RandomSourceCell(genome, random),
                Size = Vector2Int.one,
                Surface = random.NextDouble() < recipe.TopCoverProbability
                    ? WarehousePlacementSurface.StructureTop
                    : WarehousePlacementSurface.Ground,
                Direction = RotationToDirection(rotationY),
                RotationY = rotationY,
                VariantIndex = HasNewCoverVariants(recipe) ? -1 : ChooseCoverVariantIndex(recipe, random, kind)
            });
        }

        private static void RemoveCoverMutation(WarehouseGenome genome, System.Random random)
        {
            List<int> indices = new();
            for (int i = 0; i < genome.Objects.Count; i++)
            {
                if (genome.Objects[i].IsCover)
                    indices.Add(i);
            }

            if (indices.Count > 0)
                genome.Objects.RemoveAt(indices[random.Next(indices.Count)]);
        }

        private static void MutateMacro(WarehouseGenome genome, System.Random random)
        {
            int centerX = Mathf.Clamp(Mathf.FloorToInt(genome.SpawnA.x), 1, genome.Width - 2);
            genome.CapturePoint = new Vector2(4f + (float)random.NextDouble() * 7f, genome.CapturePoint.y);
            genome.LeftLaneX = Mathf.Clamp(centerX - random.Next(3, 6), 1, genome.Width - 2);
            genome.RightLaneX = Mathf.Clamp(centerX + random.Next(3, 6), 1, genome.Width - 2);
            genome.LowerConnectorY = Mathf.Clamp(genome.LowerConnectorY + random.Next(-1, 2), 3, Mathf.Max(3, genome.Height / 2 - 3));
            genome.UpperConnectorY = Mathf.Clamp(genome.UpperConnectorY + random.Next(-1, 2), genome.LowerConnectorY + 1, Mathf.Max(genome.LowerConnectorY + 1, genome.Height / 2 - 1));
        }

        private static WarehouseGenome Crossover(WarehouseGenome a, WarehouseGenome b, System.Random random)
        {
            if (random.NextDouble() < 0.5)
                (a, b) = (b, a);

            WarehouseGenome child = CloneGenome(a);
            child.Objects.Clear();
            child.CapturePoint = b.CapturePoint;
            int centerStartY = Mathf.Max(1, child.Height / 2 - CenterBandDepth);
            for (int i = 0; i < a.Objects.Count; i++)
            {
                if (a.Objects[i].Center.y < centerStartY)
                    child.Objects.Add(ClonePlacement(a.Objects[i]));
            }

            for (int i = 0; i < b.Objects.Count; i++)
            {
                if (b.Objects[i].Center.y >= centerStartY)
                    child.Objects.Add(ClonePlacement(b.Objects[i]));
            }

            return child;
        }

        private static WarehouseGenome TournamentSelect(
            List<WarehouseGenome> population,
            WarehouseEvolutionRecipe recipe,
            System.Random random)
        {
            WarehouseGenome best = population[random.Next(population.Count)];
            for (int i = 1; i < recipe.TournamentPickCount; i++)
            {
                WarehouseGenome candidate = population[random.Next(population.Count)];
                if (candidate.Score < best.Score)
                    best = candidate;
            }

            return best;
        }

        private static void RepairGenome(WarehouseEvolutionRecipe recipe, WarehouseGenome genome)
        {
            ApplyBasicRepair(genome);
            TrimBridgeCount(recipe, genome);
            for (int i = 0; i < 4; i++)
            {
                SourceGridCache grid = new(genome);
                int removed = 0;
                for (int j = genome.Objects.Count - 1; j >= 0; j--)
                {
                    WarehouseObjectPlacement obj = genome.Objects[j];
                    if ((obj.Kind == WarehouseObjectKind.Bridge && !IsBridgeValid(recipe, genome, grid, obj)) ||
                        (obj.IsLadder && !IsLadderValid(genome, grid, obj)) ||
                        (obj.IsCover && !IsCoverValid(grid, obj)))
                    {
                        genome.Objects.RemoveAt(j);
                        removed++;
                    }
                }

                if (removed == 0)
                    break;
            }
        }

        private static void ApplyBasicRepair(WarehouseGenome genome)
        {
            bool[,] structure = new bool[genome.Width, genome.Height / 2];
            bool[,] ladder = new bool[genome.Width, genome.Height / 2];
            bool[,] groundCover = new bool[genome.Width, genome.Height / 2];
            bool[,] topCover = new bool[genome.Width, genome.Height / 2];
            List<WarehouseObjectPlacement> accepted = new(genome.Objects.Count);

            for (int i = 0; i < genome.Objects.Count; i++)
            {
                WarehouseObjectPlacement obj = NormalizePlacement(genome, genome.Objects[i]);
                if (!IsInsideSource(genome, obj))
                    continue;

                if ((obj.IsStructure || obj.IsCover) &&
                    WarehouseRepairPass.ObjectTouchesRadius(obj, genome.SpawnA, genome.SpawnClearRadius))
                {
                    continue;
                }

                if (obj.IsStructure &&
                    WarehouseRepairPass.ObjectTouchesRadius(obj, genome.CapturePoint, genome.CaptureClearRadius))
                {
                    continue;
                }

                if (obj.IsLadder && ladder[obj.Origin.x, obj.Origin.y])
                    continue;

                if (obj.IsStructure)
                {
                    if (structure[obj.Origin.x, obj.Origin.y])
                        continue;

                    structure[obj.Origin.x, obj.Origin.y] = true;
                }
                else if (obj.IsLadder)
                {
                    ladder[obj.Origin.x, obj.Origin.y] = true;
                }
                else if (obj.IsCover)
                {
                    bool[,] coverGrid = obj.Surface == WarehousePlacementSurface.StructureTop ? topCover : groundCover;
                    if (coverGrid[obj.Origin.x, obj.Origin.y])
                        continue;

                    coverGrid[obj.Origin.x, obj.Origin.y] = true;
                }

                accepted.Add(obj);
            }

            genome.Objects.Clear();
            genome.Objects.AddRange(accepted);
        }

        private static void TrimBridgeCount(WarehouseEvolutionRecipe recipe, WarehouseGenome genome)
        {
            int count = 0;
            for (int i = 0; i < genome.Objects.Count; i++)
            {
                if (genome.Objects[i].Kind != WarehouseObjectKind.Bridge)
                    continue;

                count++;
                if (count > recipe.MaxBridgeCellCount)
                    genome.Objects.RemoveAt(i--);
            }
        }

        private static bool IsBridgeValid(
            WarehouseEvolutionRecipe recipe,
            WarehouseGenome genome,
            SourceGridCache grid,
            WarehouseObjectPlacement bridge)
        {
            if (CountContainerSides(grid, bridge.Origin) >= recipe.MaxBridgeBlockedSides)
                return false;

            bool horizontal = IsBridgeHorizontal(bridge);
            Vector2Int axis = horizontal ? Vector2Int.right : Vector2Int.up;
            Vector2Int start = bridge.Origin;
            Vector2Int end = bridge.Origin;
            while (IsBridgeAt(grid, start - axis, horizontal))
                start -= axis;
            while (IsBridgeAt(grid, end + axis, horizontal))
                end += axis;

            int length = horizontal ? end.x - start.x + 1 : end.y - start.y + 1;
            if (length < 1 || length > 2)
                return false;

            return IsSourceCell(genome, start - axis) &&
                   IsSourceCell(genome, end + axis) &&
                   grid.ContainerLow[start.x - axis.x, start.y - axis.y] &&
                   grid.ContainerLow[end.x + axis.x, end.y + axis.y];
        }

        private static bool IsLadderValid(
            WarehouseGenome genome,
            SourceGridCache grid,
            WarehouseObjectPlacement ladder)
        {
            Vector2Int topCell = ladder.Origin + DirectionToVector(ladder.Direction);
            if (!IsSourceCell(genome, topCell) ||
                !grid.ContainerLow[topCell.x, topCell.y] ||
                grid.Structure[ladder.Origin.x, ladder.Origin.y])
            {
                return false;
            }

            Vector2Int approach = ladder.Origin - DirectionToVector(ladder.Direction);
            return IsSourceCell(genome, approach) &&
                   !grid.GroundBlocked[approach.x, approach.y] &&
                   !grid.Ladder[approach.x, approach.y];
        }

        private static bool IsCoverValid(SourceGridCache grid, WarehouseObjectPlacement cover)
        {
            return cover.Surface == WarehousePlacementSurface.StructureTop
                ? grid.TopWalkable[cover.Origin.x, cover.Origin.y] && !grid.Ladder[cover.Origin.x, cover.Origin.y]
                : !grid.Structure[cover.Origin.x, cover.Origin.y] && !grid.Ladder[cover.Origin.x, cover.Origin.y];
        }

        private static WarehouseLayoutData BuildFinalLayout(WarehouseGenome genome)
        {
            WarehouseLayoutData layout = CreateLayoutMeta(genome);
            for (int i = 0; i < genome.Objects.Count; i++)
                layout.Objects.Add(ClonePlacement(genome.Objects[i]));

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

            return layout;
        }

        private static WarehouseLayoutData CreateLayoutMeta(WarehouseGenome genome)
        {
            return new WarehouseLayoutData
            {
                Width = genome.Width,
                Height = genome.Height,
                CellSize = genome.CellSize,
                CellSizeX = genome.CellSizeX,
                CellSizeZ = genome.CellSizeZ,
                SpawnA = genome.SpawnA,
                SpawnB = genome.SpawnB,
                CapturePoint = genome.CapturePoint,
                SpawnClearRadius = genome.SpawnClearRadius,
                CaptureClearRadius = genome.CaptureClearRadius,
                LeftLaneX = genome.LeftLaneX,
                RightLaneX = genome.RightLaneX,
                LowerConnectorY = genome.LowerConnectorY,
                UpperConnectorY = genome.UpperConnectorY
            };
        }

        private static WarehouseGenome CloneGenome(WarehouseGenome source)
        {
            WarehouseGenome clone = new()
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

            for (int i = 0; i < source.Objects.Count; i++)
                clone.Objects.Add(ClonePlacement(source.Objects[i]));

            return clone;
        }

        private static WarehouseObjectPlacement ClonePlacement(WarehouseObjectPlacement source)
        {
            return new WarehouseObjectPlacement
            {
                Kind = source.Kind,
                Side = source.Side,
                Origin = source.Origin,
                Size = source.Size,
                Surface = source.Surface,
                Direction = source.Direction,
                ConnectionMask = source.ConnectionMask,
                BridgeConnectionType = source.BridgeConnectionType,
                RotationY = source.RotationY,
                VariantIndex = source.VariantIndex
            };
        }

        private static WarehouseObjectPlacement NormalizePlacement(WarehouseGenome genome, WarehouseObjectPlacement source)
        {
            WarehouseObjectPlacement obj = ClonePlacement(source);
            obj.Side = WarehouseSide.A;
            obj.Size = Vector2Int.one;
            obj.Origin = new Vector2Int(
                Mathf.Clamp(obj.Origin.x, 0, genome.Width - 1),
                Mathf.Clamp(obj.Origin.y, 0, genome.Height / 2 - 1));
            obj.RotationY = Mathf.Repeat(obj.RotationY, 360f);
            return obj;
        }

        private static int ComputeHash(WarehouseGenome genome)
        {
            List<WarehouseObjectPlacement> sorted = new(genome.Objects);
            sorted.Sort(ComparePlacements);
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + Mathf.RoundToInt(genome.CapturePoint.x * 100f);
                hash = hash * 31 + genome.LeftLaneX;
                hash = hash * 31 + genome.RightLaneX;
                hash = hash * 31 + genome.LowerConnectorY;
                hash = hash * 31 + genome.UpperConnectorY;
                for (int i = 0; i < sorted.Count; i++)
                {
                    WarehouseObjectPlacement obj = sorted[i];
                    hash = hash * 31 + (int)obj.Kind;
                    hash = hash * 31 + obj.Origin.x;
                    hash = hash * 31 + obj.Origin.y;
                    hash = hash * 31 + (int)obj.Surface;
                    hash = hash * 31 + (int)obj.Direction;
                    hash = hash * 31 + Mathf.RoundToInt(obj.RotationY);
                    hash = hash * 31 + obj.VariantIndex;
                }

                return hash;
            }
        }

        private static int ComparePlacements(WarehouseObjectPlacement a, WarehouseObjectPlacement b)
        {
            int result = a.Origin.y.CompareTo(b.Origin.y);
            if (result != 0)
                return result;

            result = a.Origin.x.CompareTo(b.Origin.x);
            return result != 0 ? result : a.Kind.CompareTo(b.Kind);
        }

        private static int CompareGenomes(WarehouseGenome a, WarehouseGenome b)
        {
            return a.Score.CompareTo(b.Score);
        }

        private static WarehouseFitnessReport CloneReport(WarehouseFitnessReport source)
        {
            WarehouseFitnessReport report = new()
            {
                PenaltyScore = source.PenaltyScore,
                StructureCells = source.StructureCells,
                TargetStructureCells = source.TargetStructureCells,
                MinStructureCells = source.MinStructureCells,
                MaxStructureCells = source.MaxStructureCells,
                PathCostA = source.PathCostA,
                PathCostB = source.PathCostB
            };

            for (int i = 0; i < source.Violations.Count; i++)
                report.Violations.Add(source.Violations[i]);

            return report;
        }

        private static int CountCovers(WarehouseLayoutData layout)
        {
            int count = 0;
            for (int i = 0; i < layout.Objects.Count; i++)
            {
                if (layout.Objects[i].IsCover)
                    count++;
            }

            return count;
        }

        private static int CountOccupied(bool[,] cells)
        {
            int count = 0;
            for (int y = 0; y < cells.GetLength(1); y++)
            {
                for (int x = 0; x < cells.GetLength(0); x++)
                {
                    if (cells[x, y])
                        count++;
                }
            }

            return count;
        }

        private static List<List<Vector2Int>> BuildTopComponents(WarehouseLayoutData layout, WarehouseGridCache grid)
        {
            bool[,] visited = new bool[layout.Width, layout.Height];
            List<List<Vector2Int>> components = new();
            Queue<Vector2Int> queue = new();
            for (int y = 0; y < layout.Height; y++)
            {
                for (int x = 0; x < layout.Width; x++)
                {
                    if (!grid.TopWalkable[x, y] || visited[x, y])
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
                            if (!layout.IsInside(next) || !grid.TopWalkable[next.x, next.y] || visited[next.x, next.y])
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
                if (obj.IsLadder && cells.Contains(obj.Origin + DirectionToVector(obj.Direction)))
                    return true;
            }

            return false;
        }

        private static bool IsGroundWalkable(WarehouseGridCache grid, Vector2Int cell)
        {
            return cell.x >= 0 &&
                   cell.x < grid.Width &&
                   cell.y >= 0 &&
                   cell.y < grid.Height &&
                   !grid.GroundBlocked[cell.x, cell.y] &&
                   !grid.Ladder[cell.x, cell.y];
        }

        private static int CountGroundNeighbours(WarehouseGridCache grid, Vector2Int cell)
        {
            int count = 0;
            for (int i = 0; i < WarehouseGenerationUtility.CardinalDirections.Length; i++)
            {
                if (IsGroundWalkable(grid, cell + WarehouseGenerationUtility.CardinalDirections[i]))
                    count++;
            }

            return count;
        }

        private static bool IsNearPoint(Vector2Int cell, Vector2 point, float radius)
        {
            return (new Vector2(cell.x + 0.5f, cell.y + 0.5f) - point).sqrMagnitude <= radius * radius;
        }

        private static int CountContainerSides(WarehouseGridCache grid, Vector2Int cell)
        {
            int count = 0;
            for (int i = 0; i < WarehouseGenerationUtility.CardinalDirections.Length; i++)
            {
                Vector2Int next = cell + WarehouseGenerationUtility.CardinalDirections[i];
                if (next.x >= 0 &&
                    next.x < grid.Width &&
                    next.y >= 0 &&
                    next.y < grid.Height &&
                    grid.Container[next.x, next.y])
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountContainerSides(SourceGridCache grid, Vector2Int cell)
        {
            int count = 0;
            for (int i = 0; i < WarehouseGenerationUtility.CardinalDirections.Length; i++)
            {
                Vector2Int next = cell + WarehouseGenerationUtility.CardinalDirections[i];
                if (next.x >= 0 &&
                    next.x < grid.Width &&
                    next.y >= 0 &&
                    next.y < grid.Height &&
                    grid.Container[next.x, next.y])
                {
                    count++;
                }
            }

            return count;
        }

        private static bool IsBridgeAt(SourceGridCache grid, Vector2Int cell, bool horizontal)
        {
            return cell.x >= 0 &&
                   cell.x < grid.Width &&
                   cell.y >= 0 &&
                   cell.y < grid.Height &&
                   (horizontal ? grid.BridgeHorizontal[cell.x, cell.y] : grid.BridgeVertical[cell.x, cell.y]);
        }

        private static bool IsInsideSource(WarehouseGenome genome, WarehouseObjectPlacement obj)
        {
            return obj.Origin.x >= 0 &&
                   obj.Origin.x + obj.Size.x <= genome.Width &&
                   obj.Origin.y >= 0 &&
                   obj.Origin.y + obj.Size.y <= genome.Height / 2;
        }

        private static bool IsSourceCell(WarehouseGenome genome, Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < genome.Width && cell.y >= 0 && cell.y < genome.Height / 2;
        }

        private static Vector2Int RandomSourceCell(WarehouseGenome genome, System.Random random)
        {
            return new Vector2Int(random.Next(0, genome.Width), random.Next(0, genome.Height / 2));
        }

        private static WarehouseObjectKind ChooseStructureKind(System.Random random)
        {
            double roll = random.NextDouble();
            if (roll < 0.55)
                return WarehouseObjectKind.ContainerLow;
            if (roll < 0.68)
                return WarehouseObjectKind.ContainerHigh;
            if (roll < 0.85)
                return WarehouseObjectKind.Bridge;

            return WarehouseObjectKind.Ladder;
        }

        private static WarehouseDirection RandomDirection(System.Random random)
        {
            return (WarehouseDirection)random.Next(0, 4);
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

        private static bool HasNewCoverVariants(WarehouseRecipe recipe)
        {
            return recipe.CoverVariants != null && recipe.CoverVariants.Length > 0;
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

        private static WarehouseDirection VectorToDirection(Vector2Int direction)
        {
            if (direction == Vector2Int.up)
                return WarehouseDirection.North;
            if (direction == Vector2Int.down)
                return WarehouseDirection.South;
            if (direction == Vector2Int.right)
                return WarehouseDirection.East;
            if (direction == Vector2Int.left)
                return WarehouseDirection.West;

            return WarehouseDirection.North;
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

        private static WarehouseDirectionMask GetStraightBridgeMask(bool horizontal)
        {
            return horizontal
                ? WarehouseDirectionMask.East | WarehouseDirectionMask.West
                : WarehouseDirectionMask.North | WarehouseDirectionMask.South;
        }

        private readonly struct BridgeCandidate
        {
            public readonly Vector2Int Cell;
            public readonly bool Horizontal;

            public BridgeCandidate(Vector2Int cell, bool horizontal)
            {
                Cell = cell;
                Horizontal = horizontal;
            }
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

        private static bool IsBridgeHorizontal(WarehouseObjectPlacement obj)
        {
            if (obj.ConnectionMask != WarehouseDirectionMask.None)
                return (obj.ConnectionMask & (WarehouseDirectionMask.East | WarehouseDirectionMask.West)) ==
                       (WarehouseDirectionMask.East | WarehouseDirectionMask.West);

            return Mathf.Abs(Mathf.DeltaAngle(obj.RotationY, 0f)) <= 45f ||
                   Mathf.Abs(Mathf.DeltaAngle(obj.RotationY, 180f)) <= 45f;
        }

        private static void AddPenalty(WarehouseFitnessReport report, float penalty, string violation)
        {
            report.PenaltyScore += penalty;
            report.Violations.Add(violation);
        }

        private sealed class WarehouseGenome
        {
            public int Width;
            public int Height;
            public float CellSize;
            public float CellSizeX;
            public float CellSizeZ;
            public Vector2 SpawnA;
            public Vector2 SpawnB;
            public Vector2 CapturePoint;
            public float SpawnClearRadius;
            public float CaptureClearRadius;
            public int LeftLaneX;
            public int RightLaneX;
            public int LowerConnectorY;
            public int UpperConnectorY;
            public float Score;
            public int Hash;
            public WarehouseFitnessReport Fitness;
            public readonly List<WarehouseObjectPlacement> Objects = new();
        }

        private readonly struct FitnessResult
        {
            public readonly int Hash;
            public readonly float Score;
            public readonly WarehouseFitnessReport Report;

            public FitnessResult(int hash, float score, WarehouseFitnessReport report)
            {
                Hash = hash;
                Score = score;
                Report = report;
            }
        }

        private readonly struct CachedFitness
        {
            public readonly float Score;
            public readonly WarehouseFitnessReport Report;

            public CachedFitness(float score, WarehouseFitnessReport report)
            {
                Score = score;
                Report = report;
            }
        }

        private sealed class SourceGridCache
        {
            public readonly int Width;
            public readonly int Height;
            public readonly bool[,] Structure;
            public readonly bool[,] GroundBlocked;
            public readonly bool[,] TopWalkable;
            public readonly bool[,] Container;
            public readonly bool[,] ContainerLow;
            public readonly bool[,] Ladder;
            public readonly bool[,] BridgeHorizontal;
            public readonly bool[,] BridgeVertical;

            public SourceGridCache(WarehouseGenome genome)
            {
                Width = genome.Width;
                Height = genome.Height / 2;
                Structure = new bool[Width, Height];
                GroundBlocked = new bool[Width, Height];
                TopWalkable = new bool[Width, Height];
                Container = new bool[Width, Height];
                ContainerLow = new bool[Width, Height];
                Ladder = new bool[Width, Height];
                BridgeHorizontal = new bool[Width, Height];
                BridgeVertical = new bool[Width, Height];

                for (int i = 0; i < genome.Objects.Count; i++)
                {
                    WarehouseObjectPlacement obj = genome.Objects[i];
                    if (!IsSourceCell(genome, obj.Origin))
                        continue;

                    if (obj.IsStructure)
                    {
                        Structure[obj.Origin.x, obj.Origin.y] = true;
                        if (obj.IsContainer)
                            Container[obj.Origin.x, obj.Origin.y] = true;
                        if (obj.IsGroundBlocker)
                            GroundBlocked[obj.Origin.x, obj.Origin.y] = true;
                        if (obj.Kind == WarehouseObjectKind.ContainerLow)
                            ContainerLow[obj.Origin.x, obj.Origin.y] = true;
                        if (obj.IsTopWalkableSource)
                            TopWalkable[obj.Origin.x, obj.Origin.y] = true;
                        if (obj.Kind == WarehouseObjectKind.Bridge)
                        {
                            if (IsBridgeHorizontal(obj))
                                BridgeHorizontal[obj.Origin.x, obj.Origin.y] = true;
                            else
                                BridgeVertical[obj.Origin.x, obj.Origin.y] = true;
                        }
                    }
                    else if (obj.IsLadder)
                    {
                        Ladder[obj.Origin.x, obj.Origin.y] = true;
                    }
                }
            }
        }

        private sealed class WarehouseGridCache
        {
            public readonly int Width;
            public readonly int Height;
            public readonly bool[,] Structure;
            public readonly bool[,] GroundBlocked;
            public readonly bool[,] TopWalkable;
            public readonly bool[,] Container;
            public readonly bool[,] Ladder;

            public WarehouseGridCache(WarehouseLayoutData layout)
            {
                Width = layout.Width;
                Height = layout.Height;
                Structure = new bool[Width, Height];
                GroundBlocked = new bool[Width, Height];
                TopWalkable = new bool[Width, Height];
                Container = new bool[Width, Height];
                Ladder = new bool[Width, Height];

                for (int i = 0; i < layout.Objects.Count; i++)
                {
                    WarehouseObjectPlacement obj = layout.Objects[i];
                    if (!layout.IsInside(obj.Origin))
                        continue;

                    if (obj.IsStructure)
                    {
                        Structure[obj.Origin.x, obj.Origin.y] = true;
                        if (obj.IsContainer)
                            Container[obj.Origin.x, obj.Origin.y] = true;
                        if (obj.IsGroundBlocker)
                            GroundBlocked[obj.Origin.x, obj.Origin.y] = true;
                        if (obj.IsTopWalkableSource)
                            TopWalkable[obj.Origin.x, obj.Origin.y] = true;
                    }
                    else if (obj.IsLadder)
                    {
                        Ladder[obj.Origin.x, obj.Origin.y] = true;
                    }
                }
            }
        }
    }
}
