using System;
using UnityEngine;

namespace Game.Players
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-10000)]
    public class PlayerHitboxRigController : MonoBehaviour
    {
        private const string LegacyHitboxRootName = "Hitboxes";
        private const string HitboxSuffix = "Hitbox";
        private const string HitboxLayerName = "CharacterPhysics";

        private static readonly PlayerHitboxRigProfile.HitboxBinding[] DefaultBindings =
        {
            Sphere("Head", "FpsChar_Head_Bone", PlayerHitboxType.Head, 2f, new Vector3(0f, 0.04f, 0f), 0.22f),
            Capsule("Chest", "FpsChar_Spine_Bone", "FpsChar_Neck_Bone", PlayerHitboxType.Chest, 1f, 0.28f, 0.04f),
            Capsule("Pelvis", "FpsChar_Pelvis_Bone", "FpsChar_Spine_Bone", PlayerHitboxType.Pelvis, 0.9f, 0.26f, 0.02f),
            Capsule("LeftUpperArm", "FpsChar_LArm_Upper_Bone", "FpsChar_LArm_Lower_Bone", PlayerHitboxType.Arm, 0.75f, 0.105f, 0.01f),
            Capsule("LeftLowerArm", "FpsChar_LArm_Lower_Bone", "FpsChar_LHand_Bone", PlayerHitboxType.Arm, 0.75f, 0.09f, 0.01f),
            Capsule("RightUpperArm", "FpsChar_RArm_Upper_Bone", "FpsChar_RArm_Lower_Bone", PlayerHitboxType.Arm, 0.75f, 0.105f, 0.01f),
            Capsule("RightLowerArm", "FpsChar_RArm_Lower_Bone", "FpsChar_RHand_Bone", PlayerHitboxType.Arm, 0.75f, 0.09f, 0.01f),
            Capsule("LeftUpperLeg", "FpsChar_LLeg_Upper_Bone", "FpsChar_LLeg_Lower_Bone", PlayerHitboxType.Leg, 0.75f, 0.13f, 0.02f),
            Capsule("LeftLowerLeg", "FpsChar_LLeg_Lower_Bone", "FpsChar_LLeg_Foot_Bone", PlayerHitboxType.Leg, 0.75f, 0.105f, 0.02f),
            Capsule("RightUpperLeg", "FpsChar_RLeg_Upper_Bone", "FpsChar_RLeg_Lower_Bone", PlayerHitboxType.Leg, 0.75f, 0.13f, 0.02f),
            Capsule("RightLowerLeg", "FpsChar_RLeg_Lower_Bone", "FpsChar_RLeg_Foot_Bone", PlayerHitboxType.Leg, 0.75f, 0.105f, 0.02f),
        };

        [SerializeField] private PlayerHealth _health;
        [SerializeField] private Transform _skeleton_root;
        [SerializeField] private PlayerHitboxRigProfile _profile;
        [SerializeField] private bool _rebuild_on_awake = true;
        [SerializeField] private bool _remove_legacy_root = true;

        private int _pending_rebuild_frames;
        private int _last_created_count;

        public int HitboxCount => _last_created_count;

        private void Awake()
        {
            EnsureLagCompensation();

            if (_rebuild_on_awake)
                RequestRebuild();
        }

        private void LateUpdate()
        {
            if (_pending_rebuild_frames <= 0)
                return;

            _pending_rebuild_frames--;
            Rebuild();

            if (_last_created_count > 0)
                _pending_rebuild_frames = 0;
        }

        public void RequestRebuild()
        {
            _pending_rebuild_frames = 5;
            Rebuild();
        }

        public void Rebuild()
        {
            CacheReferences();

            if (_health == null)
                return;

            if (_remove_legacy_root)
                RemoveLegacyHitboxRoot();

            PlayerHitboxRigProfile.HitboxBinding[] bindings = GetBindings();
            int created_count = 0;
            for (int i = 0; i < bindings.Length; i++)
            {
                if (CreateOrUpdateHitbox(bindings[i]))
                    created_count++;
            }

            if (created_count == 0)
            {
                Debug.LogWarning(
                    $"[HitboxRig] No hitboxes created for {name}. Skeleton={_skeleton_root?.name}, Profile={_profile?.name}.");
            }

            _last_created_count = created_count;
            _health.RefreshPresentationTargets();
            RefreshLagCompensation();
        }

        private void CacheReferences()
        {
            if (_health == null)
                _health = GetComponent<PlayerHealth>();

            if (_profile == null &&
                TryGetComponent(out PlayerVisualController visual_controller) &&
                visual_controller.Definition != null)
            {
                _profile = visual_controller.Definition.HitboxRigProfile;
            }

            Transform skeleton_root = null;
            if (TryGetComponent(out PlayerVisualController current_visual_controller))
                skeleton_root = FindChildRecursive(current_visual_controller.ThirdPersonRoot, "ThirdPersonCharacter");

            if (skeleton_root == null)
                skeleton_root = FindChildRecursive(transform, "ThirdPersonCharacter");

            if (skeleton_root != null)
                _skeleton_root = skeleton_root;

            if (_skeleton_root == null)
                _skeleton_root = transform;
        }

        private void EnsureLagCompensation()
        {
            if (GetComponent<PlayerHitboxLagCompensation>() != null)
                return;

            gameObject.AddComponent<PlayerHitboxLagCompensation>();
        }

        private void RefreshLagCompensation()
        {
            PlayerHitboxLagCompensation lag_compensation = GetComponent<PlayerHitboxLagCompensation>();
            if (lag_compensation != null)
                lag_compensation.RefreshHitboxes();
        }

        private void RemoveLegacyHitboxRoot()
        {
            Transform legacy_root = FindDirectChild(transform, LegacyHitboxRootName);
            if (legacy_root == null)
                return;

            DisableLegacyHitboxes(legacy_root);
            legacy_root.gameObject.SetActive(false);

            if (Application.isPlaying)
                Destroy(legacy_root.gameObject);
            else
                DestroyImmediate(legacy_root.gameObject);
        }

        private PlayerHitboxRigProfile.HitboxBinding[] GetBindings()
        {
            return _profile != null && _profile.Bindings != null && _profile.Bindings.Length > 0
                ? _profile.Bindings
                : DefaultBindings;
        }

        private bool CreateOrUpdateHitbox(PlayerHitboxRigProfile.HitboxBinding binding)
        {
            Transform start_bone = FindChildRecursive(_skeleton_root, binding.StartBoneName);
            if (start_bone == null)
                return false;

            Transform end_bone = binding.Shape == PlayerHitboxRigProfile.HitboxShape.Capsule
                ? FindChildRecursive(_skeleton_root, binding.EndBoneName)
                : null;
            if (binding.Shape == PlayerHitboxRigProfile.HitboxShape.Capsule && end_bone == null)
                return false;

            Transform hitbox_transform = FindDirectChild(start_bone, binding.Name + HitboxSuffix);
            GameObject hitbox_object = hitbox_transform == null
                ? new GameObject(binding.Name + HitboxSuffix)
                : hitbox_transform.gameObject;

            hitbox_object.SetActive(true);
            hitbox_object.transform.SetParent(start_bone, false);
            float capsule_height = ApplyHitboxTransform(hitbox_object.transform, start_bone, end_bone, binding);
            hitbox_object.layer = GetHitboxLayer();

            Collider hitbox_collider = EnsureCollider(hitbox_object, binding, capsule_height);
            PlayerHitbox hitbox = hitbox_object.GetComponent<PlayerHitbox>();
            if (hitbox == null)
                hitbox = hitbox_object.AddComponent<PlayerHitbox>();

            hitbox.Initialize(_health, hitbox_collider, binding.Type, binding.DamageMultiplier);
            return true;
        }

        private static float ApplyHitboxTransform(
            Transform hitbox,
            Transform start_bone,
            Transform end_bone,
            PlayerHitboxRigProfile.HitboxBinding binding)
        {
            hitbox.localScale = Vector3.one;
            if (binding.Shape != PlayerHitboxRigProfile.HitboxShape.Capsule)
            {
                hitbox.localPosition = binding.LocalPosition;
                hitbox.localRotation = Quaternion.identity;
                return binding.Radius * 2f;
            }

            Vector3 start = start_bone.position;
            Vector3 end = end_bone.position;
            Vector3 direction = end - start;
            Vector3 center = (start + end) * 0.5f;
            Quaternion rotation = direction.sqrMagnitude <= 0.0001f
                ? start_bone.rotation
                : Quaternion.FromToRotation(Vector3.up, direction.normalized);

            hitbox.SetPositionAndRotation(center, rotation);
            return Mathf.Max(
                binding.Radius * 2f,
                direction.magnitude + binding.Radius * 2f + binding.LengthPadding);
        }

        private static Collider EnsureCollider(GameObject target, PlayerHitboxRigProfile.HitboxBinding binding, float capsule_height)
        {
            RemoveWrongColliders(target, binding.Shape);
            return binding.Shape switch
            {
                PlayerHitboxRigProfile.HitboxShape.Sphere => ConfigureSphere(target, binding),
                PlayerHitboxRigProfile.HitboxShape.Capsule => ConfigureCapsule(target, binding, capsule_height),
                _ => ConfigureBox(target, binding),
            };
        }

        private static BoxCollider ConfigureBox(GameObject target, PlayerHitboxRigProfile.HitboxBinding binding)
        {
            BoxCollider collider = target.GetComponent<BoxCollider>();
            if (collider == null)
                collider = target.AddComponent<BoxCollider>();

            collider.isTrigger = true;
            collider.center = Vector3.zero;
            collider.size = binding.Size;
            return collider;
        }

        private static SphereCollider ConfigureSphere(GameObject target, PlayerHitboxRigProfile.HitboxBinding binding)
        {
            SphereCollider collider = target.GetComponent<SphereCollider>();
            if (collider == null)
                collider = target.AddComponent<SphereCollider>();

            collider.isTrigger = true;
            collider.center = Vector3.zero;
            collider.radius = binding.Radius;
            return collider;
        }

        private static CapsuleCollider ConfigureCapsule(GameObject target, PlayerHitboxRigProfile.HitboxBinding binding, float capsule_height)
        {
            CapsuleCollider collider = target.GetComponent<CapsuleCollider>();
            if (collider == null)
                collider = target.AddComponent<CapsuleCollider>();

            collider.isTrigger = true;
            collider.center = Vector3.zero;
            collider.radius = binding.Radius;
            collider.height = capsule_height;
            collider.direction = 1;
            return collider;
        }

        private static void RemoveWrongColliders(GameObject target, PlayerHitboxRigProfile.HitboxShape shape)
        {
            Collider[] colliders = target.GetComponents<Collider>();
            for (int i = 0; i < colliders.Length; i++)
            {
                if (IsExpectedCollider(colliders[i], shape))
                    continue;

                if (Application.isPlaying)
                    Destroy(colliders[i]);
                else
                    DestroyImmediate(colliders[i]);
            }
        }

        private static bool IsExpectedCollider(Collider collider, PlayerHitboxRigProfile.HitboxShape shape)
        {
            return shape switch
            {
                PlayerHitboxRigProfile.HitboxShape.Sphere => collider is SphereCollider,
                PlayerHitboxRigProfile.HitboxShape.Capsule => collider is CapsuleCollider,
                _ => collider is BoxCollider,
            };
        }

        private static void DisableLegacyHitboxes(Transform legacy_root)
        {
            PlayerHitbox[] hitboxes = legacy_root.GetComponentsInChildren<PlayerHitbox>(true);
            for (int i = 0; i < hitboxes.Length; i++)
            {
                if (hitboxes[i] != null)
                    hitboxes[i].enabled = false;
            }

            Collider[] colliders = legacy_root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                    colliders[i].enabled = false;
            }
        }

        private static Transform FindChildRecursive(Transform root, string name)
        {
            if (root == null)
                return null;

            if (root.name == name)
                return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = FindChildRecursive(root.GetChild(i), name);
                if (child != null)
                    return child;
            }

            return null;
        }

        private static int GetHitboxLayer()
        {
            int layer = LayerMask.NameToLayer(HitboxLayerName);
            return layer < 0 ? 0 : layer;
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

        private static PlayerHitboxRigProfile.HitboxBinding Sphere(
            string name,
            string bone_name,
            PlayerHitboxType type,
            float damage_multiplier,
            Vector3 local_position,
            float radius)
        {
            return new PlayerHitboxRigProfile.HitboxBinding
            {
                Name = name,
                StartBoneName = bone_name,
                Type = type,
                DamageMultiplier = damage_multiplier,
                Shape = PlayerHitboxRigProfile.HitboxShape.Sphere,
                LocalPosition = local_position,
                Size = Vector3.one * radius * 2f,
                Radius = radius,
            };
        }

        private static PlayerHitboxRigProfile.HitboxBinding Capsule(
            string name,
            string start_bone_name,
            string end_bone_name,
            PlayerHitboxType type,
            float damage_multiplier,
            float radius,
            float length_padding)
        {
            return new PlayerHitboxRigProfile.HitboxBinding
            {
                Name = name,
                StartBoneName = start_bone_name,
                EndBoneName = end_bone_name,
                Type = type,
                DamageMultiplier = damage_multiplier,
                Shape = PlayerHitboxRigProfile.HitboxShape.Capsule,
                Size = new Vector3(radius * 2f, radius * 2f, radius * 2f),
                Radius = radius,
                LengthPadding = length_padding,
            };
        }
    }
}
