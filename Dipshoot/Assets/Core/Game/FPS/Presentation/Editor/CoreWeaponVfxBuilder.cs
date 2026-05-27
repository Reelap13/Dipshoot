using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Players.Editor
{
    public static class CoreWeaponVfxBuilder
    {
        private const string RequestFile = "Temp/RebuildCoreWeaponVfx.request";
        private const string Root = "Assets/Core/Resources/Presentation/Weapons/Shared/CoreVfx";
        private const string MaterialRoot = Root + "/Materials";
        private const string PrefabRoot = Root + "/Prefabs";
        private const string MuzzleMaterialPath = MaterialRoot + "/M_Core_MuzzleFlash.mat";
        private const string ImpactMaterialPath = MaterialRoot + "/M_Core_Impact.mat";
        private const string MuzzlePrefabPath = PrefabRoot + "/Core_MuzzleFlash.prefab";
        private const string WorldImpactPrefabPath = PrefabRoot + "/Core_Impact_World.prefab";
        private const string PlayerImpactPrefabPath = PrefabRoot + "/Core_Impact_Player.prefab";
        private const string PrimaryVisualPath = "Assets/Core/Game/FPS/Weapons/Definitions/PrimaryWeaponVisual.asset";
        private const string PistolVisualPath = "Assets/Core/Game/FPS/Weapons/Definitions/PistolWeaponVisual.asset";

        [InitializeOnLoadMethod]
        private static void AutoBuild()
        {
            if (!File.Exists(RequestFile))
                return;

            File.Delete(RequestFile);
            Build();
        }

        [MenuItem("Dipshoot/Resources/Rebuild Core Weapon VFX")]
        public static void Build()
        {
            EnsureFolders();

            Material muzzle_material = CreateMaterial(MuzzleMaterialPath, new Color(1f, 0.72f, 0.18f, 1f));
            Material impact_material = CreateMaterial(ImpactMaterialPath, new Color(1f, 0.88f, 0.38f, 1f));

            GameObject muzzle_prefab = CreateMuzzleFlashPrefab(muzzle_material);
            GameObject world_impact_prefab = CreateImpactPrefab("Core_Impact_World", impact_material, new Color(1f, 0.82f, 0.42f, 1f));
            GameObject player_impact_prefab = CreateImpactPrefab("Core_Impact_Player", impact_material, new Color(1f, 0.18f, 0.08f, 1f));

            SavePrefab(muzzle_prefab, MuzzlePrefabPath);
            SavePrefab(world_impact_prefab, WorldImpactPrefabPath);
            SavePrefab(player_impact_prefab, PlayerImpactPrefabPath);

            Object.DestroyImmediate(muzzle_prefab);
            Object.DestroyImmediate(world_impact_prefab);
            Object.DestroyImmediate(player_impact_prefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            AssignVisual(PrimaryVisualPath);
            AssignVisual(PistolVisualPath);

            Debug.Log("[WeaponVFX] Core weapon VFX rebuilt.");
        }

        private static void EnsureFolders()
        {
            CreateFolder("Assets/Core/Resources/Presentation/Weapons/Shared", "CoreVfx");
            CreateFolder(Root, "Materials");
            CreateFolder(Root, "Prefabs");
        }

        private static void CreateFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name))
                AssetDatabase.CreateFolder(parent, name);
        }

        private static Material CreateMaterial(string path, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (shader == null)
                    shader = Shader.Find("Particles/Standard Unlit");
                if (shader == null)
                    shader = Shader.Find("Sprites/Default");

                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetColor("_BaseColor", color);
            material.SetColor("_Color", color);
            material.SetColor("_TintColor", color);
            material.renderQueue = 3000;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject CreateMuzzleFlashPrefab(Material material)
        {
            GameObject root = new("Core_MuzzleFlash");
            AddParticle(root, material, 10, 0.035f, 0.12f, 0.18f, new Color(1f, 0.7f, 0.16f, 1f), ParticleSystemRenderMode.Billboard);

            GameObject spark = new("Sparks");
            spark.transform.SetParent(root.transform, false);
            AddParticle(spark, material, 14, 0.08f, 0.04f, 0.05f, new Color(1f, 0.95f, 0.55f, 1f), ParticleSystemRenderMode.Stretch);

            Light light = root.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.58f, 0.18f);
            light.range = 2f;
            light.intensity = 3f;
            return root;
        }

        private static GameObject CreateImpactPrefab(string name, Material material, Color color)
        {
            GameObject root = new(name);
            AddParticle(root, material, 18, 0.18f, 0.035f, 0.08f, color, ParticleSystemRenderMode.Billboard);
            return root;
        }

        private static void AddParticle(
            GameObject target,
            Material material,
            int burst_count,
            float speed,
            float size,
            float lifetime,
            Color color,
            ParticleSystemRenderMode render_mode)
        {
            ParticleSystem particle = target.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particle.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = lifetime;
            main.startSpeed = speed;
            main.startSize = size;
            main.startColor = color;
            main.maxParticles = burst_count;

            ParticleSystem.EmissionModule emission = particle.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)burst_count) });

            ParticleSystem.ShapeModule shape = particle.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 18f;
            shape.radius = 0.015f;

            ParticleSystemRenderer renderer = particle.GetComponent<ParticleSystemRenderer>();
            renderer.material = material;
            renderer.renderMode = render_mode;
            renderer.alignment = ParticleSystemRenderSpace.View;
        }

        private static void SavePrefab(GameObject root, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }

        private static void AssignVisual(string path)
        {
            WeaponVisualDefinition visual = AssetDatabase.LoadAssetAtPath<WeaponVisualDefinition>(path);
            if (visual == null)
                return;

            SerializedObject serialized_object = new(visual);
            serialized_object.FindProperty("_muzzle_flash_prefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(MuzzlePrefabPath);
            serialized_object.FindProperty("_world_impact_prefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(WorldImpactPrefabPath);
            serialized_object.FindProperty("_player_impact_prefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerImpactPrefabPath);
            serialized_object.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(visual);
        }
    }
}
