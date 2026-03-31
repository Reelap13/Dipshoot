using System.Collections.Generic;
using Mirror;
using Server.Data;
using Server.ServerSide;
using UnityEngine;

namespace Server.GameExamples
{
    public class PlayerCharacterSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject _player_character_pref;
        [SerializeField] private Vector2 _x_rand_offset = new Vector2(-10, 10);

        private Dictionary<Player, GameObject> _player_characters = new();

        private void Awake()
        {
            PlayersController.Instance.OnConnected.AddListener(CreatePlayerCharacter);  
            PlayersController.Instance.OnDeleted.AddListener(DeletePlayerCharacter);  
        }

        private void CreatePlayerCharacter(Player player)
        {
            GameObject character = Instantiate(_player_character_pref);
            character.transform.position = Vector3.zero + Vector3.right * Random.Range(_x_rand_offset.x, _x_rand_offset.y);
            character.name = $"Player {player.PlayerId}";
            NetworkServer.Spawn(character);
            player.AddNetworkObject(character.GetComponent<NetworkIdentity>());
            _player_characters.Add(player, character);
        }

        private void DeletePlayerCharacter(Player player)
        {
            Destroy(_player_characters[player].gameObject);
            _player_characters.Remove(player);
        }
    }
}