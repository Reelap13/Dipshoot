using System.Collections.Generic;
using Game.Level;
using Game.MatchMode;
using UnityEngine;

namespace Game.ProcGen.Warehouse
{
    public static class WarehousePlanExecutor
    {
        private const float FloorThickness = 0.2f;
        private const float WallHeight = 3.2f;
        private const float WallThickness = 0.5f;
        private const float MarkerThickness = 0.04f;
        private const float SpawnMarkerDiameter = 0.6f;

        public static void Execute(
            WarehouseBuildPlan plan,
            Transform parent,
            WarehouseRecipe recipe,
            string rootName,
            bool clearPreviousOutput)
        {
            if (clearPreviousOutput)
                DestroyExistingRoot(parent, rootName);

            Transform root = new GameObject(rootName).transform;
            root.SetParent(parent, false);

            Transform mapRoot = CreateRoot("Map", root);
            Transform gameplayRoot = CreateRoot("Gameplay", root);
            BuildFloor(plan.Layout, mapRoot, recipe);
            BuildOuterWalls(plan.Layout, mapRoot, recipe);
            BuildObjects(plan.Layout, mapRoot, recipe);
            BuildGameplay(plan.Layout, gameplayRoot, recipe, parent.GetComponent<LevelController>());
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

        private static void BuildFloor(WarehouseLayoutData layout, Transform parent, WarehouseRecipe recipe)
        {
            Vector3 size = new(layout.Width * GetCellSizeX(layout), FloorThickness, layout.Height * GetCellSizeZ(layout));
            InstantiatePrefab(recipe.GetFloorPrefab(), "Floor", parent, new Vector3(0f, -FloorThickness * 0.5f, 0f), Quaternion.identity, size);
        }

        private static void BuildOuterWalls(WarehouseLayoutData layout, Transform parent, WarehouseRecipe recipe)
        {
            float mapWidth = layout.Width * GetCellSizeX(layout);
            float mapHeight = layout.Height * GetCellSizeZ(layout);
            float halfWidth = mapWidth * 0.5f;
            float halfHeight = mapHeight * 0.5f;
            float wallY = WallHeight * 0.5f;
            GameObject wallPrefab = recipe.GetWallPrefab();

            InstantiatePrefab(wallPrefab, "NorthWall", parent, new Vector3(0f, wallY, halfHeight), Quaternion.identity, new Vector3(mapWidth, WallHeight, WallThickness));
            InstantiatePrefab(wallPrefab, "SouthWall", parent, new Vector3(0f, wallY, -halfHeight), Quaternion.identity, new Vector3(mapWidth, WallHeight, WallThickness));
            InstantiatePrefab(wallPrefab, "EastWall", parent, new Vector3(halfWidth, wallY, 0f), Quaternion.identity, new Vector3(WallThickness, WallHeight, mapHeight));
            InstantiatePrefab(wallPrefab, "WestWall", parent, new Vector3(-halfWidth, wallY, 0f), Quaternion.identity, new Vector3(WallThickness, WallHeight, mapHeight));
        }

        private static void BuildObjects(WarehouseLayoutData layout, Transform parent, WarehouseRecipe recipe)
        {
            for (int i = 0; i < layout.Objects.Count; i++)
            {
                WarehouseObjectPlacement obj = layout.Objects[i];
                GameObject prefab = recipe.GetPrefab(obj, layout);
                if (prefab == null)
                {
                    Debug.LogError($"Warehouse prefab for {obj.Kind} is missing.");
                    continue;
                }

                Vector3 position = recipe.GridToWorld(obj.Center);
                position.y = GetSurfaceHeight(layout, recipe, obj);
                InstantiatePrefab(prefab, $"{obj.Kind}_{obj.Side}", parent, position, GetVisualRotation(obj), GetObjectScale(obj));
            }
        }

        private static Quaternion GetVisualRotation(WarehouseObjectPlacement obj)
        {
            if (obj.IsContainer || obj.Kind == WarehouseObjectKind.Bridge)
                return Quaternion.identity;

            return Quaternion.Euler(0f, obj.RotationY, 0f);
        }

        private static float GetCellSizeX(WarehouseLayoutData layout)
        {
            return layout.CellSizeX > 0f ? layout.CellSizeX : layout.CellSize;
        }

        private static float GetCellSizeZ(WarehouseLayoutData layout)
        {
            return layout.CellSizeZ > 0f ? layout.CellSizeZ : layout.CellSize;
        }

        private static Vector3 GetObjectScale(WarehouseObjectPlacement obj)
        {
            return new Vector3(Mathf.Max(1, obj.Size.x), 1f, Mathf.Max(1, obj.Size.y));
        }

        private static float GetSurfaceHeight(WarehouseLayoutData layout, WarehouseRecipe recipe, WarehouseObjectPlacement obj)
        {
            if (obj.Kind == WarehouseObjectKind.Bridge)
                return recipe.LowContainerTopHeight;

            if (!obj.IsCover || obj.Surface != WarehousePlacementSurface.StructureTop)
                return 0f;

            WarehouseObjectPlacement structure = FindTopSurfaceAt(layout, obj.Origin);
            if (structure == null)
                return 0f;

            return structure.Kind == WarehouseObjectKind.Bridge
                ? recipe.BridgeTopHeight
                : recipe.LowContainerTopHeight;
        }

        private static WarehouseObjectPlacement FindTopSurfaceAt(WarehouseLayoutData layout, Vector2Int cell)
        {
            for (int i = 0; i < layout.Objects.Count; i++)
            {
                WarehouseObjectPlacement obj = layout.Objects[i];
                if (!obj.IsTopWalkableSource)
                    continue;

                RectInt rect = new(obj.Origin.x, obj.Origin.y, obj.Size.x, obj.Size.y);
                if (rect.Contains(cell))
                    return obj;
            }

            return null;
        }

        private static void BuildGameplay(
            WarehouseLayoutData layout,
            Transform parent,
            WarehouseRecipe recipe,
            LevelController level)
        {
            List<Transform> allSpawns = new();
            List<Transform> redSpawns = new();
            List<Transform> blueSpawns = new();

            Vector3 spawnMarkerScale = new(SpawnMarkerDiameter, MarkerThickness, SpawnMarkerDiameter);
            Transform spawnA = InstantiateMarker(recipe.GetSpawnMarkerPrefab(), "SpawnA", parent, recipe.GridToWorld(layout.SpawnA), Quaternion.identity, spawnMarkerScale);
            Transform spawnB = InstantiateMarker(recipe.GetSpawnMarkerPrefab(), "SpawnB", parent, recipe.GridToWorld(layout.SpawnB), Quaternion.Euler(0f, 180f, 0f), spawnMarkerScale);
            if (spawnA != null)
            {
                allSpawns.Add(spawnA);
                redSpawns.Add(spawnA);
            }

            if (spawnB != null)
            {
                allSpawns.Add(spawnB);
                blueSpawns.Add(spawnB);
            }

            Transform capture = InstantiateMarker(recipe.GetCapturePointMarkerPrefab(), "CapturePoint", parent, recipe.GridToWorld(layout.CapturePoint), Quaternion.identity, Vector3.one);

            if (level != null)
                level.ConfigureGeneratedLevel(allSpawns, redSpawns, blueSpawns, capture, recipe.CaptureClearRadius * recipe.GridCellSize);
        }

        private static Transform InstantiateMarker(
            GameObject prefab,
            string name,
            Transform parent,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale)
        {
            GameObject instance = InstantiatePrefab(prefab, name, parent, position, rotation, scale);
            return instance == null ? null : instance.transform;
        }

        private static GameObject InstantiatePrefab(
            GameObject prefab,
            string name,
            Transform parent,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale)
        {
            if (prefab == null)
            {
                Debug.LogError($"Warehouse prefab '{name}' is missing.");
                return null;
            }

            GameObject instance = Object.Instantiate(prefab, parent);
            instance.name = name;
            instance.transform.localPosition = position;
            instance.transform.localRotation = rotation;
            instance.transform.localScale = scale;
            return instance;
        }

        private static Transform CreateRoot(string name, Transform parent)
        {
            Transform root = new GameObject(name).transform;
            root.SetParent(parent, false);
            return root;
        }
    }
}
