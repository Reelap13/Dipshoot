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
            List<WarehouseGenome> baselines = BuildBaselineGenomes(recipe, context.Request.Seed);
            List<WarehouseGenome> population = BuildInitialPopulation(recipe, baselines, random);
            Dictionary<int, CachedFitness> fitnessCache = new();

            EvaluatePopulation(recipe, population, fitnessCache);
            for (int generation = 0; generation < recipe.GenerationCount; generation++)
            {
                population.Sort(CompareGenomes);
                List<WarehouseGenome> next = new(recipe.PopulationCount);
                for (int i = 0; i < recipe.EliteKeepCount; i++)
                    next.Add(CloneGenome(population[i]));

                while (next.Count < recipe.PopulationCount)
                {
                    WarehouseGenome parentA = TournamentSelect(population, recipe, random);
                    WarehouseGenome parentB = TournamentSelect(population, recipe, random);
                    WarehouseGenome child = Crossover(parentA, parentB, random);
                    bool aggressive = generation < recipe.GenerationCount / 3;
                    if (random.NextDouble() < recipe.MutationProbability || next.Count >= recipe.EliteKeepCount)
                        Mutate(child, recipe, random, aggressive);

                    next.Add(child);
                }

                population = next;
                EvaluatePopulation(recipe, population, fitnessCache);
            }

            population.Sort(CompareGenomes);
            WarehouseGenome best = population[0];
            WarehouseLayoutData layout = BuildFinalLayout(best);
            WarehouseFitnessReport report = best.Fitness ?? EvaluateGenome(recipe, best, fitnessCache).Report;
            WarehouseNavigationGrid navigation = new(layout);

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

        private static List<WarehouseGenome> BuildBaselineGenomes(WarehouseEvolutionRecipe recipe, int seed)
        {
            WarehouseRecipe baseRecipe = CreateBaseRecipe(recipe);
            GenerationPipeline pipeline = new();
            List<WarehouseGenome> baselines = new(recipe.BaselineCount);
            try
            {
                for (int i = 0; i < recipe.BaselineCount; i++)
                {
                    GenerationRequest request = new(unchecked(seed + i * 92821), true);
                    GenerationResult result = pipeline.Generate(request, baseRecipe);
                    baselines.Add(CreateGenomeFromLayout(result.GetRequired(WarehouseKeys.Layout)));
                }
            }
            finally
            {
                if (Application.isPlaying)
                    Object.Destroy(baseRecipe);
                else
                    Object.DestroyImmediate(baseRecipe);
            }

            return baselines;
        }

        private static WarehouseRecipe CreateBaseRecipe(WarehouseEvolutionRecipe source)
        {
            WarehouseRecipe recipe = ScriptableObject.CreateInstance<WarehouseRecipe>();
            recipe.name = "RuntimeWarehouseBaselineRecipe";
            recipe.MapWidth = source.MapWidth;
            recipe.MapHeight = source.MapHeight;
            recipe.CellSize = source.CellSize;
            recipe.SpawnAPosition = source.SpawnAPosition;
            recipe.SpawnBPosition = source.SpawnBPosition;
            recipe.CapturePointPosition = source.CapturePointPosition;
            recipe.SpawnClearRadius = source.SpawnClearRadius;
            recipe.CaptureClearRadius = source.CaptureClearRadius;
            recipe.SymmetryMode = source.SymmetryMode;
            recipe.InitialStructureCellRatio = source.InitialStructureCellRatio;
            recipe.TargetStructureCellRatio = source.TargetStructureCellRatio;
            recipe.MinStructureCellRatio = source.MinStructureCellRatio;
            recipe.MaxStructureCellRatio = source.MaxStructureCellRatio;
            recipe.TargetCoverCellRatio = source.TargetCoverCellRatio;
            recipe.StructurePlacementAttempts = source.StructurePlacementAttempts;
            recipe.CoverPlacementAttempts = source.CoverPlacementAttempts;
            recipe.HighContainerChance = source.HighContainerChance;
            recipe.BridgeChance = source.BridgeChance;
            recipe.BridgePlacementAttempts = source.BridgePlacementAttempts;
            recipe.MaxBridgeCellsPerHalf = source.MaxBridgeCellsPerHalf;
            recipe.MaxBridgePairCountPerHalf = source.MaxBridgePairCountPerHalf;
            recipe.MaxBridgeBlockedContainerSides = source.MaxBridgeBlockedContainerSides;
            recipe.TopCoverChance = source.TopCoverChance;
            recipe.FullCoverChance = source.FullCoverChance;
            recipe.StructureCellsPerLadder = source.StructureCellsPerLadder;
            recipe.ExtraCoverPairs = source.ExtraCoverPairs;
            recipe.StructureDensityPenalty = source.StructureDensityPenalty;
            recipe.MinTopComponentSizeForLadder = source.MinTopComponentSizeForLadder;
            recipe.CoverClusterRadius = source.CoverClusterRadius;
            recipe.MaxCoverNeighbors = source.MaxCoverNeighbors;
            recipe.CoverClusterPenalty = source.CoverClusterPenalty;
            recipe.MaxPathCostDifference = source.MaxPathCostDifference;
            recipe.ContainerLowHeight = source.ContainerLowHeight;
            recipe.ContainerHighHeight = source.ContainerHighHeight;
            recipe.BridgeHeight = source.BridgeHeight;
            recipe.ContainerLowPrefab = source.ContainerLowPrefab;
            recipe.ContainerHighPrefab = source.ContainerHighPrefab;
            recipe.BridgePrefab = source.BridgePrefab;
            recipe.LadderPrefab = source.LadderPrefab;
            recipe.PartialCoverPrefab = source.PartialCoverPrefab;
            recipe.FullCoverPrefab = source.FullCoverPrefab;
            recipe.FloorPrefab = source.FloorPrefab;
            recipe.WallPrefab = source.WallPrefab;
            recipe.SpawnMarkerPrefab = source.SpawnMarkerPrefab;
            recipe.CapturePointMarkerPrefab = source.CapturePointMarkerPrefab;
            recipe.PartialCoverVariants = source.PartialCoverVariants;
            recipe.FullCoverVariants = source.FullCoverVariants;
            return recipe;
        }

        private static List<WarehouseGenome> BuildInitialPopulation(
            WarehouseEvolutionRecipe recipe,
            List<WarehouseGenome> baselines,
            System.Random random)
        {
            List<WarehouseGenome> population = new(recipe.PopulationCount);
            for (int i = 0; i < recipe.PopulationCount; i++)
            {
                WarehouseGenome genome = CloneGenome(baselines[i % baselines.Count]);
                if (i > 0)
                {
                    bool aggressive = i > recipe.PopulationCount / 2;
                    Mutate(genome, recipe, random, aggressive);
                }

                population.Add(genome);
            }

            return population;
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
            WarehouseFitnessReport report = new();
            WarehouseGridCache grid = new(layout);
            ScoreClearZones(layout, report);
            ScoreDensity(recipe, layout, grid, report);

            if (report.PenaltyScore < NoPathPenalty)
            {
                WarehouseNavigationGrid navigation = new(layout);
                ScorePaths(recipe, layout, navigation, report);
                ScoreTopAccess(recipe, layout, grid, report);
                ScoreBridgeQuality(recipe, layout, grid, report);
                ScoreChokepoints(layout, grid, report);
                ScoreCoverQuality(recipe, layout, report);
            }

            fitnessCache[hash] = new CachedFitness(report.PenaltyScore, CloneReport(report));
            return new FitnessResult(hash, report.PenaltyScore, report);
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
                if ((obj.Center - layout.CapturePoint).sqrMagnitude <= Mathf.Pow(layout.CaptureClearRadius + 1f, 2f))
                    captureCovers++;
            }

            if (captureCovers > 8)
                AddPenalty(report, (captureCovers - 8) * 80f, "Too many covers near capture.");

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
            genome.Objects.Add(new WarehouseObjectPlacement
            {
                Kind = kind,
                Side = WarehouseSide.A,
                Origin = cell,
                Size = Vector2Int.one,
                Direction = direction,
                RotationY = kind == WarehouseObjectKind.Ladder
                    ? DirectionToRotation(direction)
                    : random.Next(0, 4) * 90f
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
            obj.RotationY = obj.Kind == WarehouseObjectKind.Ladder
                ? DirectionToRotation(obj.Direction)
                : random.Next(0, 4) * 90f;
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
            genome.Objects.Add(new WarehouseObjectPlacement
            {
                Kind = kind,
                Side = WarehouseSide.A,
                Origin = RandomSourceCell(genome, random),
                Size = Vector2Int.one,
                Surface = random.NextDouble() < recipe.TopCoverProbability
                    ? WarehousePlacementSurface.StructureTop
                    : WarehousePlacementSurface.Ground,
                RotationY = random.Next(0, 4) * 90f,
                VariantIndex = ChooseCoverVariantIndex(recipe, random, kind)
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
            WarehouseLayoutData meta = CreateLayoutMeta(genome);
            bool[,] reserved = WarehouseGenerationUtility.BuildReservedGroundMask(meta);
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

                if (obj.IsGroundBlocker && reserved[obj.Origin.x, obj.Origin.y])
                    continue;

                if (obj.IsLadder && (reserved[obj.Origin.x, obj.Origin.y] || ladder[obj.Origin.x, obj.Origin.y]))
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

            for (int i = 0; i < WarehouseGenerationUtility.CardinalDirections.Length; i++)
            {
                Vector2Int approach = ladder.Origin + WarehouseGenerationUtility.CardinalDirections[i];
                if (approach == topCell || !IsSourceCell(genome, approach))
                    continue;

                if (!grid.GroundBlocked[approach.x, approach.y] && !grid.Ladder[approach.x, approach.y])
                    return true;
            }

            return false;
        }

        private static bool IsCoverValid(SourceGridCache grid, WarehouseObjectPlacement cover)
        {
            return cover.Surface == WarehousePlacementSurface.StructureTop
                ? grid.TopWalkable[cover.Origin.x, cover.Origin.y] && !grid.Ladder[cover.Origin.x, cover.Origin.y]
                : !grid.Structure[cover.Origin.x, cover.Origin.y] && !grid.Ladder[cover.Origin.x, cover.Origin.y];
        }

        private static WarehouseGenome CreateGenomeFromLayout(WarehouseLayoutData layout)
        {
            WarehouseGenome genome = new()
            {
                Width = layout.Width,
                Height = layout.Height,
                CellSize = layout.CellSize,
                SpawnA = layout.SpawnA,
                SpawnB = layout.SpawnB,
                CapturePoint = layout.CapturePoint,
                SpawnClearRadius = layout.SpawnClearRadius,
                CaptureClearRadius = layout.CaptureClearRadius,
                LeftLaneX = layout.LeftLaneX,
                RightLaneX = layout.RightLaneX,
                LowerConnectorY = layout.LowerConnectorY,
                UpperConnectorY = layout.UpperConnectorY
            };

            for (int i = 0; i < layout.Objects.Count; i++)
            {
                WarehouseObjectPlacement obj = layout.Objects[i];
                if (obj.Side == WarehouseSide.A && obj.Origin.y < layout.Height / 2)
                    genome.Objects.Add(ClonePlacement(obj));
            }

            return genome;
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

        private static bool IsBridgeHorizontal(WarehouseObjectPlacement obj)
        {
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
