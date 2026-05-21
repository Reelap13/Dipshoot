using UnityEngine;

namespace Game.ProcGen.City
{
    public static class CityPlanExecutor
    {
        public static void Execute(CityBuildPlan plan, Transform parent, CityRecipe recipe, string rootName, bool clearPreviousOutput)
        {
            if (clearPreviousOutput)
                DestroyExistingRoot(parent, rootName);

            Transform root = new GameObject(rootName).transform;
            root.SetParent(parent, false);

            BuildSurface(plan, root);
            BuildRoads(plan, root);
            BuildArchitecture(plan, root);
            BuildGameplayMarkers(plan, root);
            BuildDetails(plan, root);
        }

        private static void DestroyExistingRoot(Transform parent, string rootName)
        {
            Transform existing = parent.Find(rootName);
            if (existing == null)
                return;

            if (Application.isPlaying)
                Object.Destroy(existing.gameObject);
            else
                Object.DestroyImmediate(existing.gameObject);
        }

        private static void BuildSurface(CityBuildPlan plan, Transform root)
        {
            Transform surfaceRoot = CreateRoot("Surface", root);
            CitySurfaceData surface = plan.Surface;

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.SetParent(surfaceRoot, false);
            ground.transform.localPosition = new Vector3(surface.Bounds.center.x, -0.08f, surface.Bounds.center.y);
            ground.transform.localScale = new Vector3(surface.Bounds.width, 0.12f, surface.Bounds.height);
            SetMaterial(ground, new Color(0.26f, 0.34f, 0.27f));

            Transform zoneRoot = CreateRoot("ZoneOverlays", surfaceRoot);
            for (int i = 0; i < plan.Zones.Zones.Count; i++)
            {
                CityZone zone = plan.Zones.Zones[i];
                GameObject overlay = GameObject.CreatePrimitive(PrimitiveType.Cube);
                overlay.name = $"Zone_{zone.Id}_{zone.Type}";
                overlay.transform.SetParent(zoneRoot, false);
                overlay.transform.localPosition = new Vector3(zone.Bounds.center.x, 0.005f, zone.Bounds.center.y);
                overlay.transform.localScale = new Vector3(zone.Bounds.width, 0.02f, zone.Bounds.height);
                SetMaterial(overlay, GetZoneColor(zone.Type));
            }

            for (int i = 0; i < surface.WaterAreas.Count; i++)
            {
                Rect water = surface.WaterAreas[i];
                GameObject waterObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                waterObject.name = $"Water_{i}";
                waterObject.transform.SetParent(surfaceRoot, false);
                waterObject.transform.localPosition = new Vector3(water.center.x, 0.04f, water.center.y);
                waterObject.transform.localScale = new Vector3(water.width, 0.05f, water.height);
                SetMaterial(waterObject, new Color(0.07f, 0.29f, 0.48f));
            }
        }

        private static void BuildRoads(CityBuildPlan plan, Transform root)
        {
            Transform roadsRoot = CreateRoot("Roads", root);

            for (int i = 0; i < plan.GlobalRoads.Roads.Count; i++)
                BuildRoad(plan.GlobalRoads.Roads[i], roadsRoot);

            for (int i = 0; i < plan.LocalLayout.LocalRoads.Count; i++)
                BuildRoad(plan.LocalLayout.LocalRoads[i], roadsRoot);
        }

        private static void BuildRoad(CityRoadSegment road, Transform parent)
        {
            Vector2 delta = road.End - road.Start;
            float length = delta.magnitude;
            if (length <= 0.01f)
                return;

            Vector2 center = (road.Start + road.End) * 0.5f;
            GameObject roadObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roadObject.name = road.IsMain ? "MainRoad" : "LocalRoad";
            roadObject.transform.SetParent(parent, false);
            roadObject.transform.localPosition = new Vector3(center.x, 0.08f, center.y);
            roadObject.transform.localRotation = Quaternion.Euler(0f, Mathf.Atan2(delta.x, delta.y) * Mathf.Rad2Deg, 0f);
            roadObject.transform.localScale = new Vector3(road.Width, 0.08f, length);
            SetMaterial(roadObject, road.IsMain ? new Color(0.16f, 0.16f, 0.16f) : new Color(0.22f, 0.21f, 0.19f));
        }

        private static void BuildArchitecture(CityBuildPlan plan, Transform root)
        {
            Transform architectureRoot = CreateRoot("Architecture", root);

            for (int i = 0; i < plan.Architecture.Structures.Count; i++)
            {
                CityStructurePlacement structure = plan.Architecture.Structures[i];
                GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obj.name = $"{structure.Kind}_{structure.ZoneId}";
                obj.transform.SetParent(architectureRoot, false);
                obj.transform.localPosition = new Vector3(structure.Position.x, structure.Height * 0.5f, structure.Position.y);
                obj.transform.localRotation = Quaternion.Euler(0f, structure.RotationY, 0f);
                obj.transform.localScale = new Vector3(structure.Size.x, Mathf.Max(0.05f, structure.Height), structure.Size.y);
                SetMaterial(obj, GetStructureColor(structure));
            }
        }

        private static void BuildGameplayMarkers(CityBuildPlan plan, Transform root)
        {
            Transform gameplayRoot = CreateRoot("GameplayMarkers", root);

            for (int i = 0; i < plan.Gameplay.Placements.Count; i++)
            {
                CityGameplayPlacement placement = plan.Gameplay.Placements[i];
                GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                obj.name = $"{placement.Kind}_{placement.ZoneId}";
                obj.transform.SetParent(gameplayRoot, false);
                obj.transform.localPosition = new Vector3(placement.Position.x, 0.8f, placement.Position.y);
                obj.transform.localScale = Vector3.one * placement.Radius;
                SetMaterial(obj, GetGameplayColor(placement.Kind));
            }
        }

        private static void BuildDetails(CityBuildPlan plan, Transform root)
        {
            Transform detailRoot = CreateRoot("VisualDetails", root);

            for (int i = 0; i < plan.Details.Placements.Count; i++)
            {
                CityDetailPlacement placement = plan.Details.Placements[i];
                BuildDetail(placement, detailRoot);
            }
        }

        private static void BuildDetail(CityDetailPlacement placement, Transform parent)
        {
            switch (placement.Kind)
            {
                case CityDetailKind.Lamp:
                    BuildLamp(placement, parent);
                    break;
                case CityDetailKind.Tree:
                    BuildTree(placement, parent);
                    break;
                default:
                    BuildSimpleDetail(placement, parent);
                    break;
            }
        }

        private static void BuildLamp(CityDetailPlacement placement, Transform parent)
        {
            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = $"Lamp_{placement.ZoneId}";
            pole.transform.SetParent(parent, false);
            pole.transform.localPosition = new Vector3(placement.Position.x, 1.5f, placement.Position.y);
            pole.transform.localScale = new Vector3(0.12f, 1.5f, 0.12f);
            SetMaterial(pole, new Color(0.08f, 0.08f, 0.07f));

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "LampHead";
            head.transform.SetParent(pole.transform, false);
            head.transform.localPosition = new Vector3(0f, 1f, 0f);
            head.transform.localScale = Vector3.one * 0.8f;
            SetMaterial(head, new Color(1f, 0.82f, 0.38f));
        }

        private static void BuildTree(CityDetailPlacement placement, Transform parent)
        {
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = $"Tree_{placement.ZoneId}";
            trunk.transform.SetParent(parent, false);
            trunk.transform.localPosition = new Vector3(placement.Position.x, 0.7f, placement.Position.y);
            trunk.transform.localScale = Vector3.Scale(new Vector3(0.25f, 0.7f, 0.25f), placement.Scale);
            SetMaterial(trunk, new Color(0.28f, 0.17f, 0.09f));

            GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            crown.name = "Crown";
            crown.transform.SetParent(trunk.transform, false);
            crown.transform.localPosition = new Vector3(0f, 1.4f, 0f);
            crown.transform.localScale = Vector3.one * 2.5f;
            SetMaterial(crown, new Color(0.12f, 0.36f, 0.16f));
        }

        private static void BuildSimpleDetail(CityDetailPlacement placement, Transform parent)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = $"{placement.Kind}_{placement.ZoneId}";
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = new Vector3(placement.Position.x, 0.35f, placement.Position.y);
            obj.transform.localRotation = Quaternion.Euler(0f, placement.RotationY, 0f);
            obj.transform.localScale = Vector3.Scale(GetDetailBaseScale(placement.Kind), placement.Scale);
            SetMaterial(obj, GetDetailColor(placement.Kind));
        }

        private static Transform CreateRoot(string name, Transform parent)
        {
            Transform root = new GameObject(name).transform;
            root.SetParent(parent, false);
            return root;
        }

        private static void SetMaterial(GameObject obj, Color color)
        {
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer == null)
                return;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            Material material = new Material(shader);
            material.color = color;
            renderer.sharedMaterial = material;
        }

        private static Color GetZoneColor(CityZoneType type)
        {
            switch (type)
            {
                case CityZoneType.Safe:
                    return new Color(0.20f, 0.43f, 0.30f);
                case CityZoneType.Market:
                    return new Color(0.48f, 0.37f, 0.18f);
                case CityZoneType.Residential:
                    return new Color(0.34f, 0.40f, 0.50f);
                case CityZoneType.Industrial:
                    return new Color(0.35f, 0.35f, 0.34f);
                case CityZoneType.Port:
                    return new Color(0.20f, 0.37f, 0.47f);
                case CityZoneType.Ruins:
                    return new Color(0.35f, 0.29f, 0.27f);
                case CityZoneType.Danger:
                    return new Color(0.44f, 0.19f, 0.18f);
                case CityZoneType.Boss:
                    return new Color(0.28f, 0.16f, 0.32f);
                default:
                    return Color.gray;
            }
        }

        private static Color GetStructureColor(CityStructurePlacement structure)
        {
            switch (structure.Kind)
            {
                case CityStructureKind.Warehouse:
                    return new Color(0.30f, 0.31f, 0.31f);
                case CityStructureKind.MarketStall:
                    return new Color(0.58f, 0.41f, 0.18f);
                case CityStructureKind.Plaza:
                    return new Color(0.56f, 0.54f, 0.48f);
                case CityStructureKind.Ruin:
                    return new Color(0.25f, 0.23f, 0.22f);
                case CityStructureKind.Wall:
                case CityStructureKind.Tower:
                    return new Color(0.18f, 0.17f, 0.19f);
                default:
                    return structure.ZoneType == CityZoneType.Boss
                        ? new Color(0.32f, 0.22f, 0.36f)
                        : new Color(0.43f, 0.42f, 0.39f);
            }
        }

        private static Color GetGameplayColor(CityGameplayMarkerKind kind)
        {
            switch (kind)
            {
                case CityGameplayMarkerKind.PlayerSpawn:
                    return new Color(0.1f, 0.45f, 1f);
                case CityGameplayMarkerKind.EnemySpawn:
                    return new Color(0.85f, 0.1f, 0.08f);
                case CityGameplayMarkerKind.Loot:
                    return new Color(1f, 0.84f, 0.12f);
                case CityGameplayMarkerKind.Trader:
                    return new Color(0.15f, 0.75f, 0.25f);
                case CityGameplayMarkerKind.Quest:
                    return new Color(0.15f, 0.85f, 0.85f);
                case CityGameplayMarkerKind.Encounter:
                    return new Color(0.75f, 0.12f, 0.9f);
                default:
                    return Color.white;
            }
        }

        private static Vector3 GetDetailBaseScale(CityDetailKind kind)
        {
            switch (kind)
            {
                case CityDetailKind.Sign:
                    return new Vector3(1.6f, 0.8f, 0.15f);
                case CityDetailKind.Crate:
                    return new Vector3(1.1f, 0.9f, 1.1f);
                case CityDetailKind.Debris:
                    return new Vector3(1.4f, 0.25f, 0.9f);
                default:
                    return Vector3.one;
            }
        }

        private static Color GetDetailColor(CityDetailKind kind)
        {
            switch (kind)
            {
                case CityDetailKind.Sign:
                    return new Color(0.75f, 0.30f, 0.16f);
                case CityDetailKind.Crate:
                    return new Color(0.40f, 0.24f, 0.12f);
                case CityDetailKind.Debris:
                    return new Color(0.16f, 0.15f, 0.14f);
                default:
                    return Color.white;
            }
        }
    }
}
