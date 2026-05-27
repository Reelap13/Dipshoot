using UnityEngine;
namespace Game.Players
{
    [DisallowMultipleComponent]
    public class PlayerWeaponVisualController : MonoBehaviour
    {
        private const string PrimaryName = "PrimaryWeaponVisual";
        private const string PistolName = "PistolWeaponVisual";
        private const string ThirdPersonWeaponSocketName = "FpsChar_RHand_Bone";
        private const string FireTrigger = "Fire";
        private const string ReloadTrigger = "Reload";
        private const string DrawTrigger = "Draw";
        private const float MuzzleFlashLifetime = 0.15f;

        [SerializeField] private PlayerVisualController _visual;
        [SerializeField] private WeaponController _weapon_controller;
        [SerializeField] private bool _hide_base_first_person_arms = true;

        private WeaponVisualInstance _primary_visual = new(WeaponSlot.Primary, PrimaryName);
        private WeaponVisualInstance _pistol_visual = new(WeaponSlot.Pistol, PistolName);
        private WeaponSlot _last_active_slot = WeaponSlot.None;
        private bool _last_active_reload_state;
        private bool _has_weapon_state;

        private void Awake()
        {
            CacheReferences();
            EnsureVisuals();
            ApplyState();
        }

        private void Update()
        {
            CacheReferences();
            EnsureVisuals();
            ApplyState();
        }

        public void PlayShot(ShotResult result)
        {
            WeaponVisualInstance visual = GetVisual(result.WeaponSlot);
            if (visual == null)
                return;

            bool use_first_person = _visual != null && _visual.IsFirstPersonVisible;
            visual.Trigger(use_first_person, FireTrigger);
            SpawnMuzzleFlash(visual, use_first_person);
            PlayAudio(visual, use_first_person, visual.Definition?.Audio?.Fire);
        }

        public bool TryGetShotTracerOrigin(WeaponSlot slot, out Vector3 origin)
        {
            origin = default;
            if (_visual == null)
                return false;

            Transform socket = GetVisual(slot)?.GetMuzzleSocket(_visual.IsFirstPersonVisible);
            if (socket == null)
                return false;

            origin = socket.position;
            return true;
        }

        private void CacheReferences()
        {
            if (_visual == null)
                _visual = GetComponent<PlayerVisualController>();

            if (_weapon_controller == null)
                _weapon_controller = GetComponent<WeaponController>();
        }

        private void EnsureVisuals()
        {
            if (_visual == null || _weapon_controller == null)
                return;

            _primary_visual.Ensure(
                _weapon_controller.PrimaryWeaponDefinition?.Visual,
                _visual.FirstPersonRoot,
                GetThirdPersonWeaponRoot(),
                GetFirstPersonWeaponLayer(),
                GetThirdPersonWeaponLayer());
            _pistol_visual.Ensure(
                _weapon_controller.PistolWeaponDefinition?.Visual,
                _visual.FirstPersonRoot,
                GetThirdPersonWeaponRoot(),
                GetFirstPersonWeaponLayer(),
                GetThirdPersonWeaponLayer());
        }

        private void ApplyState()
        {
            if (_visual == null || _weapon_controller == null)
                return;

            WeaponSlot active_slot = _weapon_controller.ActiveSlot;
            bool show_first_person = _visual.IsFirstPersonVisible;
            bool show_third_person = _visual.IsThirdPersonVisible;
            bool has_first_person_weapon = HasFirstPersonWeapon(active_slot);

            _primary_visual.SetVisible(
                active_slot == WeaponSlot.Primary && show_first_person,
                active_slot == WeaponSlot.Primary && show_third_person);
            _pistol_visual.SetVisible(
                active_slot == WeaponSlot.Pistol && show_first_person,
                active_slot == WeaponSlot.Pistol && show_third_person);

            SetBaseFirstPersonArmsVisible(!show_first_person || !has_first_person_weapon);

            if (!_has_weapon_state || _last_active_slot != active_slot)
            {
                GetVisual(active_slot)?.Trigger(show_first_person, DrawTrigger);
                _last_active_slot = active_slot;
                _last_active_reload_state = _weapon_controller.IsActiveReloading;
                _has_weapon_state = true;
                return;
            }

            bool is_reloading = _weapon_controller.IsActiveReloading;
            if (is_reloading && !_last_active_reload_state)
            {
                GetVisual(active_slot)?.Trigger(show_first_person, ReloadTrigger);
                WeaponVisualInstance visual = GetVisual(active_slot);
                PlayAudio(visual, show_first_person, visual?.Definition?.Audio?.Reload);
            }

            _last_active_reload_state = is_reloading;
        }

        private Transform GetThirdPersonWeaponRoot()
        {
            Transform hand = FindChildRecursive(_visual.ThirdPersonRoot, ThirdPersonWeaponSocketName);
            return hand == null ? _visual.ThirdPersonRoot : hand;
        }

        private string GetFirstPersonWeaponLayer()
        {
            return _visual.Definition == null
                ? "WieldablesFirstPerson"
                : _visual.Definition.FirstPersonWeaponLayer;
        }

        private string GetThirdPersonWeaponLayer()
        {
            return _visual.Definition == null
                ? "WieldablesExternal"
                : _visual.Definition.ThirdPersonWeaponLayer;
        }

        private bool HasFirstPersonWeapon(WeaponSlot slot)
        {
            WeaponVisualInstance visual = GetVisual(slot);
            return visual != null && visual.HasFirstPersonInstance;
        }

        private WeaponVisualInstance GetVisual(WeaponSlot slot)
        {
            return slot == WeaponSlot.Pistol
                ? _pistol_visual
                : slot == WeaponSlot.Primary
                    ? _primary_visual
                    : null;
        }

        private void SetBaseFirstPersonArmsVisible(bool is_visible)
        {
            if (!_hide_base_first_person_arms || _visual.FirstPersonArmsInstance == null)
                return;

            if (_visual.FirstPersonArmsInstance.activeSelf != is_visible)
                _visual.FirstPersonArmsInstance.SetActive(is_visible);
        }

        private void SpawnMuzzleFlash(WeaponVisualInstance visual, bool use_first_person)
        {
            Transform socket = visual.GetMuzzleSocket(use_first_person);
            GameObject prefab = visual.Definition == null ? null : visual.Definition.MuzzleFlashPrefab;
            if (socket == null)
            {
                WeaponVfxUtility.LogWarningOnce(
                    this,
                    $"MissingMuzzleSocket:{visual.Definition?.name}:{use_first_person}",
                    $"[WeaponVFX] Missing muzzle socket for {visual.Definition?.name}.");
                return;
            }

            if (prefab == null)
            {
                WeaponVfxUtility.LogWarningOnce(
                    this,
                    $"MissingMuzzleFlash:{visual.Definition?.name}",
                    $"[WeaponVFX] Missing muzzle flash prefab for {visual.Definition?.name}.");
                return;
            }

            GameObject muzzle_flash = Instantiate(prefab, socket);
            muzzle_flash.name = "MuzzleFlash";
            muzzle_flash.transform.localPosition = Vector3.zero;
            muzzle_flash.transform.localRotation = Quaternion.identity;
            muzzle_flash.transform.localScale = Vector3.one;
            PlayerVisualLayerUtility.SetLayerRecursive(muzzle_flash.transform, socket.gameObject.layer);
            WeaponVfxUtility.PlayParticles(muzzle_flash);

            muzzle_flash.AddComponent<SelfDestroyer>().Initialize(MuzzleFlashLifetime);
        }

        private void PlayAudio(WeaponVisualInstance visual, bool use_first_person, AudioCue cue)
        {
            if (visual == null || cue == null)
                return;

            Transform socket = visual.GetMuzzleSocket(use_first_person);
            Vector3 position = socket == null ? transform.position : socket.position;
            GameAudioService.Instance.Play(cue, position, use_first_person);
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

        private sealed class WeaponVisualInstance
        {
            private readonly WeaponSlot _slot;
            private readonly string _name;

            private GameObject _first_person_instance;
            private GameObject _third_person_instance;
            private Animator _first_person_animator;
            private Animator _third_person_animator;
            private Transform _first_person_muzzle_socket;
            private Transform _third_person_muzzle_socket;
            private WeaponVisualDefinition _definition;

            public WeaponVisualDefinition Definition => _definition;
            public bool HasFirstPersonInstance => _first_person_instance != null;

            public WeaponVisualInstance(WeaponSlot slot, string name)
            {
                _slot = slot;
                _name = name;
            }

            public void Ensure(
                WeaponVisualDefinition definition,
                Transform first_person_parent,
                Transform third_person_parent,
                string first_person_layer,
                string third_person_layer)
            {
                if (_definition == definition &&
                    ParentMatches(_first_person_instance, first_person_parent) &&
                    ParentMatches(_third_person_instance, third_person_parent))
                {
                    return;
                }

                DestroyInstance(_first_person_instance);
                DestroyInstance(_third_person_instance);
                _first_person_instance = null;
                _third_person_instance = null;
                _first_person_animator = null;
                _third_person_animator = null;
                _first_person_muzzle_socket = null;
                _third_person_muzzle_socket = null;
                _definition = definition;

                if (definition == null || definition.Slot != _slot)
                    return;

                _first_person_instance = InstantiateVisual(
                    definition.FirstPersonPrefab,
                    first_person_parent,
                    $"{_name}_FirstPerson",
                    first_person_layer,
                    definition.FirstPersonLocalPosition,
                    definition.FirstPersonLocalEulerAngles,
                    definition.FirstPersonLocalScale,
                    definition.FirstPersonAnimatorController,
                    out _first_person_animator);
                _third_person_instance = InstantiateVisual(
                    definition.ThirdPersonPrefab,
                    third_person_parent,
                    $"{_name}_ThirdPerson",
                    third_person_layer,
                    definition.ThirdPersonLocalPosition,
                    definition.ThirdPersonLocalEulerAngles,
                    definition.ThirdPersonLocalScale,
                    definition.ThirdPersonAnimatorController,
                    out _third_person_animator);
                _first_person_muzzle_socket = FindChildRecursive(_first_person_instance?.transform, definition.MuzzleSocketName);
                _third_person_muzzle_socket = FindChildRecursive(_third_person_instance?.transform, definition.MuzzleSocketName);
            }

            public void SetVisible(bool first_person_visible, bool third_person_visible)
            {
                SetActive(_first_person_instance, first_person_visible);
                SetActive(_third_person_instance, third_person_visible);
            }

            public void Trigger(bool use_first_person, string trigger_name)
            {
                Animator animator = use_first_person ? _first_person_animator : _third_person_animator;
                if (animator == null || !HasParameter(animator, trigger_name))
                    return;

                animator.ResetTrigger(trigger_name);
                animator.SetTrigger(trigger_name);
            }

            public Transform GetMuzzleSocket(bool use_first_person)
            {
                return use_first_person
                    ? _first_person_muzzle_socket
                    : _third_person_muzzle_socket;
            }

            private static GameObject InstantiateVisual(
                GameObject prefab,
                Transform parent,
                string name,
                string layer_name,
                Vector3 local_position,
                Vector3 local_euler_angles,
                Vector3 local_scale,
                RuntimeAnimatorController animator_controller,
                out Animator animator)
            {
                animator = null;
                if (prefab == null || parent == null)
                    return null;

                GameObject instance = Object.Instantiate(prefab, parent);
                instance.name = name;
                instance.transform.localPosition = local_position;
                instance.transform.localRotation = Quaternion.Euler(local_euler_angles);
                instance.transform.localScale = local_scale;
                PlayerVisualLayerUtility.SetLayerRecursive(instance, layer_name);
                DisableEmbeddedMuzzleFlashes(instance.transform);

                animator = instance.GetComponentInChildren<Animator>(true);
                if (animator != null && animator_controller != null)
                    animator.runtimeAnimatorController = animator_controller;

                instance.SetActive(false);
                return instance;
            }

            private static void DisableEmbeddedMuzzleFlashes(Transform root)
            {
                if (root == null)
                    return;

                for (int i = 0; i < root.childCount; i++)
                {
                    Transform child = root.GetChild(i);
                    if (child.name.StartsWith("MuzzleFlash", System.StringComparison.OrdinalIgnoreCase))
                    {
                        child.gameObject.SetActive(false);
                        continue;
                    }

                    DisableEmbeddedMuzzleFlashes(child);
                }
            }

            private static bool ParentMatches(GameObject instance, Transform parent)
            {
                return instance == null || instance.transform.parent == parent;
            }

            private static bool HasParameter(Animator animator, string parameter_name)
            {
                AnimatorControllerParameter[] parameters = animator.parameters;
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i].name == parameter_name)
                        return true;
                }

                return false;
            }

            private static void SetActive(GameObject target, bool is_active)
            {
                if (target != null && target.activeSelf != is_active)
                    target.SetActive(is_active);
            }

            private static void DestroyInstance(GameObject instance)
            {
                if (instance == null)
                    return;

                Object.Destroy(instance);
            }
        }
    }
}
