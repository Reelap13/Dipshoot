using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.ProcGen.City
{
    public sealed class CitySurfacePass : ProcGenPass
    {
        public override string Id => "city-surface";

        public override void Declare(GenerationPassContract contract)
        {
            contract.Write(CityKeys.Surface);
        }

        public override void Execute(GenerationContext context)
        {
            CityRecipe recipe = context.GetRecipe<CityRecipe>();
            System.Random random = context.CreateRandom(Id);
            Vector2 size = recipe.MapSize;
            Rect bounds = new Rect(-size.x * 0.5f, -size.y * 0.5f, size.x, size.y);
            CitySurfaceData surface = new CitySurfaceData(size, bounds);

            if (recipe.WaterwayWidth > 0.01f)
            {
                float canalCenter = Mathf.Lerp(bounds.yMin + size.y * 0.22f, bounds.yMax - size.y * 0.22f, (float)random.NextDouble());
                surface.WaterAreas.Add(new Rect(bounds.xMin, canalCenter - recipe.WaterwayWidth * 0.5f, size.x, recipe.WaterwayWidth));
            }

            Vector2 blockerSize = new Vector2(size.x * 0.11f, size.y * 0.13f);
            Vector2 blockerPosition = new Vector2(
                Mathf.Lerp(bounds.xMin + blockerSize.x, bounds.xMax - blockerSize.x * 2f, (float)random.NextDouble()),
                Mathf.Lerp(bounds.yMin + blockerSize.y, bounds.yMax - blockerSize.y * 2f, (float)random.NextDouble()));
            surface.BlockedAreas.Add(new Rect(blockerPosition.x, blockerPosition.y, blockerSize.x, blockerSize.y));
            context.Blackboard.Set(CityKeys.Surface, surface);
        }
    }

    public sealed class CityZonePass : ProcGenPass
    {
        public override string Id => "city-zones";

        public override void Declare(GenerationPassContract contract)
        {
            contract.Read(CityKeys.Surface);
            contract.Write(CityKeys.Zones);
        }

        public override void Execute(GenerationContext context)
        {
            CityRecipe recipe = context.GetRecipe<CityRecipe>();
            CitySurfaceData surface = context.Blackboard.GetRequired(CityKeys.Surface);
            CityZoneData zones = new CityZoneData();
            Vector2Int gridSize = recipe.ZoneGridSize;
            float cellWidth = surface.Bounds.width / gridSize.x;
            float cellHeight = surface.Bounds.height / gridSize.y;
            int centerX = gridSize.x / 2;
            int centerY = gridSize.y / 2;
            System.Random random = context.CreateRandom(Id);
            int id = 0;

            for (int y = 0; y < gridSize.y; y++)
            {
                for (int x = 0; x < gridSize.x; x++)
                {
                    Rect bounds = new Rect(
                        surface.Bounds.xMin + x * cellWidth + recipe.ZonePadding,
                        surface.Bounds.yMin + y * cellHeight + recipe.ZonePadding,
                        cellWidth - recipe.ZonePadding * 2f,
                        cellHeight - recipe.ZonePadding * 2f);

                    CityZoneType type = PickZoneType(x, y, centerX, centerY, gridSize, random);
                    zones.Zones.Add(new CityZone
                    {
                        Id = id++,
                        Type = type,
                        Bounds = bounds,
                        Center = bounds.center,
                        DangerLevel = GetDangerLevel(type)
                    });
                }
            }

            context.Blackboard.Set(CityKeys.Zones, zones);
        }

        private static CityZoneType PickZoneType(int x, int y, int centerX, int centerY, Vector2Int gridSize, System.Random random)
        {
            if (x == 0 && y == 0)
                return CityZoneType.Safe;

            if (x == centerX && y == centerY)
                return CityZoneType.Market;

            if (x == gridSize.x - 1 && y == gridSize.y - 1)
                return CityZoneType.Boss;

            if (y == 0)
                return CityZoneType.Residential;

            if (x == 0)
                return CityZoneType.Port;

            if (x == gridSize.x - 1)
                return CityZoneType.Ruins;

            CityZoneType[] candidates =
            {
                CityZoneType.Residential,
                CityZoneType.Industrial,
                CityZoneType.Danger,
                CityZoneType.Ruins
            };

            return candidates[random.Next(0, candidates.Length)];
        }

        private static float GetDangerLevel(CityZoneType type)
        {
            switch (type)
            {
                case CityZoneType.Safe:
                    return 0f;
                case CityZoneType.Market:
                case CityZoneType.Residential:
                case CityZoneType.Port:
                    return 0.25f;
                case CityZoneType.Industrial:
                case CityZoneType.Ruins:
                    return 0.6f;
                case CityZoneType.Danger:
                    return 0.8f;
                case CityZoneType.Boss:
                    return 1f;
                default:
                    return 0.5f;
            }
        }
    }

    public sealed class CityGlobalSkeletonPass : ProcGenPass
    {
        public override string Id => "city-global-skeleton";

        public override void Declare(GenerationPassContract contract)
        {
            contract.Read(CityKeys.Surface);
            contract.Read(CityKeys.Zones);
            contract.Write(CityKeys.GlobalRoads);
        }

        public override void Execute(GenerationContext context)
        {
            CityRecipe recipe = context.GetRecipe<CityRecipe>();
            CitySurfaceData surface = context.Blackboard.GetRequired(CityKeys.Surface);
            CityZoneData zones = context.Blackboard.GetRequired(CityKeys.Zones);
            CityRoadGraphData graph = new CityRoadGraphData();
            Vector2 center = surface.Bounds.center;

            AddRoad(graph, new Vector2(surface.Bounds.xMin, center.y), new Vector2(surface.Bounds.xMax, center.y), recipe.MainRoadWidth, true, -1);
            AddRoad(graph, new Vector2(center.x, surface.Bounds.yMin), new Vector2(center.x, surface.Bounds.yMax), recipe.MainRoadWidth, true, -1);

            for (int i = 0; i < zones.Zones.Count; i++)
            {
                CityZone zone = zones.Zones[i];
                Vector2 joint = new Vector2(zone.Center.x, center.y);
                AddRoad(graph, zone.Center, joint, recipe.MainRoadWidth, true, zone.Id);
                AddRoad(graph, joint, center, recipe.MainRoadWidth, true, zone.Id);
            }

            context.Blackboard.Set(CityKeys.GlobalRoads, graph);
        }

        private static void AddRoad(CityRoadGraphData graph, Vector2 start, Vector2 end, float width, bool isMain, int zoneId)
        {
            if ((end - start).sqrMagnitude < 0.01f)
                return;

            graph.Roads.Add(new CityRoadSegment
            {
                Start = start,
                End = end,
                Width = width,
                IsMain = isMain,
                ZoneId = zoneId
            });
        }
    }

    public sealed class CityLocalStructurePass : ProcGenPass
    {
        public override string Id => "city-local-structure";

        public override void Declare(GenerationPassContract contract)
        {
            contract.Read(CityKeys.Zones);
            contract.Write(CityKeys.LocalLayout);
        }

        public override void Execute(GenerationContext context)
        {
            CityRecipe recipe = context.GetRecipe<CityRecipe>();
            CityZoneData zones = context.Blackboard.GetRequired(CityKeys.Zones);
            CityLocalLayoutData layout = new CityLocalLayoutData();

            for (int i = 0; i < zones.Zones.Count; i++)
                BuildZoneLayout(recipe, zones.Zones[i], layout);

            context.Blackboard.Set(CityKeys.LocalLayout, layout);
        }

        private static void BuildZoneLayout(CityRecipe recipe, CityZone zone, CityLocalLayoutData layout)
        {
            List<float> xCuts = BuildCuts(zone.Bounds.xMin, zone.Bounds.xMax, recipe.LocalRoadSpacing);
            List<float> zCuts = BuildCuts(zone.Bounds.yMin, zone.Bounds.yMax, recipe.LocalRoadSpacing);

            for (int x = 1; x < xCuts.Count - 1; x++)
            {
                layout.LocalRoads.Add(new CityRoadSegment
                {
                    Start = new Vector2(xCuts[x], zone.Bounds.yMin),
                    End = new Vector2(xCuts[x], zone.Bounds.yMax),
                    Width = recipe.LocalRoadWidth,
                    IsMain = false,
                    ZoneId = zone.Id
                });
            }

            for (int z = 1; z < zCuts.Count - 1; z++)
            {
                layout.LocalRoads.Add(new CityRoadSegment
                {
                    Start = new Vector2(zone.Bounds.xMin, zCuts[z]),
                    End = new Vector2(zone.Bounds.xMax, zCuts[z]),
                    Width = recipe.LocalRoadWidth,
                    IsMain = false,
                    ZoneId = zone.Id
                });
            }

            for (int z = 0; z < zCuts.Count - 1; z++)
            {
                for (int x = 0; x < xCuts.Count - 1; x++)
                {
                    Rect block = Rect.MinMaxRect(
                        xCuts[x] + recipe.LocalRoadWidth * 0.5f,
                        zCuts[z] + recipe.LocalRoadWidth * 0.5f,
                        xCuts[x + 1] - recipe.LocalRoadWidth * 0.5f,
                        zCuts[z + 1] - recipe.LocalRoadWidth * 0.5f);

                    if (block.width < 5f || block.height < 5f)
                        continue;

                    bool centerBlock = Mathf.Abs(block.center.x - zone.Center.x) < recipe.LocalRoadSpacing * 0.35f
                        && Mathf.Abs(block.center.y - zone.Center.y) < recipe.LocalRoadSpacing * 0.35f;

                    layout.Blocks.Add(new CityBlock
                    {
                        ZoneId = zone.Id,
                        Bounds = block,
                        ReservedOpenSpace = centerBlock && (zone.Type == CityZoneType.Market || zone.Type == CityZoneType.Safe)
                    });
                }
            }
        }

        private static List<float> BuildCuts(float min, float max, float spacing)
        {
            List<float> cuts = new List<float> { min };
            for (float value = min + spacing; value < max - spacing * 0.4f; value += spacing)
                cuts.Add(value);
            cuts.Add(max);
            return cuts;
        }
    }

    public sealed class CityArchitecturePass : ProcGenPass
    {
        public override string Id => "city-architecture";

        public override void Declare(GenerationPassContract contract)
        {
            contract.Read(CityKeys.Surface);
            contract.Read(CityKeys.Zones);
            contract.Read(CityKeys.LocalLayout);
            contract.Write(CityKeys.Architecture);
        }

        public override void Execute(GenerationContext context)
        {
            CityRecipe recipe = context.GetRecipe<CityRecipe>();
            CitySurfaceData surface = context.Blackboard.GetRequired(CityKeys.Surface);
            CityZoneData zones = context.Blackboard.GetRequired(CityKeys.Zones);
            CityLocalLayoutData layout = context.Blackboard.GetRequired(CityKeys.LocalLayout);
            CityArchitectureData architecture = new CityArchitectureData();
            System.Random random = context.CreateRandom(Id);

            for (int i = 0; i < layout.Blocks.Count; i++)
            {
                CityBlock block = layout.Blocks[i];
                CityZone zone = FindZone(zones, block.ZoneId);
                if (zone == null)
                    continue;

                if (block.ReservedOpenSpace)
                {
                    AddStructure(architecture, CityStructureKind.Plaza, zone, block.Bounds.center, 0f, block.Bounds.size * 0.8f, 0.12f);
                    continue;
                }

                Rect footprint = Inset(block.Bounds, recipe.BuildingSetback);
                if (footprint.width < 3f || footprint.height < 3f || surface.IsBuildable(footprint) == false)
                    continue;

                if (zone.Type == CityZoneType.Safe && random.NextDouble() < 0.35)
                    continue;

                CityStructureKind kind = PickStructureKind(zone.Type);
                float height = PickHeight(recipe, zone.Type, random);
                AddStructure(architecture, kind, zone, footprint.center, 0f, footprint.size, height);
            }

            AddBossWalls(recipe, zones, architecture);
            context.Blackboard.Set(CityKeys.Architecture, architecture);
        }

        private static CityStructureKind PickStructureKind(CityZoneType type)
        {
            switch (type)
            {
                case CityZoneType.Market:
                    return CityStructureKind.MarketStall;
                case CityZoneType.Industrial:
                case CityZoneType.Port:
                    return CityStructureKind.Warehouse;
                case CityZoneType.Ruins:
                    return CityStructureKind.Ruin;
                default:
                    return CityStructureKind.Building;
            }
        }

        private static float PickHeight(CityRecipe recipe, CityZoneType type, System.Random random)
        {
            Vector2 range = recipe.BuildingHeightRange;
            float t = (float)random.NextDouble();
            float height = Mathf.Lerp(range.x, range.y, t);

            if (type == CityZoneType.Market || type == CityZoneType.Safe)
                height *= 0.55f;
            else if (type == CityZoneType.Ruins)
                height *= 0.45f;
            else if (type == CityZoneType.Boss)
                height *= 1.35f;

            return Mathf.Max(1f, height);
        }

        private static void AddBossWalls(CityRecipe recipe, CityZoneData zones, CityArchitectureData architecture)
        {
            for (int i = 0; i < zones.Zones.Count; i++)
            {
                CityZone zone = zones.Zones[i];
                if (zone.Type != CityZoneType.Boss)
                    continue;

                float wallThickness = 1.2f;
                AddStructure(architecture, CityStructureKind.Wall, zone, new Vector2(zone.Center.x, zone.Bounds.yMin), 0f, new Vector2(zone.Bounds.width, wallThickness), recipe.WallHeight);
                AddStructure(architecture, CityStructureKind.Wall, zone, new Vector2(zone.Center.x, zone.Bounds.yMax), 0f, new Vector2(zone.Bounds.width, wallThickness), recipe.WallHeight);
                AddStructure(architecture, CityStructureKind.Wall, zone, new Vector2(zone.Bounds.xMin, zone.Center.y), 90f, new Vector2(zone.Bounds.height, wallThickness), recipe.WallHeight);
                AddStructure(architecture, CityStructureKind.Wall, zone, new Vector2(zone.Bounds.xMax, zone.Center.y), 90f, new Vector2(zone.Bounds.height, wallThickness), recipe.WallHeight);

                AddStructure(architecture, CityStructureKind.Tower, zone, new Vector2(zone.Bounds.xMin, zone.Bounds.yMin), 0f, new Vector2(4f, 4f), recipe.TowerHeight);
                AddStructure(architecture, CityStructureKind.Tower, zone, new Vector2(zone.Bounds.xMin, zone.Bounds.yMax), 0f, new Vector2(4f, 4f), recipe.TowerHeight);
                AddStructure(architecture, CityStructureKind.Tower, zone, new Vector2(zone.Bounds.xMax, zone.Bounds.yMin), 0f, new Vector2(4f, 4f), recipe.TowerHeight);
                AddStructure(architecture, CityStructureKind.Tower, zone, new Vector2(zone.Bounds.xMax, zone.Bounds.yMax), 0f, new Vector2(4f, 4f), recipe.TowerHeight);
            }
        }

        private static void AddStructure(CityArchitectureData architecture, CityStructureKind kind, CityZone zone, Vector2 position, float rotationY, Vector2 size, float height)
        {
            architecture.Structures.Add(new CityStructurePlacement
            {
                Kind = kind,
                ZoneType = zone.Type,
                ZoneId = zone.Id,
                Position = position,
                RotationY = rotationY,
                Size = size,
                Height = height
            });
        }

        private static Rect Inset(Rect rect, float value)
        {
            return Rect.MinMaxRect(rect.xMin + value, rect.yMin + value, rect.xMax - value, rect.yMax - value);
        }

        private static CityZone FindZone(CityZoneData zones, int zoneId)
        {
            for (int i = 0; i < zones.Zones.Count; i++)
            {
                if (zones.Zones[i].Id == zoneId)
                    return zones.Zones[i];
            }

            return null;
        }
    }

    public sealed class CityGameplayPass : ProcGenPass
    {
        public override string Id => "city-gameplay";

        public override void Declare(GenerationPassContract contract)
        {
            contract.Read(CityKeys.Zones);
            contract.Read(CityKeys.Architecture);
            contract.Write(CityKeys.Gameplay);
        }

        public override void Execute(GenerationContext context)
        {
            CityRecipe recipe = context.GetRecipe<CityRecipe>();
            CityZoneData zones = context.Blackboard.GetRequired(CityKeys.Zones);
            CityGameplayData gameplay = new CityGameplayData();
            System.Random random = context.CreateRandom(Id);

            for (int i = 0; i < zones.Zones.Count; i++)
            {
                CityZone zone = zones.Zones[i];

                if (zone.Type == CityZoneType.Safe)
                    Add(gameplay, CityGameplayMarkerKind.PlayerSpawn, zone, zone.Center, 2f);

                if (zone.Type == CityZoneType.Market)
                {
                    Add(gameplay, CityGameplayMarkerKind.Trader, zone, zone.Center + new Vector2(4f, 0f), 2f);
                    Add(gameplay, CityGameplayMarkerKind.Quest, zone, zone.Center + new Vector2(-4f, 0f), 2f);
                }

                if (zone.Type == CityZoneType.Boss)
                    Add(gameplay, CityGameplayMarkerKind.Encounter, zone, zone.Center, 5f);

                if (zone.DangerLevel >= 0.55f)
                {
                    int enemyCount = zone.Type == CityZoneType.Boss ? recipe.EnemyMarkersPerDangerZone + 2 : recipe.EnemyMarkersPerDangerZone;
                    for (int enemy = 0; enemy < enemyCount; enemy++)
                        Add(gameplay, CityGameplayMarkerKind.EnemySpawn, zone, RandomPoint(zone.Bounds, 5f, random), 1.5f);
                }

                int lootCount = zone.Type == CityZoneType.Safe ? 1 : recipe.LootMarkersPerZone;
                for (int loot = 0; loot < lootCount; loot++)
                    Add(gameplay, CityGameplayMarkerKind.Loot, zone, RandomPoint(zone.Bounds, 5f, random), 1f);
            }

            context.Blackboard.Set(CityKeys.Gameplay, gameplay);
        }

        private static void Add(CityGameplayData gameplay, CityGameplayMarkerKind kind, CityZone zone, Vector2 position, float radius)
        {
            gameplay.Placements.Add(new CityGameplayPlacement
            {
                Kind = kind,
                ZoneId = zone.Id,
                Position = position,
                RotationY = 0f,
                Radius = radius
            });
        }

        private static Vector2 RandomPoint(Rect rect, float margin, System.Random random)
        {
            float x = Mathf.Lerp(rect.xMin + margin, rect.xMax - margin, (float)random.NextDouble());
            float z = Mathf.Lerp(rect.yMin + margin, rect.yMax - margin, (float)random.NextDouble());
            return new Vector2(x, z);
        }
    }

    public sealed class CityVisualDetailPass : ProcGenPass
    {
        public override string Id => "city-visual-detail";

        public override void Declare(GenerationPassContract contract)
        {
            contract.Read(CityKeys.Zones);
            contract.Read(CityKeys.GlobalRoads);
            contract.Read(CityKeys.LocalLayout);
            contract.Read(CityKeys.Architecture);
            contract.Write(CityKeys.Details);
        }

        public override void Execute(GenerationContext context)
        {
            CityRecipe recipe = context.GetRecipe<CityRecipe>();
            CityZoneData zones = context.Blackboard.GetRequired(CityKeys.Zones);
            CityRoadGraphData globalRoads = context.Blackboard.GetRequired(CityKeys.GlobalRoads);
            CityLocalLayoutData localLayout = context.Blackboard.GetRequired(CityKeys.LocalLayout);
            CityArchitectureData architecture = context.Blackboard.GetRequired(CityKeys.Architecture);
            CityDetailData details = new CityDetailData();
            System.Random random = context.CreateRandom(Id);

            AddRoadLamps(recipe, details, globalRoads.Roads, true);
            AddRoadLamps(recipe, details, localLayout.LocalRoads, false);
            AddZoneDetails(recipe, zones, details, random);
            AddArchitectureDetails(architecture, details, random);
            context.Blackboard.Set(CityKeys.Details, details);
        }

        private static void AddRoadLamps(CityRecipe recipe, CityDetailData details, IReadOnlyList<CityRoadSegment> roads, bool includeAll)
        {
            float spacing = recipe.DetailSpacing;
            for (int i = 0; i < roads.Count; i++)
            {
                CityRoadSegment road = roads[i];
                if (includeAll == false && road.ZoneId % 2 != 0)
                    continue;

                Vector2 delta = road.End - road.Start;
                float length = delta.magnitude;
                if (length < spacing)
                    continue;

                Vector2 direction = delta / length;
                Vector2 normal = new Vector2(-direction.y, direction.x) * (road.Width * 0.75f);
                int count = Mathf.FloorToInt(length / spacing);
                for (int lamp = 1; lamp < count; lamp++)
                {
                    Vector2 position = road.Start + direction * (lamp * spacing) + normal;
                    details.Placements.Add(new CityDetailPlacement
                    {
                        Kind = CityDetailKind.Lamp,
                        ZoneId = road.ZoneId,
                        Position = position,
                        RotationY = 0f,
                        Scale = Vector3.one
                    });
                }
            }
        }

        private static void AddZoneDetails(CityRecipe recipe, CityZoneData zones, CityDetailData details, System.Random random)
        {
            int extraPerZone = Mathf.RoundToInt(4f * recipe.DetailDensity);
            for (int i = 0; i < zones.Zones.Count; i++)
            {
                CityZone zone = zones.Zones[i];
                for (int item = 0; item < extraPerZone; item++)
                {
                    CityDetailKind kind = PickZoneDetail(zone.Type, random);
                    details.Placements.Add(new CityDetailPlacement
                    {
                        Kind = kind,
                        ZoneId = zone.Id,
                        Position = RandomPoint(zone.Bounds, 4f, random),
                        RotationY = (float)random.NextDouble() * 360f,
                        Scale = Vector3.one * Mathf.Lerp(0.75f, 1.4f, (float)random.NextDouble())
                    });
                }
            }
        }

        private static void AddArchitectureDetails(CityArchitectureData architecture, CityDetailData details, System.Random random)
        {
            for (int i = 0; i < architecture.Structures.Count; i++)
            {
                CityStructurePlacement structure = architecture.Structures[i];
                if (structure.Kind == CityStructureKind.MarketStall && random.NextDouble() < 0.7)
                {
                    details.Placements.Add(new CityDetailPlacement
                    {
                        Kind = CityDetailKind.Sign,
                        ZoneId = structure.ZoneId,
                        Position = structure.Position + new Vector2(0f, structure.Size.y * 0.5f + 1f),
                        RotationY = 0f,
                        Scale = Vector3.one
                    });
                }

                if (structure.Kind == CityStructureKind.Ruin && random.NextDouble() < 0.7)
                {
                    details.Placements.Add(new CityDetailPlacement
                    {
                        Kind = CityDetailKind.Debris,
                        ZoneId = structure.ZoneId,
                        Position = structure.Position + RandomOffset(structure.Size, random),
                        RotationY = (float)random.NextDouble() * 360f,
                        Scale = Vector3.one
                    });
                }
            }
        }

        private static CityDetailKind PickZoneDetail(CityZoneType type, System.Random random)
        {
            switch (type)
            {
                case CityZoneType.Safe:
                case CityZoneType.Residential:
                    return CityDetailKind.Tree;
                case CityZoneType.Market:
                    return random.NextDouble() < 0.5 ? CityDetailKind.Sign : CityDetailKind.Crate;
                case CityZoneType.Ruins:
                case CityZoneType.Danger:
                case CityZoneType.Boss:
                    return CityDetailKind.Debris;
                default:
                    return CityDetailKind.Crate;
            }
        }

        private static Vector2 RandomPoint(Rect rect, float margin, System.Random random)
        {
            float x = Mathf.Lerp(rect.xMin + margin, rect.xMax - margin, (float)random.NextDouble());
            float z = Mathf.Lerp(rect.yMin + margin, rect.yMax - margin, (float)random.NextDouble());
            return new Vector2(x, z);
        }

        private static Vector2 RandomOffset(Vector2 size, System.Random random)
        {
            float x = Mathf.Lerp(-size.x * 0.35f, size.x * 0.35f, (float)random.NextDouble());
            float z = Mathf.Lerp(-size.y * 0.35f, size.y * 0.35f, (float)random.NextDouble());
            return new Vector2(x, z);
        }
    }

    public sealed class CityBuildPlanPass : ProcGenPass
    {
        public override string Id => "city-build-plan";

        public override void Declare(GenerationPassContract contract)
        {
            contract.Read(CityKeys.Surface);
            contract.Read(CityKeys.Zones);
            contract.Read(CityKeys.GlobalRoads);
            contract.Read(CityKeys.LocalLayout);
            contract.Read(CityKeys.Architecture);
            contract.Read(CityKeys.Gameplay);
            contract.Read(CityKeys.Details);
            contract.Write(CityKeys.BuildPlan);
        }

        public override void Execute(GenerationContext context)
        {
            CityBuildPlan plan = new CityBuildPlan
            {
                Surface = context.Blackboard.GetRequired(CityKeys.Surface),
                Zones = context.Blackboard.GetRequired(CityKeys.Zones),
                GlobalRoads = context.Blackboard.GetRequired(CityKeys.GlobalRoads),
                LocalLayout = context.Blackboard.GetRequired(CityKeys.LocalLayout),
                Architecture = context.Blackboard.GetRequired(CityKeys.Architecture),
                Gameplay = context.Blackboard.GetRequired(CityKeys.Gameplay),
                Details = context.Blackboard.GetRequired(CityKeys.Details)
            };

            context.Blackboard.Set(CityKeys.BuildPlan, plan);
        }
    }
}
