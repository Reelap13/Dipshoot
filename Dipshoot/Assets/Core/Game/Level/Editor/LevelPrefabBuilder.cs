using System.Collections.Generic;
using Game.Level;
using Game.MatchMode;
using Mirror;
using UnityEditor;
using UnityEngine;

namespace Game.Level.Editor
{
    public static class LevelPrefabBuilder
    {
        private const string LevelPrefabPath = "Assets/Core/Game/Level/Level.prefab";
        private const string MaterialFolderPath = "Assets/Core/Game/Level/Materials";

        private const float FloorTop = -1f;
        private const float MapWidth = 34f;
        private const float MapLength = 28f;
        private const float WallHeight = 3f;
        private const float WallThickness = 0.6f;
        private const float CaptureRadius = 4f;
        private const uint LevelNetworkAssetId = 1339167122;

        private static Material _floor_material;
        private static Material _wall_material;
        private static Material _cover_material;
        private static Material _low_cover_material;
        private static Material _red_material;
        private static Material _blue_material;
        private static Material _capture_material;

        [MenuItem("Dipshoot/Level/Rebuild Aim Map")]
        public static void RebuildAimMap()
        {
            EnsureFolders();
            LoadMaterials();

            GameObject root = new("Level", typeof(NetworkIdentity), typeof(NetworkMatch), typeof(LevelController));
            Transform map_root = CreateContainer("Aim Map By Team Levinant", root.transform);
            Transform spawn_root = CreateContainer("SpawnPoints", root.transform);
            Transform capture_point = CreateContainer("CapturePoint", root.transform);
            capture_point.localPosition = Vector3.zero;

            List<Transform> all_spawn_points = new();
            List<Transform> red_spawn_points = new();
            List<Transform> blue_spawn_points = new();

            CreateFloor(map_root);
            CreateOuterWalls(map_root);
            CreateSpawnShelters(map_root);
            CreateSymmetricCover(map_root);
            CreateCaptureMarker(capture_point);
            CreateSpawnPoints(spawn_root, all_spawn_points, red_spawn_points, blue_spawn_points);
            CreateMapLight(map_root);
            SerializeNetworkIdentity(root);
            SerializeLevelReferences(root, all_spawn_points, red_spawn_points, blue_spawn_points, capture_point);

            PrefabUtility.SaveAsPrefabAsset(root, LevelPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder(MaterialFolderPath))
                AssetDatabase.CreateFolder("Assets/Core/Game/Level", "Materials");
        }

        private static void LoadMaterials()
        {
            _floor_material = GetOrCreateMaterial("AimFloor.mat", new Color(0.34f, 0.32f, 0.22f, 1f));
            _wall_material = GetOrCreateMaterial("AimWall.mat", new Color(0.43f, 0.43f, 0.4f, 1f));
            _cover_material = GetOrCreateMaterial("AimCover.mat", new Color(0.34f, 0.28f, 0.2f, 1f));
            _low_cover_material = GetOrCreateMaterial("AimLowCover.mat", new Color(0.43f, 0.38f, 0.31f, 1f));
            _red_material = GetOrCreateMaterial("AimRedAccent.mat", new Color(0.55f, 0.12f, 0.1f, 1f));
            _blue_material = GetOrCreateMaterial("AimBlueAccent.mat", new Color(0.1f, 0.22f, 0.55f, 1f));
            _capture_material = GetOrCreateMaterial("AimCapturePoint.mat", new Color(0.7f, 0.7f, 0.7f, 0.35f));
        }

        private static Material GetOrCreateMaterial(string file_name, Color color)
        {
            string path = $"{MaterialFolderPath}/{file_name}";
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

        private static Transform CreateContainer(string name, Transform parent)
        {
            GameObject target = new(name);
            target.transform.SetParent(parent, false);
            return target.transform;
        }

        private static void CreateFloor(Transform parent)
        {
            CreateBlock(
                "GrassConcreteFloor",
                parent,
                new Vector3(0f, -1.1f, 0f),
                new Vector3(MapWidth, 0.2f, MapLength),
                Vector3.zero,
                _floor_material);
        }

        private static void CreateOuterWalls(Transform parent)
        {
            float half_width = MapWidth * 0.5f;
            float half_length = MapLength * 0.5f;
            float wall_y = FloorTop + WallHeight * 0.5f;

            CreateBlock("NorthWall", parent, new Vector3(0f, wall_y, half_length), new Vector3(MapWidth, WallHeight, WallThickness), Vector3.zero, _wall_material);
            CreateBlock("SouthWall", parent, new Vector3(0f, wall_y, -half_length), new Vector3(MapWidth, WallHeight, WallThickness), Vector3.zero, _wall_material);
            CreateBlock("EastWall", parent, new Vector3(half_width, wall_y, 0f), new Vector3(WallThickness, WallHeight, MapLength), Vector3.zero, _wall_material);
            CreateBlock("WestWall", parent, new Vector3(-half_width, wall_y, 0f), new Vector3(WallThickness, WallHeight, MapLength), Vector3.zero, _wall_material);
        }

        private static void CreateSpawnShelters(Transform parent)
        {
            CreateBlock("RedSpawnWall", parent, new Vector3(0f, 0.2f, -10.2f), new Vector3(12f, 2.4f, 0.65f), Vector3.zero, _wall_material);
            CreateBlock("BlueSpawnWall", parent, new Vector3(0f, 0.2f, 10.2f), new Vector3(12f, 2.4f, 0.65f), Vector3.zero, _wall_material);
            CreateBlock("RedSpawnStripe", parent, new Vector3(0f, 1.45f, -9.85f), new Vector3(12f, 0.08f, 0.08f), Vector3.zero, _red_material);
            CreateBlock("BlueSpawnStripe", parent, new Vector3(0f, 1.45f, 9.85f), new Vector3(12f, 0.08f, 0.08f), Vector3.zero, _blue_material);

            CreateBlock("RedLeftReturn", parent, new Vector3(-7.3f, 0f, -11.4f), new Vector3(0.65f, 2f, 3.2f), Vector3.zero, _wall_material);
            CreateBlock("RedRightReturn", parent, new Vector3(7.3f, 0f, -11.4f), new Vector3(0.65f, 2f, 3.2f), Vector3.zero, _wall_material);
            CreateBlock("BlueLeftReturn", parent, new Vector3(-7.3f, 0f, 11.4f), new Vector3(0.65f, 2f, 3.2f), Vector3.zero, _wall_material);
            CreateBlock("BlueRightReturn", parent, new Vector3(7.3f, 0f, 11.4f), new Vector3(0.65f, 2f, 3.2f), Vector3.zero, _wall_material);
        }

        private static void CreateSymmetricCover(Transform parent)
        {
            CreateMirroredBlock("MidLongCover", parent, new Vector3(0f, -0.25f, 4.6f), new Vector3(7.5f, 1.5f, 0.8f), Vector3.zero, _cover_material);
            CreateMirroredBlock("LeftLaneCover", parent, new Vector3(-8.3f, -0.25f, 4.1f), new Vector3(5.2f, 1.5f, 0.8f), Vector3.zero, _cover_material);
            CreateMirroredBlock("RightLaneCover", parent, new Vector3(8.3f, -0.25f, 4.1f), new Vector3(5.2f, 1.5f, 0.8f), Vector3.zero, _cover_material);
            CreateMirroredBlock("LeftCrate", parent, new Vector3(-4.2f, -0.2f, 6.9f), new Vector3(1.7f, 1.6f, 1.7f), Vector3.zero, _cover_material);
            CreateMirroredBlock("RightCrate", parent, new Vector3(4.2f, -0.2f, 6.9f), new Vector3(1.7f, 1.6f, 1.7f), Vector3.zero, _cover_material);
            CreateMirroredBlock("OuterLowWallLeft", parent, new Vector3(-12.2f, -0.55f, 1.8f), new Vector3(0.8f, 0.9f, 6f), Vector3.zero, _low_cover_material);
            CreateMirroredBlock("OuterLowWallRight", parent, new Vector3(12.2f, -0.55f, 1.8f), new Vector3(0.8f, 0.9f, 6f), Vector3.zero, _low_cover_material);
            CreateMirroredBlock("RampLeft", parent, new Vector3(-6f, -0.7f, 2.2f), new Vector3(2.4f, 0.3f, 3.2f), new Vector3(18f, 0f, 0f), _low_cover_material);
            CreateMirroredBlock("RampRight", parent, new Vector3(6f, -0.7f, 2.2f), new Vector3(2.4f, 0.3f, 3.2f), new Vector3(18f, 0f, 0f), _low_cover_material);

            CreateBlock("CenterLeftBox", parent, new Vector3(-3.2f, -0.2f, 0f), new Vector3(1.8f, 1.6f, 1.8f), Vector3.zero, _cover_material);
            CreateBlock("CenterRightBox", parent, new Vector3(3.2f, -0.2f, 0f), new Vector3(1.8f, 1.6f, 1.8f), Vector3.zero, _cover_material);
            CreateBlock("CenterBackLowCover", parent, new Vector3(0f, -0.55f, -2.7f), new Vector3(4.4f, 0.9f, 0.65f), Vector3.zero, _low_cover_material);
            CreateBlock("CenterFrontLowCover", parent, new Vector3(0f, -0.55f, 2.7f), new Vector3(4.4f, 0.9f, 0.65f), Vector3.zero, _low_cover_material);
        }

        private static void CreateMirroredBlock(
            string name,
            Transform parent,
            Vector3 positive_z_position,
            Vector3 scale,
            Vector3 euler_angles,
            Material material)
        {
            CreateBlock($"{name}BlueSide", parent, positive_z_position, scale, euler_angles, material);
            Vector3 mirrored_position = positive_z_position;
            mirrored_position.z = -mirrored_position.z;
            Vector3 mirrored_euler = euler_angles;
            mirrored_euler.x = -mirrored_euler.x;
            CreateBlock($"{name}RedSide", parent, mirrored_position, scale, mirrored_euler, material);
        }

        private static void CreateCaptureMarker(Transform capture_point)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "CapturePointMarker";
            marker.transform.SetParent(capture_point, false);
            marker.transform.localPosition = new Vector3(0f, FloorTop + 0.025f, 0f);
            marker.transform.localScale = new Vector3(CaptureRadius * 2f, 0.02f, CaptureRadius * 2f);

            UnityEngine.Object.DestroyImmediate(marker.GetComponent<Collider>());
            marker.GetComponent<Renderer>().sharedMaterial = _capture_material;
        }

        private static void CreateSpawnPoints(
            Transform parent,
            List<Transform> all_spawn_points,
            List<Transform> red_spawn_points,
            List<Transform> blue_spawn_points)
        {
            CreateTeamSpawnPoint("RedSpawnLeft", parent, TeamId.Red, new Vector3(-5f, 0f, -12.3f), Quaternion.identity, all_spawn_points, red_spawn_points, blue_spawn_points);
            CreateTeamSpawnPoint("RedSpawnCenter", parent, TeamId.Red, new Vector3(0f, 0f, -12.3f), Quaternion.identity, all_spawn_points, red_spawn_points, blue_spawn_points);
            CreateTeamSpawnPoint("RedSpawnRight", parent, TeamId.Red, new Vector3(5f, 0f, -12.3f), Quaternion.identity, all_spawn_points, red_spawn_points, blue_spawn_points);

            Quaternion blue_rotation = Quaternion.Euler(0f, 180f, 0f);
            CreateTeamSpawnPoint("BlueSpawnLeft", parent, TeamId.Blue, new Vector3(-5f, 0f, 12.3f), blue_rotation, all_spawn_points, red_spawn_points, blue_spawn_points);
            CreateTeamSpawnPoint("BlueSpawnCenter", parent, TeamId.Blue, new Vector3(0f, 0f, 12.3f), blue_rotation, all_spawn_points, red_spawn_points, blue_spawn_points);
            CreateTeamSpawnPoint("BlueSpawnRight", parent, TeamId.Blue, new Vector3(5f, 0f, 12.3f), blue_rotation, all_spawn_points, red_spawn_points, blue_spawn_points);
        }

        private static void CreateTeamSpawnPoint(
            string name,
            Transform parent,
            TeamId team_id,
            Vector3 position,
            Quaternion rotation,
            List<Transform> all_spawn_points,
            List<Transform> red_spawn_points,
            List<Transform> blue_spawn_points)
        {
            Transform spawn_point = CreateContainer(name, parent);
            spawn_point.localPosition = position;
            spawn_point.localRotation = rotation;

            all_spawn_points.Add(spawn_point);
            if (team_id == TeamId.Red)
                red_spawn_points.Add(spawn_point);
            else if (team_id == TeamId.Blue)
                blue_spawn_points.Add(spawn_point);
        }

        private static void CreateMapLight(Transform parent)
        {
            GameObject light_object = new("MapDirectionalLight");
            light_object.transform.SetParent(parent, false);
            light_object.transform.localRotation = Quaternion.Euler(50f, -35f, 0f);

            Light light = light_object.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.25f;
            light.color = new Color(1f, 0.95f, 0.86f, 1f);
        }

        private static void CreateBlock(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Vector3 euler_angles,
            Material material)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, false);
            block.transform.localPosition = position;
            block.transform.localRotation = Quaternion.Euler(euler_angles);
            block.transform.localScale = scale;
            block.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void SerializeLevelReferences(
            GameObject root,
            List<Transform> all_spawn_points,
            List<Transform> red_spawn_points,
            List<Transform> blue_spawn_points,
            Transform capture_point)
        {
            SerializedObject serialized_object = new(root.GetComponent<LevelController>());
            SetTransformList(serialized_object, "_spawn_points", all_spawn_points);
            SetTransformList(serialized_object, "_red_spawn_points", red_spawn_points);
            SetTransformList(serialized_object, "_blue_spawn_points", blue_spawn_points);
            serialized_object.FindProperty("_capture_point").objectReferenceValue = capture_point;
            serialized_object.FindProperty("_capture_marker_radius").floatValue = CaptureRadius;
            serialized_object.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SerializeNetworkIdentity(GameObject root)
        {
            SerializedObject serialized_object = new(root.GetComponent<NetworkIdentity>());
            SerializedProperty asset_id = serialized_object.FindProperty("_assetId");
            if (asset_id != null)
                asset_id.uintValue = LevelNetworkAssetId;

            serialized_object.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetTransformList(
            SerializedObject serialized_object,
            string property_name,
            List<Transform> transforms)
        {
            SerializedProperty property = serialized_object.FindProperty(property_name);
            property.arraySize = transforms.Count;
            for (int i = 0; i < transforms.Count; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = transforms[i];
        }
    }
}
