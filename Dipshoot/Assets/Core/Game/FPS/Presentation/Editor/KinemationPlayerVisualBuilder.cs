using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Players.Editor
{
    public static class KinemationPlayerVisualBuilder
    {
        private const string RequestFile = "Temp/RebuildKinemationPlayerVisual.request";
        private const string Root = "Assets/Core/Resources/Presentation/Kinemation";
        private const string SwatModelPath = Root + "/Characters/Swat/Swat.fbx";
        private const string SwatMaterialRoot = Root + "/Characters/Swat/Materials";
        private const string SwatPrefabRoot = Root + "/Characters/Swat/Prefabs";
        private const string SwatDefaultPrefabPath = SwatPrefabRoot + "/Swat_ThirdPerson.prefab";
        private const string SwatBluePrefabPath = SwatPrefabRoot + "/Swat_ThirdPerson_Blue.prefab";
        private const string SwatRedPrefabPath = SwatPrefabRoot + "/Swat_ThirdPerson_Red.prefab";
        private const string SwatBlueBodyMaterialPath = SwatMaterialRoot + "/Soldier_body1_Blue.mat";
        private const string SwatBlueHeadMaterialPath = SwatMaterialRoot + "/Soldier_head6_Blue.mat";
        private const string SwatRedBodyMaterialPath = SwatMaterialRoot + "/Soldier_body1_Red.mat";
        private const string SwatRedHeadMaterialPath = SwatMaterialRoot + "/Soldier_head6_Red.mat";
        private const string SwatHitboxProfilePath = Root + "/Characters/Swat/SwatHitboxRigProfile.asset";
        private const string HumanoidAnimatorPath = Root + "/Animations/Locomotion/FPSAnimator_Humanoid.controller";
        private const string PlayerVisualDefinitionPath = "Assets/Core/Resources/Presentation/Characters/Humanoid/PlayerVisualDefinition.asset";

        [InitializeOnLoadMethod]
        private static void AutoBuild()
        {
            if (!File.Exists(RequestFile))
                return;

            File.Delete(RequestFile);
            Build();
        }

        [MenuItem("Dipshoot/Resources/Rebuild Kinemation Player Visual")]
        public static void Build()
        {
            EnsureFolder(SwatPrefabRoot);

            RuntimeAnimatorController animator = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(HumanoidAnimatorPath);
            BuildPrefab(SwatDefaultPrefabPath, null, null, animator);
            BuildPrefab(
                SwatBluePrefabPath,
                CreateMaterialVariant("Soldier_body1", SwatBlueBodyMaterialPath, new Color(0.45f, 0.62f, 1f, 1f)),
                CreateMaterialVariant("Soldier_head6", SwatBlueHeadMaterialPath, Color.white),
                animator);
            BuildPrefab(
                SwatRedPrefabPath,
                CreateMaterialVariant("Soldier_body1", SwatRedBodyMaterialPath, new Color(1f, 0.45f, 0.42f, 1f)),
                CreateMaterialVariant("Soldier_head6", SwatRedHeadMaterialPath, Color.white),
                animator);
            PlayerHitboxRigProfile hitbox_profile = BuildHitboxProfile();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            GameObject default_prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SwatDefaultPrefabPath);
            GameObject blue_prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SwatBluePrefabPath);
            GameObject red_prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SwatRedPrefabPath);
            AssignPlayerVisualDefinition(default_prefab, blue_prefab, red_prefab, animator, hitbox_profile);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Kinemation] SWAT player visual rebuilt.");
        }

        private static void BuildPrefab(
            string prefab_path,
            Material body_material,
            Material head_material,
            RuntimeAnimatorController animator_controller)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(SwatModelPath);
            if (source == null)
            {
                Debug.LogError($"[Kinemation] Missing SWAT model: {SwatModelPath}");
                return;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            instance.name = Path.GetFileNameWithoutExtension(prefab_path);
            ConfigureAnimator(instance, animator_controller);
            ApplyMaterials(instance, body_material, head_material);
            EnsureFolderForAsset(prefab_path);
            PrefabUtility.SaveAsPrefabAsset(instance, prefab_path);
            Object.DestroyImmediate(instance);
        }

        private static void ConfigureAnimator(GameObject target, RuntimeAnimatorController animator_controller)
        {
            Animator animator = target.GetComponentInChildren<Animator>(true);
            if (animator == null)
                animator = target.AddComponent<Animator>();

            animator.runtimeAnimatorController = animator_controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        private static void ApplyMaterials(GameObject target, Material body_material, Material head_material)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Material[] materials = renderers[i].sharedMaterials;
                for (int j = 0; j < materials.Length; j++)
                {
                    if (materials[j] == null)
                        continue;

                    if (body_material != null && materials[j].name.Contains("body", System.StringComparison.OrdinalIgnoreCase))
                        materials[j] = body_material;
                    else if (head_material != null && materials[j].name.Contains("head", System.StringComparison.OrdinalIgnoreCase))
                        materials[j] = head_material;
                }

                renderers[i].sharedMaterials = materials;
            }
        }

        private static Material CreateMaterialVariant(string source_name, string path, Color tint)
        {
            Material source = FindMaterial(source_name);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = source == null ? new Material(Shader.Find("Standard")) : new Material(source);
                AssetDatabase.CreateAsset(material, path);
            }

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", tint);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", tint);

            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material FindMaterial(string name)
        {
            string[] guids = AssetDatabase.FindAssets($"{name} t:Material", new[] { SwatMaterialRoot });
            if (guids.Length == 0)
                return null;

            return AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        private static PlayerHitboxRigProfile BuildHitboxProfile()
        {
            PlayerHitboxRigProfile profile = AssetDatabase.LoadAssetAtPath<PlayerHitboxRigProfile>(SwatHitboxProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<PlayerHitboxRigProfile>();
                AssetDatabase.CreateAsset(profile, SwatHitboxProfilePath);
            }

            SerializedObject serialized_object = new(profile);
            SerializedProperty bindings = serialized_object.FindProperty("_bindings");
            bindings.arraySize = 11;

            SetSphere(bindings.GetArrayElementAtIndex(0), "Head", "mixamorig:Head", PlayerHitboxType.Head, 2f, new Vector3(0f, 0.06f, 0f), 0.22f);
            SetCapsule(bindings.GetArrayElementAtIndex(1), "Chest", "mixamorig:Spine1", "mixamorig:Neck", PlayerHitboxType.Chest, 1f, 0.28f, 0.04f);
            SetCapsule(bindings.GetArrayElementAtIndex(2), "Pelvis", "mixamorig:Hips", "mixamorig:Spine", PlayerHitboxType.Pelvis, 0.9f, 0.26f, 0.02f);
            SetCapsule(bindings.GetArrayElementAtIndex(3), "LeftUpperArm", "mixamorig:LeftArm", "mixamorig:LeftForeArm", PlayerHitboxType.Arm, 0.75f, 0.105f, 0.01f);
            SetCapsule(bindings.GetArrayElementAtIndex(4), "LeftLowerArm", "mixamorig:LeftForeArm", "mixamorig:LeftHand", PlayerHitboxType.Arm, 0.75f, 0.09f, 0.01f);
            SetCapsule(bindings.GetArrayElementAtIndex(5), "RightUpperArm", "mixamorig:RightArm", "mixamorig:RightForeArm", PlayerHitboxType.Arm, 0.75f, 0.105f, 0.01f);
            SetCapsule(bindings.GetArrayElementAtIndex(6), "RightLowerArm", "mixamorig:RightForeArm", "mixamorig:RightHand", PlayerHitboxType.Arm, 0.75f, 0.09f, 0.01f);
            SetCapsule(bindings.GetArrayElementAtIndex(7), "LeftUpperLeg", "mixamorig:LeftUpLeg", "mixamorig:LeftLeg", PlayerHitboxType.Leg, 0.75f, 0.13f, 0.02f);
            SetCapsule(bindings.GetArrayElementAtIndex(8), "LeftLowerLeg", "mixamorig:LeftLeg", "mixamorig:LeftFoot", PlayerHitboxType.Leg, 0.75f, 0.105f, 0.02f);
            SetCapsule(bindings.GetArrayElementAtIndex(9), "RightUpperLeg", "mixamorig:RightUpLeg", "mixamorig:RightLeg", PlayerHitboxType.Leg, 0.75f, 0.13f, 0.02f);
            SetCapsule(bindings.GetArrayElementAtIndex(10), "RightLowerLeg", "mixamorig:RightLeg", "mixamorig:RightFoot", PlayerHitboxType.Leg, 0.75f, 0.105f, 0.02f);

            serialized_object.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static void SetSphere(
            SerializedProperty property,
            string name,
            string bone_name,
            PlayerHitboxType type,
            float multiplier,
            Vector3 local_position,
            float radius)
        {
            SetCommon(property, name, bone_name, null, type, multiplier, PlayerHitboxRigProfile.HitboxShape.Sphere);
            property.FindPropertyRelative("LocalPosition").vector3Value = local_position;
            property.FindPropertyRelative("Size").vector3Value = Vector3.one * radius * 2f;
            property.FindPropertyRelative("Radius").floatValue = radius;
            property.FindPropertyRelative("LengthPadding").floatValue = 0f;
        }

        private static void SetCapsule(
            SerializedProperty property,
            string name,
            string start_bone,
            string end_bone,
            PlayerHitboxType type,
            float multiplier,
            float radius,
            float padding)
        {
            SetCommon(property, name, start_bone, end_bone, type, multiplier, PlayerHitboxRigProfile.HitboxShape.Capsule);
            property.FindPropertyRelative("LocalPosition").vector3Value = Vector3.zero;
            property.FindPropertyRelative("Size").vector3Value = Vector3.one * radius * 2f;
            property.FindPropertyRelative("Radius").floatValue = radius;
            property.FindPropertyRelative("LengthPadding").floatValue = padding;
        }

        private static void SetCommon(
            SerializedProperty property,
            string name,
            string start_bone,
            string end_bone,
            PlayerHitboxType type,
            float multiplier,
            PlayerHitboxRigProfile.HitboxShape shape)
        {
            property.FindPropertyRelative("Name").stringValue = name;
            property.FindPropertyRelative("StartBoneName").stringValue = start_bone;
            property.FindPropertyRelative("EndBoneName").stringValue = end_bone;
            property.FindPropertyRelative("Type").enumValueIndex = (int)type;
            property.FindPropertyRelative("DamageMultiplier").floatValue = multiplier;
            property.FindPropertyRelative("Shape").enumValueIndex = (int)shape;
        }

        private static void AssignPlayerVisualDefinition(
            GameObject default_prefab,
            GameObject blue_prefab,
            GameObject red_prefab,
            RuntimeAnimatorController animator,
            PlayerHitboxRigProfile hitbox_profile)
        {
            PlayerVisualDefinition definition = AssetDatabase.LoadAssetAtPath<PlayerVisualDefinition>(PlayerVisualDefinitionPath);
            if (definition == null)
            {
                Debug.LogError($"[Kinemation] Missing player visual definition: {PlayerVisualDefinitionPath}");
                return;
            }

            SerializedObject serialized_object = new(definition);
            serialized_object.FindProperty("_third_person_character_prefab").objectReferenceValue = default_prefab;
            serialized_object.FindProperty("_blue_team_third_person_character_prefab").objectReferenceValue = blue_prefab;
            serialized_object.FindProperty("_red_team_third_person_character_prefab").objectReferenceValue = red_prefab;
            serialized_object.FindProperty("_third_person_animator_controller").objectReferenceValue = animator;
            serialized_object.FindProperty("_hitbox_rig_profile").objectReferenceValue = hitbox_profile;
            serialized_object.FindProperty("_third_person_local_position").vector3Value = Vector3.zero;
            serialized_object.FindProperty("_third_person_local_euler_angles").vector3Value = Vector3.zero;
            serialized_object.FindProperty("_third_person_local_scale").vector3Value = Vector3.one;
            serialized_object.FindProperty("_third_person_weapon_socket_name").stringValue = "mixamorig:RightHand";
            serialized_object.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
        }

        private static void EnsureFolderForAsset(string path)
        {
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
