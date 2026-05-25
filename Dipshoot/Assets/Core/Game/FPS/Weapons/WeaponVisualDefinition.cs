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
        [SerializeField] private RuntimeAnimatorController _first_person_animator_controller;
        [SerializeField] private RuntimeAnimatorController _third_person_animator_controller;
        [SerializeField] private string _muzzle_socket_name = "MuzzleSocket";

        public WeaponSlot Slot => _slot;
        public GameObject FirstPersonPrefab => _first_person_prefab;
        public GameObject ThirdPersonPrefab => _third_person_prefab;
        public GameObject MuzzleFlashPrefab => _muzzle_flash_prefab;
        public RuntimeAnimatorController FirstPersonAnimatorController => _first_person_animator_controller;
        public RuntimeAnimatorController ThirdPersonAnimatorController => _third_person_animator_controller;
        public string MuzzleSocketName => _muzzle_socket_name;
    }
}
