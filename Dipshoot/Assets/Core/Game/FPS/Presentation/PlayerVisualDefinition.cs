using UnityEngine;
using Game.MatchMode;

namespace Game.Players
{
    [CreateAssetMenu(fileName = "PlayerVisualDefinition", menuName = "Game/Players/PlayerVisualDefinition")]
    public class PlayerVisualDefinition : ScriptableObject
    {
        private const string DefaultSwatPrefabPath = "Presentation/Kinemation/Characters/Swat/Prefabs/Swat_ThirdPerson";
        private const string BlueSwatPrefabPath = "Presentation/Kinemation/Characters/Swat/Prefabs/Swat_ThirdPerson_Blue";
        private const string RedSwatPrefabPath = "Presentation/Kinemation/Characters/Swat/Prefabs/Swat_ThirdPerson_Red";

        [SerializeField] private GameObject _first_person_arms_prefab;
        [SerializeField] private GameObject _third_person_character_prefab;
        [SerializeField] private GameObject _blue_team_third_person_character_prefab;
        [SerializeField] private GameObject _red_team_third_person_character_prefab;
        [SerializeField] private RuntimeAnimatorController _third_person_animator_controller;
        [SerializeField] private PlayerHitboxRigProfile _hitbox_rig_profile;
        [SerializeField] private Vector3 _third_person_local_position = Vector3.zero;
        [SerializeField] private Vector3 _third_person_local_euler_angles = Vector3.zero;
        [SerializeField] private Vector3 _third_person_local_scale = Vector3.one;
        [SerializeField] private string _third_person_weapon_socket_name = "FpsChar_RHand_Bone";
        [SerializeField] private string _first_person_character_layer = "CharacterFirstPerson";
        [SerializeField] private string _third_person_character_layer = "CharacterExternal";
        [SerializeField] private string _first_person_weapon_layer = "WieldablesFirstPerson";
        [SerializeField] private string _third_person_weapon_layer = "WieldablesExternal";

        public GameObject FirstPersonArmsPrefab => _first_person_arms_prefab;
        public GameObject ThirdPersonCharacterPrefab => _third_person_character_prefab;
        public GameObject BlueTeamThirdPersonCharacterPrefab => _blue_team_third_person_character_prefab;
        public GameObject RedTeamThirdPersonCharacterPrefab => _red_team_third_person_character_prefab;
        public RuntimeAnimatorController ThirdPersonAnimatorController => _third_person_animator_controller;
        public PlayerHitboxRigProfile HitboxRigProfile => _hitbox_rig_profile;
        public Vector3 ThirdPersonLocalPosition => _third_person_local_position;
        public Vector3 ThirdPersonLocalEulerAngles => _third_person_local_euler_angles;
        public Vector3 ThirdPersonLocalScale => _third_person_local_scale;
        public string ThirdPersonWeaponSocketName => _third_person_weapon_socket_name;
        public string FirstPersonCharacterLayer => _first_person_character_layer;
        public string ThirdPersonCharacterLayer => _third_person_character_layer;
        public string FirstPersonWeaponLayer => _first_person_weapon_layer;
        public string ThirdPersonWeaponLayer => _third_person_weapon_layer;

        public GameObject GetThirdPersonCharacterPrefab(TeamId team_id)
        {
            if (team_id == TeamId.Blue && _blue_team_third_person_character_prefab != null)
                return _blue_team_third_person_character_prefab;

            if (team_id == TeamId.Red && _red_team_third_person_character_prefab != null)
                return _red_team_third_person_character_prefab;

            if (_third_person_character_prefab != null)
                return _third_person_character_prefab;

            return team_id switch
            {
                TeamId.Blue => Resources.Load<GameObject>(BlueSwatPrefabPath) ?? Resources.Load<GameObject>(DefaultSwatPrefabPath),
                TeamId.Red => Resources.Load<GameObject>(RedSwatPrefabPath) ?? Resources.Load<GameObject>(DefaultSwatPrefabPath),
                _ => Resources.Load<GameObject>(DefaultSwatPrefabPath),
            };
        }
    }
}
