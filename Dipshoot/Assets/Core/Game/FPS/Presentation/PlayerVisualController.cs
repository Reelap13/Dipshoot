using Mirror;
using Scripts.Stats;
using UnityEngine;

namespace Game.Players
{
    [DisallowMultipleComponent]
    public class PlayerVisualController : NetworkBehaviour
    {
        [SerializeField] private PlayerCharacter _character;
        [SerializeField] private PlayerVisualDefinition _definition;
        [SerializeField] private Transform _camera_point;
        [SerializeField] private Transform _visual_root;
        [SerializeField] private Transform _first_person_root;
        [SerializeField] private Transform _third_person_root;
        [SerializeField] private Transform _legacy_render_root;
        [SerializeField] private StatsController _stats;
        [SerializeField] private bool _hide_legacy_renderers = true;
        [SerializeField] private bool _align_third_person_to_capsule_bottom = true;
        [SerializeField] private float _fallback_stand_height = 2f;

        private GameObject _first_person_arms_instance;
        private GameObject _third_person_character_instance;
        private Renderer[] _third_person_renderers;
        private bool _has_visibility_state;
        private bool _last_first_person_visible;
        private bool _last_third_person_visible;
        private bool _has_applied_visual_layers;

        public PlayerVisualDefinition Definition => _definition;
        public Transform FirstPersonRoot => _first_person_root;
        public Transform ThirdPersonRoot => _third_person_root;
        public GameObject FirstPersonArmsInstance => _first_person_arms_instance;
        public bool IsFirstPersonVisible => isClient && isOwned;
        public bool IsThirdPersonVisible => isClient && !isOwned;

        private void Awake()
        {
            CacheReferences();
            EnsureVisualRoots();
            EnsureVisualInstances();
            EnsureWeaponVisualController();
            EnsureAnimationController();
            ApplyVisibility();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            ApplyVisibility();
        }

        public override void OnStartAuthority()
        {
            base.OnStartAuthority();
            ApplyVisibility();
        }

        public override void OnStopAuthority()
        {
            base.OnStopAuthority();
            ApplyVisibility();
        }

        private void Update()
        {
            if (!NeedsVisualInstanceRefresh())
                return;

            EnsureVisualInstances();
            ApplyVisibility();
        }

        private void CacheReferences()
        {
            if (_character == null)
                _character = GetComponent<PlayerCharacter>();

            if (_camera_point == null)
                _camera_point = FindDirectChild(transform, "CameraPoint");

            if (_visual_root == null)
                _visual_root = FindDirectChild(transform, "Visual");

            if (_legacy_render_root == null)
                _legacy_render_root = FindDirectChild(transform, "Model");

            if (_stats == null)
                _stats = GetComponent<StatsController>();
        }

        private void EnsureVisualRoots()
        {
            if (_visual_root == null)
                _visual_root = CreateChild(transform, "Visual");

            Transform first_person_parent = _camera_point == null
                ? _visual_root
                : _camera_point;

            if (_first_person_root == null)
                _first_person_root = FindDirectChild(first_person_parent, "FirstPersonView");

            if (_first_person_root == null)
                _first_person_root = CreateChild(first_person_parent, "FirstPersonView");

            if (_third_person_root == null)
                _third_person_root = FindDirectChild(_visual_root, "ThirdPersonView");

            if (_third_person_root == null)
                _third_person_root = CreateChild(_visual_root, "ThirdPersonView");

            ApplyThirdPersonRootOffset();
        }

        private void EnsureVisualInstances()
        {
            if (_definition == null)
            {
                ApplyLegacyRendererVisibility(true);
                return;
            }

            if (_first_person_arms_instance == null)
                _first_person_arms_instance = FindDirectChildGameObject(_first_person_root, "FirstPersonArms");

            if (_first_person_arms_instance == null && _definition.FirstPersonArmsPrefab != null)
            {
                _first_person_arms_instance = Instantiate(_definition.FirstPersonArmsPrefab, _first_person_root);
                _first_person_arms_instance.name = "FirstPersonArms";
            }

            if (_third_person_character_instance == null)
                _third_person_character_instance = FindDirectChildGameObject(_third_person_root, "ThirdPersonCharacter");

            if (_third_person_character_instance == null && _definition.ThirdPersonCharacterPrefab != null)
            {
                _third_person_character_instance = Instantiate(_definition.ThirdPersonCharacterPrefab, _third_person_root);
                _third_person_character_instance.name = "ThirdPersonCharacter";
                AssignThirdPersonAnimatorController();
                CacheThirdPersonRenderers();
                RebuildHitboxRig();
            }
            else
            {
                AssignThirdPersonAnimatorController();
            }

            if (!_has_applied_visual_layers)
            {
                PlayerVisualLayerUtility.SetLayerRecursive(_first_person_root, LayerMask.NameToLayer(_definition.FirstPersonCharacterLayer));
                PlayerVisualLayerUtility.SetLayerRecursive(_third_person_root, LayerMask.NameToLayer(_definition.ThirdPersonCharacterLayer));
                _has_applied_visual_layers = true;
            }

            if (_third_person_renderers == null)
                CacheThirdPersonRenderers();

            ApplyLegacyRendererVisibility(!HasRenderableVisuals());
        }

        private void EnsureWeaponVisualController()
        {
            if (GetComponent<PlayerWeaponVisualController>() != null)
                return;

            gameObject.AddComponent<PlayerWeaponVisualController>();
        }

        private void EnsureAnimationController()
        {
            if (GetComponent<PlayerAnimationController>() != null)
                return;

            gameObject.AddComponent<PlayerAnimationController>();
        }

        private void RebuildHitboxRig()
        {
            PlayerHitboxRigController hitbox_rig = GetComponent<PlayerHitboxRigController>();
            if (hitbox_rig != null)
                hitbox_rig.Rebuild();
        }

        private void AssignThirdPersonAnimatorController()
        {
            if (_third_person_character_instance == null ||
                _definition == null ||
                _definition.ThirdPersonAnimatorController == null)
            {
                return;
            }

            Animator animator = _third_person_character_instance.GetComponentInChildren<Animator>(true);
            if (animator == null)
                return;

            animator.runtimeAnimatorController = _definition.ThirdPersonAnimatorController;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        private void ApplyVisibility()
        {
            ApplyThirdPersonRootOffset();

            bool show_first_person = IsFirstPersonVisible;
            bool show_third_person = IsThirdPersonVisible;

            if (_has_visibility_state &&
                _last_first_person_visible == show_first_person &&
                _last_third_person_visible == show_third_person)
            {
                return;
            }

            SetActive(_first_person_root, show_first_person);
            SetActive(_third_person_root, true);
            SetThirdPersonRendererVisibility(show_third_person);

            _last_first_person_visible = show_first_person;
            _last_third_person_visible = show_third_person;
            _has_visibility_state = true;
        }

        private void ApplyThirdPersonRootOffset()
        {
            if (!_align_third_person_to_capsule_bottom || _third_person_root == null)
                return;

            Vector3 position = _third_person_root.localPosition;
            position.y = -GetStandHeight() * 0.5f;
            _third_person_root.localPosition = position;
        }

        private float GetStandHeight()
        {
            return _stats == null
                ? _fallback_stand_height
                : Mathf.Max(0.1f, _stats.GetStatValue(Stat.MOVEMENT_STAND_HEIGHT, _fallback_stand_height));
        }

        private void SetThirdPersonRendererVisibility(bool is_visible)
        {
            if (_third_person_renderers == null)
                CacheThirdPersonRenderers();

            if (_third_person_renderers == null)
                return;

            for (int i = 0; i < _third_person_renderers.Length; i++)
            {
                if (_third_person_renderers[i] != null)
                    _third_person_renderers[i].enabled = is_visible;
            }
        }

        private void CacheThirdPersonRenderers()
        {
            _third_person_renderers = _third_person_root == null
                ? null
                : _third_person_root.GetComponentsInChildren<Renderer>(true);
        }

        private bool NeedsVisualInstanceRefresh()
        {
            return _definition != null &&
                ((_first_person_arms_instance == null && _definition.FirstPersonArmsPrefab != null) ||
                (_third_person_character_instance == null && _definition.ThirdPersonCharacterPrefab != null));
        }

        private void ApplyLegacyRendererVisibility(bool is_visible)
        {
            if (!_hide_legacy_renderers || _legacy_render_root == null)
                return;

            Renderer[] renderers = _legacy_render_root.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
                renderer.enabled = is_visible;
        }

        private bool HasRenderableVisuals()
        {
            return HasRenderer(_first_person_root) || HasRenderer(_third_person_root);
        }

        private static bool HasRenderer(Transform root)
        {
            return root != null && root.GetComponentInChildren<Renderer>(true) != null;
        }

        private static void SetActive(Transform target, bool is_active)
        {
            if (target == null || target.gameObject.activeSelf == is_active)
                return;

            target.gameObject.SetActive(is_active);
        }

        private static Transform CreateChild(Transform parent, string name)
        {
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

        private static GameObject FindDirectChildGameObject(Transform parent, string name)
        {
            Transform child = FindDirectChild(parent, name);
            return child == null ? null : child.gameObject;
        }
    }
}
