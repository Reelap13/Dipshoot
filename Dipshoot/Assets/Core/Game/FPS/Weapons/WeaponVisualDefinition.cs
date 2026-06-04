using UnityEngine;

namespace Game.Players
{
    [CreateAssetMenu(fileName = "WeaponVisualDefinition", menuName = "Game/Weapons/WeaponVisualDefinition")]
    public class WeaponVisualDefinition : ScriptableObject
    {
        [SerializeField] private WeaponSlot _slot = WeaponSlot.Primary;
        [SerializeField] private GameObject _first_person_prefab;
        [SerializeField] private GameObject _third_person_prefab;
        [SerializeField] private GameObject _muzzle_flash_prefab;
        [SerializeField] private GameObject _tracer_prefab;
        [SerializeField] private GameObject _world_impact_prefab;
        [SerializeField] private GameObject _player_impact_prefab;
        [SerializeField] private WeaponAudioDefinition _audio;
        [SerializeField] private RuntimeAnimatorController _first_person_animator_controller;
        [SerializeField] private RuntimeAnimatorController _third_person_animator_controller;
        [SerializeField] private string _muzzle_socket_name = "MuzzleSocket";
        [SerializeField] private float _tracer_speed = 240f;
        [SerializeField] private float _tracer_visual_speed = 420f;
        [SerializeField] private float _tracer_lifetime = 0.08f;
        [SerializeField] private float _tracer_min_visible_time = 0.045f;
        [SerializeField] private float _tracer_fade_time = 0.08f;
        [SerializeField] private float _tracer_length = 2.2f;
        [SerializeField] private float _tracer_start_width = 0.018f;
        [SerializeField] private float _tracer_end_width = 0.004f;
        [SerializeField] private Color _tracer_color = new(1f, 0.86f, 0.45f, 1f);
        [SerializeField] private float _impact_lifetime = 1.5f;
        [SerializeField] private Vector3 _first_person_local_position = new(0f, -0.32f, 0.32f);
        [SerializeField] private Vector3 _first_person_local_euler_angles = Vector3.zero;
        [SerializeField] private Vector3 _first_person_local_scale = Vector3.one;
        [SerializeField] private Vector3 _third_person_local_position = Vector3.zero;
        [SerializeField] private Vector3 _third_person_local_euler_angles = Vector3.zero;
        [SerializeField] private Vector3 _third_person_local_scale = Vector3.one;

        public WeaponSlot Slot => _slot;
        public GameObject FirstPersonPrefab => _first_person_prefab;
        public GameObject ThirdPersonPrefab => _third_person_prefab;
        public GameObject MuzzleFlashPrefab => _muzzle_flash_prefab;
        public GameObject TracerPrefab => _tracer_prefab;
        public GameObject WorldImpactPrefab => _world_impact_prefab;
        public GameObject PlayerImpactPrefab => _player_impact_prefab;
        public WeaponAudioDefinition Audio => _audio;
        public RuntimeAnimatorController FirstPersonAnimatorController => _first_person_animator_controller;
        public RuntimeAnimatorController ThirdPersonAnimatorController => _third_person_animator_controller;
        public string MuzzleSocketName => _muzzle_socket_name;
        public float TracerSpeed => _tracer_speed;
        public float TracerVisualSpeed => _tracer_visual_speed;
        public float TracerLifetime => _tracer_lifetime;
        public float TracerMinVisibleTime => _tracer_min_visible_time;
        public float TracerFadeTime => _tracer_fade_time;
        public float TracerLength => _tracer_length;
        public float TracerStartWidth => _tracer_start_width;
        public float TracerEndWidth => _tracer_end_width;
        public Color TracerColor => _tracer_color;
        public float ImpactLifetime => _impact_lifetime;
        public Vector3 FirstPersonLocalPosition => _first_person_local_position;
        public Vector3 FirstPersonLocalEulerAngles => _first_person_local_euler_angles;
        public Vector3 FirstPersonLocalScale => _first_person_local_scale;
        public Vector3 ThirdPersonLocalPosition => _third_person_local_position;
        public Vector3 ThirdPersonLocalEulerAngles => _third_person_local_euler_angles;
        public Vector3 ThirdPersonLocalScale => _third_person_local_scale;
    }
}
