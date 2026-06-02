using UnityEditor;
using UnityEngine;

namespace Game.ProcGen.Warehouse.Editor
{
    public static class WarehousePrefabBuilder
    {
        private const string PrefabFolder = "Assets/Core/Resources/Presentation/Warehouse";
        private const string MaterialFolder = "Assets/Core/Resources/Presentation/Warehouse/Materials";
        private const float CellSizeX = 2.6f;
        private const float CellSizeZ = 3.8f;
        private const float ContainerLowHeight = 2.7f;
        private const float ContainerHighHeight = 5.4f;
        private const float BridgeHeight = 0.25f;
        private const float FullCoverHeight = 1.55f;
        private const float PartialCoverHeight = 1f;

        [MenuItem("Dipshoot/ProcGen/Rebuild Warehouse Prefabs")]
        public static void BuildAll()
        {
            EnsureFolders();

            Material containerLow = GetOrCreateMaterial("WarehouseContainerLow.mat", new Color(0.28f, 0.34f, 0.37f));
            Material containerHigh = GetOrCreateMaterial("WarehouseContainerHigh.mat", new Color(0.22f, 0.29f, 0.33f));
            Material bridge = GetOrCreateMaterial("WarehouseBridge.mat", new Color(0.36f, 0.39f, 0.36f));
            Material ladder = GetOrCreateMaterial("WarehouseLadder.mat", new Color(0.62f, 0.52f, 0.32f));
            Material partialCover = GetOrCreateMaterial("WarehousePartialCover.mat", new Color(0.48f, 0.42f, 0.32f));
            Material fullCover = GetOrCreateMaterial("WarehouseFullCover.mat", new Color(0.42f, 0.34f, 0.23f));
            Material floor = GetOrCreateMaterial("WarehouseFloor.mat", new Color(0.32f, 0.32f, 0.29f));
            Material wall = GetOrCreateMaterial("WarehouseWall.mat", new Color(0.55f, 0.56f, 0.56f));
            Material spawn = GetOrCreateMaterial("WarehouseSpawnMarker.mat", new Color(0.1f, 0.35f, 0.85f));
            Material capture = GetOrCreateMaterial("WarehouseCaptureMarker.mat", new Color(0.78f, 0.78f, 0.78f, 0.45f));

            SaveSizedCubePrefab("ContainerLow", new Vector3(CellSizeX, ContainerLowHeight, CellSizeZ), containerLow, true);
            SaveSizedCubePrefab("ContainerHigh", new Vector3(CellSizeX, ContainerHighHeight, CellSizeZ), containerHigh, true);
            SaveSizedCubePrefab("Bridge", new Vector3(CellSizeX, BridgeHeight, CellSizeZ), bridge, true);
            SaveSizedCubePrefab("Ladder", new Vector3(CellSizeX * 0.28f, ContainerLowHeight, 0.12f), ladder, false);
            SaveSizedCubePrefab("PartialCover", new Vector3(CellSizeX * 0.86f, PartialCoverHeight, CellSizeZ * 0.28f), partialCover, true);
            SaveSizedCubePrefab("FullCover", new Vector3(CellSizeX * 0.86f, FullCoverHeight, CellSizeZ * 0.28f), fullCover, true);
            SaveCubePrefab("WarehouseFloor", floor, true);
            SaveCubePrefab("WarehouseWall", wall, true);
            SaveSpawnMarker(spawn);
            SaveCaptureMarker(capture);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void EnsureFolders()
        {
            CreateFolder("Assets/Core", "Resources");
            CreateFolder("Assets/Core/Resources", "Presentation");
            CreateFolder("Assets/Core/Resources/Presentation", "Warehouse");
            CreateFolder(PrefabFolder, "Materials");
        }

        private static void SaveCubePrefab(string name, Material material, bool keepCollider)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.GetComponent<Renderer>().sharedMaterial = material;
            if (!keepCollider && obj.TryGetComponent(out Collider collider))
                Object.DestroyImmediate(collider);

            SavePrefab(obj, name);
        }

        private static void SaveSizedCubePrefab(string name, Vector3 size, Material material, bool keepCollider)
        {
            GameObject root = new(name);
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "Visual";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = new Vector3(0f, size.y * 0.5f, 0f);
            visual.transform.localScale = size;
            visual.GetComponent<Renderer>().sharedMaterial = material;

            if (!keepCollider && visual.TryGetComponent(out Collider collider))
                Object.DestroyImmediate(collider);

            SavePrefab(root, name);
        }

        private static void SaveSpawnMarker(Material material)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obj.name = "SpawnMarker";
            obj.transform.localScale = new Vector3(0.6f, 0.04f, 0.6f);
            obj.GetComponent<Renderer>().sharedMaterial = material;
            if (obj.TryGetComponent(out Collider collider))
                Object.DestroyImmediate(collider);

            SavePrefab(obj, obj.name);
        }

        private static void SaveCaptureMarker(Material material)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obj.name = "CapturePointMarker";
            obj.transform.localScale = new Vector3(1f, 0.04f, 1f);
            obj.GetComponent<Renderer>().sharedMaterial = material;
            if (obj.TryGetComponent(out Collider collider))
                Object.DestroyImmediate(collider);

            SavePrefab(obj, obj.name);
        }

        private static void SavePrefab(GameObject obj, string name)
        {
            PrefabUtility.SaveAsPrefabAsset(obj, $"{PrefabFolder}/{name}.prefab");
            Object.DestroyImmediate(obj);
        }

        private static Material GetOrCreateMaterial(string name, Color color)
        {
            string path = $"{MaterialFolder}/{name}";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                    shader = Shader.Find("Standard");

                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void CreateFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{name}"))
                AssetDatabase.CreateFolder(parent, name);
        }
    }
}
