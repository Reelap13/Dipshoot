using UnityEngine;

namespace Game.Players
{
    [CreateAssetMenu(fileName = "PlayerVisualDefinition", menuName = "Game/Players/PlayerVisualDefinition")]
    public class PlayerVisualDefinition : ScriptableObject
    {
        [SerializeField] private GameObject _first_person_arms_prefab;
        [SerializeField] private GameObject _third_person_character_prefab;
        [SerializeField] private string _first_person_character_layer = "CharacterFirstPerson";
        [SerializeField] private string _third_person_character_layer = "CharacterExternal";
        [SerializeField] private string _first_person_weapon_layer = "WieldablesFirstPerson";
        [SerializeField] private string _third_person_weapon_layer = "WieldablesExternal";

        public GameObject FirstPersonArmsPrefab => _first_person_arms_prefab;
        public GameObject ThirdPersonCharacterPrefab => _third_person_character_prefab;
        public string FirstPersonCharacterLayer => _first_person_character_layer;
        public string ThirdPersonCharacterLayer => _third_person_character_layer;
        public string FirstPersonWeaponLayer => _first_person_weapon_layer;
        public string ThirdPersonWeaponLayer => _third_person_weapon_layer;
    }
}
