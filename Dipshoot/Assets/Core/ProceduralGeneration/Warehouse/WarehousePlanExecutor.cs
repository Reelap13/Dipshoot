using System.Collections.Generic;
using Game.Level;
using Game.MatchMode;
using UnityEngine;

namespace Game.ProcGen.Warehouse
{
    public static class WarehousePlanExecutor
    {
        private const float FloorThickness = 0.2f;
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
            BuildDecorations(plan.Layout, mapRoot);
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
            GameObject wallPrefab = recipe.GetWallPrefab();
            GameObject topPrefab = recipe.GetWallTopPrefab();
            float levelHeight = Mathf.Max(0.1f, recipe.ContainerLowHeight);
            Transform wallsRoot = CreateRoot("Walls", parent);

            for (int x = 0; x < layout.Width; x++)
            {
                float worldX = (x + 0.5f - layout.Width * 0.5f) * GetCellSizeX(layout);
                BuildWallStack(wallPrefab, topPrefab, wallsRoot, "NorthWall", new Vector3(worldX, 0f, halfHeight), Quaternion.Euler(0f, 180f, 0f), levelHeight);
                BuildWallStack(wallPrefab, topPrefab, wallsRoot, "SouthWall", new Vector3(worldX, 0f, -halfHeight), Quaternion.identity, levelHeight);
            }

            for (int y = 0; y < layout.Height; y++)
            {
                float worldZ = (y + 0.5f - layout.Height * 0.5f) * GetCellSizeZ(layout);
                BuildWallStack(wallPrefab, topPrefab, wallsRoot, "EastWall", new Vector3(halfWidth, 0f, worldZ), Quaternion.Euler(0f, 270f, 0f), levelHeight);
                BuildWallStack(wallPrefab, topPrefab, wallsRoot, "WestWall", new Vector3(-halfWidth, 0f, worldZ), Quaternion.Euler(0f, 90f, 0f), levelHeight);
            }
        }

        private static void BuildWallStack(
            GameObject wallPrefab,
            GameObject topPrefab,
            Transform parent,
            string name,
            Vector3 basePosition,
            Quaternion rotation,
            float levelHeight)
        {
            InstantiateWallLevel(wallPrefab, name, parent, basePosition, rotation, levelHeight, 0);
            InstantiateWallLevel(wallPrefab, name, parent, basePosition, rotation, levelHeight, 1);
            InstantiateWallLevel(topPrefab, $"{name}Top", parent, basePosition, rotation, levelHeight, 2);
        }

        private static void InstantiateWallLevel(
            GameObject prefab,
            string name,
            Transform parent,
            Vector3 basePosition,
            Quaternion rotation,
            float levelHeight,
            int level)
        {
            Vector3 position = basePosition;
            position.y = levelHeight * (level + 0.5f);
            InstantiatePrefab(prefab, $"{name}_{level}", parent, position, rotation, Vector3.one);
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
                GameObject instance = InstantiatePrefab(prefab, $"{obj.Kind}_{obj.Side}", parent, position, GetVisualRotation(obj), GetObjectScale(obj));
                ApplyContainerPalette(instance, obj, recipe);
            }
        }

        private static void ApplyContainerPalette(GameObject instance, WarehouseObjectPlacement obj, WarehouseRecipe recipe)
        {
            if (instance == null || !obj.IsContainer || recipe.ContainerPalette == null || obj.PaletteIndex < 0)
                return;

            Material material = recipe.ContainerPalette.GetMaterial(obj.PaletteIndex);
            if (material == null)
                return;

            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Material[] materials = renderers[i].sharedMaterials;
                for (int j = 0; j < materials.Length; j++)
                    materials[j] = material;

                renderers[i].sharedMaterials = materials;
            }
        }

        private static Quaternion GetVisualRotation(WarehouseObjectPlacement obj)
        {
            if (obj.IsContainer)
                return Quaternion.identity;

            return Quaternion.Euler(0f, obj.RotationY, 0f);
        }

        private static void BuildDecorations(WarehouseLayoutData layout, Transform parent)
        {
            for (int i = 0; i < layout.Decorations.Count; i++)
            {
                WarehouseDecorationPlacement decoration = layout.Decorations[i];
                if (decoration.Prefab == null)
                    continue;

                Vector3 position = GridToWorld(layout, new Vector2(decoration.Cell.x + 0.5f, decoration.Cell.y + 0.5f));
                position.x += decoration.Offset.x * GetCellSizeX(layout);
                position.z += decoration.Offset.y * GetCellSizeZ(layout);
                InstantiatePrefab(
                    decoration.Prefab,
                    $"Decoration_{decoration.Kind}",
                    parent,
                    position,
                    Quaternion.Euler(0f, decoration.RotationY, 0f),
                    Vector3.one * Mathf.Max(0.01f, decoration.Scale));
            }
        }

        private static Vector3 GridToWorld(WarehouseLayoutData layout, Vector2 gridPosition)
        {
            return new Vector3(
                (gridPosition.x - layout.Width * 0.5f) * GetCellSizeX(layout),
                0f,
                (gridPosition.y - layout.Height * 0.5f) * GetCellSizeZ(layout));
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
