using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Players.Editor
{
    public static class PlayerVisualResourceBuilder
    {
        private const string AutoBuildRequestFile = "Temp/RebuildPlayerVisualResources.request";
        private const string NeoFpsSourceRoot = "Assets/Resources/NeoFPS";
        private const string PresentationRoot = "Assets/Core/Resources/Presentation";
        private const string DependencyRoot = PresentationRoot + "/NeoFPSDependencies";
        private const string CharacterRoot = PresentationRoot + "/Characters/Humanoid";
        private const string WeaponRoot = PresentationRoot + "/Weapons";

        private const string FirstPersonArmsModelPath = CharacterRoot + "/Models/Character_FirstPerson_Arms.FBX";
        private const string ThirdPersonCharacterModelPath = CharacterRoot + "/Models/Character_ThirdPerson.fbx";
        private const string FirstPersonArmsPrefabPath = CharacterRoot + "/Prefabs/FirstPersonArms.prefab";
        private const string ThirdPersonCharacterPrefabPath = CharacterRoot + "/Prefabs/ThirdPersonCharacter.prefab";
        private const string PlayerVisualDefinitionPath = CharacterRoot + "/PlayerVisualDefinition.asset";

        private const string PrimaryFirstPersonModelPath = WeaponRoot + "/Primary/AssaultRifle/Models/Weapon_FP_AssaultRifle.FBX";
        private const string PrimaryThirdPersonModelPath = WeaponRoot + "/Primary/AssaultRifle/Models/Weapon_Low_AssaultRifle.FBX";
        private const string PrimaryFirstPersonAnimatorPath = WeaponRoot + "/Primary/AssaultRifle/Animation/AnimCtrl_FP_AssaultRifle.controller";
        private const string PrimaryMuzzleSourcePath = WeaponRoot + "/Primary/AssaultRifle/Effects/RealisticMuzzleFlash_AssaultRifle_Source.prefab";
        private const string PrimaryFirstPersonPrefabPath = WeaponRoot + "/Primary/AssaultRifle/Prefabs/FP_AssaultRifle.prefab";
        private const string PrimaryThirdPersonPrefabPath = WeaponRoot + "/Primary/AssaultRifle/Prefabs/TP_AssaultRifle.prefab";
        private const string PrimaryMuzzlePrefabPath = WeaponRoot + "/Primary/AssaultRifle/Prefabs/MuzzleFlash_AssaultRifle.prefab";

        private const string PistolFirstPersonModelPath = WeaponRoot + "/Secondary/Pistol/Models/Weapon_FP_Pistol.FBX";
        private const string PistolThirdPersonModelPath = WeaponRoot + "/Secondary/Pistol/Models/Weapon_Low_Pistol.FBX";
        private const string PistolFirstPersonAnimatorPath = WeaponRoot + "/Secondary/Pistol/Animation/AnimCtrl_FP_Pistol.controller";
        private const string PistolMuzzleSourcePath = WeaponRoot + "/Secondary/Pistol/Effects/RealisticMuzzleFlash_Pistol_Source.prefab";
        private const string PistolFirstPersonPrefabPath = WeaponRoot + "/Secondary/Pistol/Prefabs/FP_Pistol.prefab";
        private const string PistolThirdPersonPrefabPath = WeaponRoot + "/Secondary/Pistol/Prefabs/TP_Pistol.prefab";
        private const string PistolMuzzlePrefabPath = WeaponRoot + "/Secondary/Pistol/Prefabs/MuzzleFlash_Pistol.prefab";

        private const string PrimaryWeaponDefinitionPath = "Assets/Core/Game/FPS/Weapons/Definitions/PrimaryWeapon.asset";
        private const string PistolWeaponDefinitionPath = "Assets/Core/Game/FPS/Weapons/Definitions/PistolWeapon.asset";
        private const string PrimaryWeaponVisualDefinitionPath = "Assets/Core/Game/FPS/Weapons/Definitions/PrimaryWeaponVisual.asset";
        private const string PistolWeaponVisualDefinitionPath = "Assets/Core/Game/FPS/Weapons/Definitions/PistolWeaponVisual.asset";
        private const string PlayerCharacterPrefabPath = "Assets/Core/Game/FPS/PlayerCharacter.prefab";

        private static readonly SourceAsset[] SourceAssets =
        {
            new(
                "Assets/Resources/NeoFPS/Samples/Shared/Geometry/Character/Character_FirstPerson_Arms.FBX",
                FirstPersonArmsModelPath),
            new(
                "Assets/Resources/NeoFPS/Samples/Shared/Geometry/Character/Character_ThirdPerson.fbx",
                ThirdPersonCharacterModelPath),
            new(
                "Assets/Resources/NeoFPS/Samples/Shared/Geometry/Weapons/Weapon_FP_AssaultRifle.FBX",
                PrimaryFirstPersonModelPath),
            new(
                "Assets/Resources/NeoFPS/Samples/Shared/Geometry/Weapons/Weapon_Low_AssaultRifle.FBX",
                PrimaryThirdPersonModelPath),
            new(
                "Assets/Resources/NeoFPS/Samples/Shared/Geometry/Weapons/AnimCtrl_FP_AssaultRifle.controller",
                PrimaryFirstPersonAnimatorPath),
            new(
                "Assets/Resources/NeoFPS/Samples/Shared/Prefabs/Weapons/MuzzleFlashes/RealisticMuzzleFlash_AssaultRifle.prefab",
                PrimaryMuzzleSourcePath),
            new(
                "Assets/Resources/NeoFPS/Samples/Shared/Geometry/Weapons/Weapon_FP_Pistol.FBX",
                PistolFirstPersonModelPath),
            new(
                "Assets/Resources/NeoFPS/Samples/Shared/Geometry/Weapons/Weapon_Low_Pistol.FBX",
                PistolThirdPersonModelPath),
            new(
                "Assets/Resources/NeoFPS/Samples/Shared/Geometry/Weapons/AnimCtrl_FP_Pistol.controller",
                PistolFirstPersonAnimatorPath),
            new(
                "Assets/Resources/NeoFPS/Samples/Shared/Prefabs/Weapons/MuzzleFlashes/RealisticMuzzleFlash_Pistol.prefab",
                PistolMuzzleSourcePath),
        };

        [InitializeOnLoadMethod]
        private static void BuildAllIfRequested()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(AutoBuildRequestFile))
                    return;

                try
                {
                    BuildAll();
                    Debug.Log("Player visual resources rebuilt from request file.");
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
                finally
                {
                    if (File.Exists(AutoBuildRequestFile))
                        File.Delete(AutoBuildRequestFile);
                }
            };
        }

        [MenuItem("Dipshoot/Player Visuals/Rebuild Resources")]
        public static void BuildAll()
        {
            EnsureFolder(PresentationRoot);

            Dictionary<string, string> copied_paths = CopySourceAssets();
            AssetDatabase.Refresh();

            Dictionary<Object, Object> object_map = BuildObjectMap(copied_paths);
            RemapCopiedAssetReferences(copied_paths.Values, object_map);

            GameObject first_person_arms = BuildModelPrefab(
                "FirstPersonArms",
                FirstPersonArmsModelPath,
                FirstPersonArmsPrefabPath,
                "CharacterFirstPerson",
                false,
                object_map);
            GameObject third_person_character = BuildModelPrefab(
                "ThirdPersonCharacter",
                ThirdPersonCharacterModelPath,
                ThirdPersonCharacterPrefabPath,
                "CharacterExternal",
                false,
                object_map);

            GameObject primary_first_person = BuildModelPrefab(
                "FP_AssaultRifle",
                PrimaryFirstPersonModelPath,
                PrimaryFirstPersonPrefabPath,
                "WieldablesFirstPerson",
                true,
                object_map);
            GameObject primary_third_person = BuildModelPrefab(
                "TP_AssaultRifle",
                PrimaryThirdPersonModelPath,
                PrimaryThirdPersonPrefabPath,
                "WieldablesExternal",
                true,
                object_map);
            GameObject primary_muzzle = BuildCleanPrefab(
                "MuzzleFlash_AssaultRifle",
                PrimaryMuzzleSourcePath,
                PrimaryMuzzlePrefabPath,
                "WieldablesFirstPerson",
                object_map);

            GameObject pistol_first_person = BuildModelPrefab(
                "FP_Pistol",
                PistolFirstPersonModelPath,
                PistolFirstPersonPrefabPath,
                "WieldablesFirstPerson",
                true,
                object_map);
            GameObject pistol_third_person = BuildModelPrefab(
                "TP_Pistol",
                PistolThirdPersonModelPath,
                PistolThirdPersonPrefabPath,
                "WieldablesExternal",
                true,
                object_map);
            GameObject pistol_muzzle = BuildCleanPrefab(
                "MuzzleFlash_Pistol",
                PistolMuzzleSourcePath,
                PistolMuzzlePrefabPath,
                "WieldablesFirstPerson",
                object_map);

            PlayerVisualDefinition player_visual = BuildPlayerVisualDefinition(
                first_person_arms,
                third_person_character);
            WeaponVisualDefinition primary_visual = BuildWeaponVisualDefinition(
                PrimaryWeaponVisualDefinitionPath,
                WeaponSlot.Primary,
                primary_first_person,
                primary_third_person,
                primary_muzzle,
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(PrimaryFirstPersonAnimatorPath));
            WeaponVisualDefinition pistol_visual = BuildWeaponVisualDefinition(
                PistolWeaponVisualDefinitionPath,
                WeaponSlot.Pistol,
                pistol_first_person,
                pistol_third_person,
                pistol_muzzle,
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(PistolFirstPersonAnimatorPath));

            UpdateWeaponDefinitionVisual(PrimaryWeaponDefinitionPath, primary_visual);
            UpdateWeaponDefinitionVisual(PistolWeaponDefinitionPath, pistol_visual);
            UpdatePlayerCharacterPrefab(player_visual, first_person_arms, third_person_character);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static Dictionary<string, string> CopySourceAssets()
        {
            Dictionary<string, string> copied_paths = new(StringComparer.OrdinalIgnoreCase);
            HashSet<string> selected_source_paths = new(StringComparer.OrdinalIgnoreCase);
            foreach (SourceAsset source_asset in SourceAssets)
                selected_source_paths.Add(source_asset.SourcePath);

            foreach (SourceAsset source_asset in SourceAssets)
            {
                if (AssetDatabase.LoadMainAssetAtPath(source_asset.SourcePath) == null)
                    throw new FileNotFoundException($"Missing NeoFPS visual source asset: {source_asset.SourcePath}");

                string[] dependencies = AssetDatabase.GetDependencies(source_asset.SourcePath, true);
                foreach (string dependency in dependencies)
                {
                    if (!ShouldCopyDependency(dependency, selected_source_paths))
                        continue;

                    string destination = GetDestinationPath(dependency);
                    copied_paths[dependency] = destination;
                }
            }

            foreach (KeyValuePair<string, string> copied_path in copied_paths)
                CopyAssetIfNeeded(copied_path.Key, copied_path.Value);

            return copied_paths;
        }

        private static bool ShouldCopyDependency(string asset_path, HashSet<string> selected_source_paths)
        {
            if (string.IsNullOrEmpty(asset_path) ||
                !asset_path.StartsWith(NeoFpsSourceRoot, StringComparison.OrdinalIgnoreCase) ||
                AssetDatabase.IsValidFolder(asset_path))
            {
                return false;
            }

            string extension = Path.GetExtension(asset_path).ToLowerInvariant();
            if (extension == ".cs" ||
                extension == ".asmdef" ||
                extension == ".dll" ||
                extension == ".unity" ||
                extension == ".wav" ||
                extension == ".mp3" ||
                extension == ".ogg" ||
                extension == ".aif" ||
                extension == ".aiff" ||
                extension == ".flac")
            {
                return false;
            }

            if (extension == ".prefab" && !selected_source_paths.Contains(asset_path))
                return false;

            return true;
        }

        private static string GetDestinationPath(string source_path)
        {
            foreach (SourceAsset source_asset in SourceAssets)
            {
                if (string.Equals(source_asset.SourcePath, source_path, StringComparison.OrdinalIgnoreCase))
                    return source_asset.DestinationPath;
            }

            string relative_path = source_path.Substring(NeoFpsSourceRoot.Length).TrimStart('/');
            return $"{DependencyRoot}/{relative_path}";
        }

        private static void CopyAssetIfNeeded(string source_path, string destination_path)
        {
            EnsureFolderForAssetPath(destination_path);

            if (AssetDatabase.LoadMainAssetAtPath(destination_path) != null)
                return;

            if (!AssetDatabase.CopyAsset(source_path, destination_path))
                throw new InvalidOperationException($"Failed to copy asset from {source_path} to {destination_path}");
        }

        private static Dictionary<Object, Object> BuildObjectMap(Dictionary<string, string> copied_paths)
        {
            Dictionary<Object, Object> object_map = new();
            foreach (KeyValuePair<string, string> copied_path in copied_paths)
            {
                Object[] source_objects = AssetDatabase.LoadAllAssetsAtPath(copied_path.Key);
                Object[] destination_objects = AssetDatabase.LoadAllAssetsAtPath(copied_path.Value);
                foreach (Object source_object in source_objects)
                {
                    if (source_object == null)
                        continue;

                    foreach (Object destination_object in destination_objects)
                    {
                        if (destination_object == null ||
                            source_object.GetType() != destination_object.GetType() ||
                            source_object.name != destination_object.name ||
                            object_map.ContainsKey(source_object))
                        {
                            continue;
                        }

                        object_map.Add(source_object, destination_object);
                    }
                }
            }

            return object_map;
        }

        private static void RemapCopiedAssetReferences(
            IEnumerable<string> destination_paths,
            Dictionary<Object, Object> object_map)
        {
            HashSet<string> processed_paths = new(StringComparer.OrdinalIgnoreCase);
            foreach (string destination_path in destination_paths)
            {
                if (!processed_paths.Add(destination_path))
                    continue;

                Object[] assets = AssetDatabase.LoadAllAssetsAtPath(destination_path);
                foreach (Object asset in assets)
                {
                    if (asset == null || !CanRemapAsset(asset))
                        continue;

                    TryRemapObjectReferences(asset, object_map);
                }
            }
        }

        private static bool CanRemapAsset(Object asset)
        {
            return asset is Material ||
                asset is RuntimeAnimatorController ||
                asset is AvatarMask;
        }

        private static void TryRemapObjectReferences(Object asset, Dictionary<Object, Object> object_map)
        {
            try
            {
                SerializedObject serialized_object = new(asset);
                SerializedProperty property = serialized_object.GetIterator();
                bool enter_children = true;
                bool was_changed = false;
                while (property.NextVisible(enter_children))
                {
                    enter_children = false;
                    if (property.propertyType != SerializedPropertyType.ObjectReference ||
                        property.objectReferenceValue == null ||
                        !object_map.TryGetValue(property.objectReferenceValue, out Object replacement))
                    {
                        continue;
                    }

                    property.objectReferenceValue = replacement;
                    was_changed = true;
                }

                if (!was_changed)
                    return;

                serialized_object.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
            }
            catch (Exception)
            {
                // Imported sub-assets can reject SerializedObject edits. The generated prefabs still remap renderers explicitly.
            }
        }

        private static GameObject BuildModelPrefab(
            string root_name,
            string model_path,
            string prefab_path,
            string layer_name,
            bool add_muzzle_socket,
            Dictionary<Object, Object> object_map)
        {
            GameObject model_asset = AssetDatabase.LoadAssetAtPath<GameObject>(model_path);
            if (model_asset == null)
                throw new FileNotFoundException($"Missing copied model asset: {model_path}");

            GameObject root = new(root_name);
            GameObject model = InstantiatePrefab(model_asset, root.transform, "Model");
            TryUnpackPrefabInstance(model);
            StripNonVisualComponents(model);
            RemapRendererMaterials(root, object_map);

            if (add_muzzle_socket)
                EnsureMuzzleSocket(root.transform);

            PlayerVisualLayerUtility.SetLayerRecursive(root, layer_name);
            return SavePrefab(root, prefab_path);
        }

        private static GameObject BuildCleanPrefab(
            string root_name,
            string source_prefab_path,
            string prefab_path,
            string layer_name,
            Dictionary<Object, Object> object_map)
        {
            GameObject source_prefab = AssetDatabase.LoadAssetAtPath<GameObject>(source_prefab_path);
            if (source_prefab == null)
                throw new FileNotFoundException($"Missing copied visual prefab: {source_prefab_path}");

            GameObject root = InstantiatePrefab(source_prefab, null, root_name);
            TryUnpackPrefabInstance(root);
            StripNonVisualComponents(root);
            RemapRendererMaterials(root, object_map);
            PlayerVisualLayerUtility.SetLayerRecursive(root, layer_name);
            return SavePrefab(root, prefab_path);
        }

        private static PlayerVisualDefinition BuildPlayerVisualDefinition(
            GameObject first_person_arms,
            GameObject third_person_character)
        {
            PlayerVisualDefinition definition = GetOrCreateAsset<PlayerVisualDefinition>(PlayerVisualDefinitionPath);
            SerializedObject serialized_object = new(definition);
            Set(serialized_object, "_first_person_arms_prefab", first_person_arms);
            Set(serialized_object, "_third_person_character_prefab", third_person_character);
            serialized_object.FindProperty("_first_person_character_layer").stringValue = "CharacterFirstPerson";
            serialized_object.FindProperty("_third_person_character_layer").stringValue = "CharacterExternal";
            serialized_object.FindProperty("_first_person_weapon_layer").stringValue = "WieldablesFirstPerson";
            serialized_object.FindProperty("_third_person_weapon_layer").stringValue = "WieldablesExternal";
            serialized_object.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static WeaponVisualDefinition BuildWeaponVisualDefinition(
            string definition_path,
            WeaponSlot slot,
            GameObject first_person_prefab,
            GameObject third_person_prefab,
            GameObject muzzle_flash_prefab,
            RuntimeAnimatorController first_person_animator)
        {
            WeaponVisualDefinition definition = GetOrCreateAsset<WeaponVisualDefinition>(definition_path);
            SerializedObject serialized_object = new(definition);
            serialized_object.FindProperty("_slot").intValue = (int)slot;
            Set(serialized_object, "_first_person_prefab", first_person_prefab);
            Set(serialized_object, "_third_person_prefab", third_person_prefab);
            Set(serialized_object, "_muzzle_flash_prefab", muzzle_flash_prefab);
            Set(serialized_object, "_first_person_animator_controller", first_person_animator);
            Set(serialized_object, "_third_person_animator_controller", null);
            serialized_object.FindProperty("_muzzle_socket_name").stringValue = "MuzzleSocket";
            serialized_object.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static void UpdateWeaponDefinitionVisual(
            string weapon_definition_path,
            WeaponVisualDefinition visual_definition)
        {
            WeaponDefinition weapon_definition = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(weapon_definition_path);
            if (weapon_definition == null)
                throw new FileNotFoundException($"Missing weapon definition: {weapon_definition_path}");

            SerializedObject serialized_object = new(weapon_definition);
            Set(serialized_object, "_visual", visual_definition);
            serialized_object.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(weapon_definition);
        }

        private static void UpdatePlayerCharacterPrefab(
            PlayerVisualDefinition player_visual,
            GameObject first_person_arms,
            GameObject third_person_character)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerCharacterPrefabPath);
            try
            {
                Transform camera_point = FindChildRecursive(root.transform, "CameraPoint");
                Transform visual_root = FindOrCreateChild(root.transform, "Visual");
                Transform first_person_root = FindOrCreateChild(camera_point == null ? visual_root : camera_point, "FirstPersonView");
                Transform third_person_root = FindOrCreateChild(visual_root, "ThirdPersonView");
                Transform legacy_root = FindChildRecursive(root.transform, "Model");

                ClearChildren(first_person_root);
                ClearChildren(third_person_root);
                InstantiatePrefab(first_person_arms, first_person_root, "FirstPersonArms");
                InstantiatePrefab(third_person_character, third_person_root, "ThirdPersonCharacter");

                DisableRenderers(legacy_root);
                RebuildDefaultHitboxes(root);
                SerializeWeaponRaycastSettings(root);

                PlayerVisualController controller = root.GetComponent<PlayerVisualController>();
                if (controller == null)
                    controller = root.AddComponent<PlayerVisualController>();

                SerializedObject serialized_object = new(controller);
                Set(serialized_object, "_character", root.GetComponent<PlayerCharacter>());
                Set(serialized_object, "_definition", player_visual);
                Set(serialized_object, "_camera_point", camera_point);
                Set(serialized_object, "_visual_root", visual_root);
                Set(serialized_object, "_first_person_root", first_person_root);
                Set(serialized_object, "_third_person_root", third_person_root);
                Set(serialized_object, "_legacy_render_root", legacy_root);
                serialized_object.FindProperty("_hide_legacy_renderers").boolValue = true;
                serialized_object.ApplyModifiedPropertiesWithoutUndo();

                PlayerWeaponVisualController weapon_visual_controller = root.GetComponent<PlayerWeaponVisualController>();
                if (weapon_visual_controller == null)
                    weapon_visual_controller = root.AddComponent<PlayerWeaponVisualController>();

                SerializedObject weapon_visual_serialized_object = new(weapon_visual_controller);
                Set(weapon_visual_serialized_object, "_visual", controller);
                Set(weapon_visual_serialized_object, "_weapon_controller", root.GetComponent<WeaponController>());
                weapon_visual_serialized_object.FindProperty("_hide_base_first_person_arms").boolValue = true;
                weapon_visual_serialized_object.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PlayerCharacterPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void RebuildDefaultHitboxes(GameObject root)
        {
            PlayerHealth health = root.GetComponent<PlayerHealth>();
            Transform hitbox_root = FindOrCreateChild(root.transform, "Hitboxes");
            ClearChildren(hitbox_root);

            int hitbox_layer = LayerMask.NameToLayer("CharacterPhysics");
            CreateSphereHitbox(
                "HeadHitbox",
                hitbox_root,
                health,
                PlayerHitboxType.Head,
                2f,
                new Vector3(0f, 0.78f, 0.03f),
                0.23f,
                hitbox_layer);
            CreateBoxHitbox(
                "ChestHitbox",
                hitbox_root,
                health,
                PlayerHitboxType.Chest,
                1f,
                new Vector3(0f, 0.25f, 0f),
                new Vector3(0.72f, 0.7f, 0.44f),
                hitbox_layer);
            CreateBoxHitbox(
                "PelvisHitbox",
                hitbox_root,
                health,
                PlayerHitboxType.Pelvis,
                0.9f,
                new Vector3(0f, -0.24f, 0f),
                new Vector3(0.64f, 0.38f, 0.38f),
                hitbox_layer);
            CreateBoxHitbox(
                "LeftArmHitbox",
                hitbox_root,
                health,
                PlayerHitboxType.Arm,
                0.75f,
                new Vector3(-0.5f, 0.13f, 0f),
                new Vector3(0.24f, 0.72f, 0.28f),
                hitbox_layer);
            CreateBoxHitbox(
                "RightArmHitbox",
                hitbox_root,
                health,
                PlayerHitboxType.Arm,
                0.75f,
                new Vector3(0.5f, 0.13f, 0f),
                new Vector3(0.24f, 0.72f, 0.28f),
                hitbox_layer);
            CreateBoxHitbox(
                "LeftLegHitbox",
                hitbox_root,
                health,
                PlayerHitboxType.Leg,
                0.75f,
                new Vector3(-0.18f, -0.68f, 0f),
                new Vector3(0.26f, 0.76f, 0.3f),
                hitbox_layer);
            CreateBoxHitbox(
                "RightLegHitbox",
                hitbox_root,
                health,
                PlayerHitboxType.Leg,
                0.75f,
                new Vector3(0.18f, -0.68f, 0f),
                new Vector3(0.26f, 0.76f, 0.3f),
                hitbox_layer);

            PlayerVisualLayerUtility.SetLayerRecursive(hitbox_root, hitbox_layer);
        }

        private static void CreateBoxHitbox(
            string name,
            Transform parent,
            PlayerHealth health,
            PlayerHitboxType type,
            float damage_multiplier,
            Vector3 local_position,
            Vector3 size,
            int layer)
        {
            GameObject target = CreateHitboxRoot(name, parent, local_position, layer);
            BoxCollider collider = target.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = size;
            SerializeHitbox(target.AddComponent<PlayerHitbox>(), health, collider, type, damage_multiplier);
        }

        private static void CreateSphereHitbox(
            string name,
            Transform parent,
            PlayerHealth health,
            PlayerHitboxType type,
            float damage_multiplier,
            Vector3 local_position,
            float radius,
            int layer)
        {
            GameObject target = CreateHitboxRoot(name, parent, local_position, layer);
            SphereCollider collider = target.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = radius;
            SerializeHitbox(target.AddComponent<PlayerHitbox>(), health, collider, type, damage_multiplier);
        }

        private static GameObject CreateHitboxRoot(
            string name,
            Transform parent,
            Vector3 local_position,
            int layer)
        {
            GameObject target = new(name);
            target.transform.SetParent(parent, false);
            target.transform.localPosition = local_position;
            target.transform.localRotation = Quaternion.identity;
            target.transform.localScale = Vector3.one;
            if (layer >= 0)
                target.layer = layer;

            return target;
        }

        private static void SerializeHitbox(
            PlayerHitbox hitbox,
            PlayerHealth health,
            Collider collider,
            PlayerHitboxType type,
            float damage_multiplier)
        {
            SerializedObject serialized_object = new(hitbox);
            Set(serialized_object, "_health", health);
            Set(serialized_object, "_collider", collider);
            serialized_object.FindProperty("_type").intValue = (int)type;
            serialized_object.FindProperty("_damage_multiplier").floatValue = damage_multiplier;
            serialized_object.FindProperty("_force_trigger").boolValue = true;
            serialized_object.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SerializeWeaponRaycastSettings(GameObject root)
        {
            WeaponController weapon_controller = root.GetComponent<WeaponController>();
            if (weapon_controller == null)
                return;

            SerializedObject serialized_object = new(weapon_controller);
            serialized_object.FindProperty("_trigger_interaction").enumValueIndex = (int)QueryTriggerInteraction.Collide;
            serialized_object.ApplyModifiedPropertiesWithoutUndo();
        }

        private static T GetOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;

            EnsureFolderForAssetPath(path);
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static GameObject InstantiatePrefab(GameObject prefab, Transform parent, string name)
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
            if (instance == null)
                instance = Object.Instantiate(prefab, parent);

            instance.name = name;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            return instance;
        }

        private static void TryUnpackPrefabInstance(GameObject target)
        {
            try
            {
                PrefabUtility.UnpackPrefabInstance(target, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }
            catch (Exception)
            {
                // FBX model instances can reject unpack in some import states. Keeping the instance is still valid.
            }
        }

        private static void StripNonVisualComponents(GameObject root)
        {
            Component[] components = root.GetComponentsInChildren<Component>(true);
            foreach (Component component in components)
            {
                if (component == null ||
                    component is Transform ||
                    component is MeshFilter ||
                    component is Renderer ||
                    component is Animator ||
                    component is Animation ||
                    component is ParticleSystem ||
                    component is Light)
                {
                    continue;
                }

                Object.DestroyImmediate(component);
            }
        }

        private static void RemapRendererMaterials(GameObject root, Dictionary<Object, Object> object_map)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                Material[] materials = renderer.sharedMaterials;
                bool was_changed = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null ||
                        !object_map.TryGetValue(materials[i], out Object replacement) ||
                        replacement is not Material replacement_material)
                    {
                        continue;
                    }

                    materials[i] = replacement_material;
                    was_changed = true;
                }

                if (was_changed)
                    renderer.sharedMaterials = materials;
            }
        }

        private static void EnsureMuzzleSocket(Transform root)
        {
            if (FindChildRecursive(root, "MuzzleSocket") != null)
                return;

            Transform socket_parent = FindChildRecursive(root, "MuzzleFlash") ??
                FindChildRecursive(root, "Muzzle") ??
                FindChildRecursive(root, "Barrel") ??
                root;

            GameObject socket = new("MuzzleSocket");
            socket.transform.SetParent(socket_parent, false);
            socket.transform.localPosition = Vector3.zero;
            socket.transform.localRotation = Quaternion.identity;
            socket.transform.localScale = Vector3.one;
        }

        private static Transform FindOrCreateChild(Transform parent, string name)
        {
            Transform child = FindDirectChild(parent, name);
            if (child != null)
                return child;

            GameObject target = new(name);
            target.transform.SetParent(parent, false);
            return target.transform;
        }

        private static Transform FindDirectChild(Transform parent, string name)
        {
            if (parent == null)
                return null;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == name)
                    return child;
            }

            return null;
        }

        private static Transform FindChildRecursive(Transform parent, string name)
        {
            if (parent == null)
                return null;

            if (parent.name == name)
                return parent;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform result = FindChildRecursive(parent.GetChild(i), name);
                if (result != null)
                    return result;
            }

            return null;
        }

        private static void ClearChildren(Transform parent)
        {
            if (parent == null)
                return;

            for (int i = parent.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }

        private static void DisableRenderers(Transform root)
        {
            if (root == null)
                return;

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
                renderer.enabled = false;
        }

        private static GameObject SavePrefab(GameObject target, string path)
        {
            EnsureFolderForAssetPath(path);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(target, path);
            Object.DestroyImmediate(target);
            return prefab;
        }

        private static void EnsureFolderForAssetPath(string asset_path)
        {
            string folder_path = Path.GetDirectoryName(asset_path)?.Replace("\\", "/");
            if (!string.IsNullOrEmpty(folder_path))
                EnsureFolder(folder_path);
        }

        private static void EnsureFolder(string folder_path)
        {
            if (string.IsNullOrEmpty(folder_path) || AssetDatabase.IsValidFolder(folder_path))
                return;

            string parent = Path.GetDirectoryName(folder_path)?.Replace("\\", "/");
            string folder_name = Path.GetFileName(folder_path);

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folder_name);
        }

        private static void Set(SerializedObject serialized_object, string property_name, Object value)
        {
            SerializedProperty property = serialized_object.FindProperty(property_name);
            property.objectReferenceValue = value;
        }

        private readonly struct SourceAsset
        {
            public readonly string SourcePath;
            public readonly string DestinationPath;

            public SourceAsset(string source_path, string destination_path)
            {
                SourcePath = source_path;
                DestinationPath = destination_path;
            }
        }
    }
}
