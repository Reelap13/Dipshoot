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
        [SerializeField] private WeaponAudioDefinition _audio;
        [SerializeField] private RuntimeAnimatorController _first_person_animator_controller;
        [SerializeField] private RuntimeAnimatorController _third_person_animator_controller;
        [SerializeField] private string _muzzle_socket_name = "MuzzleSocket";
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
        public WeaponAudioDefinition Audio => _audio;
        public RuntimeAnimatorController FirstPersonAnimatorController => _first_person_animator_controller;
        public RuntimeAnimatorController ThirdPersonAnimatorController => _third_person_animator_controller;
        public string MuzzleSocketName => _muzzle_socket_name;
        public Vector3 FirstPersonLocalPosition => _first_person_local_position;
        public Vector3 FirstPersonLocalEulerAngles => _first_person_local_euler_angles;
        public Vector3 FirstPersonLocalScale => _first_person_local_scale;
        public Vector3 ThirdPersonLocalPosition => _third_person_local_position;
        public Vector3 ThirdPersonLocalEulerAngles => _third_person_local_euler_angles;
        public Vector3 ThirdPersonLocalScale => _third_person_local_scale;
    }
}
