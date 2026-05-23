using System.Collections;
using System.Collections.Generic;
using Game.Players;
using Mirror;
using Server.Data;
using Server.Match;
using Server.ServerSide;
using UnityEngine;

namespace Game
{
    public class PlayersSpawner : MonoBehaviour
    {
        [field: SerializeField]
        public DipshootGameController GameController { get; private set; }
        [SerializeField] private PlayerCharacter _character_prefab;
        [SerializeField] private float _respawn_delay = 2f;

        private readonly Dictionary<PlayerHealth, Coroutine> _respawn_coroutines = new();

        private void Awake()
        {
        }

        private void SpawnPlayersCharacters()
        {
            foreach (var lobby_player in GameController.MatchController.MatchData.LobbyData.Players)
            {
                Player player = PlayersController.Instance.GetPlayer(lobby_player.PlayerId);
                PlayerCharacter character = NetworkUtils.NetworkMatchInstantiate(
                    _character_prefab, GameController.Scene, GameController.MatchId, 
                    GameController.LevelCreator.Level.GetRandomSpawnPoint(), transform);
                player.AddNetworkObject(character.netIdentity);
                character.InitializeServerSimulation(GameController.TickManager);
                SubscribeRespawn(character);
            }
        }

        private void SubscribeRespawn(PlayerCharacter character)
        {
            if (character == null || character.Health == null)
                return;

            character.Health.OnDied += HandlePlayerDied;
        }

        private void HandlePlayerDied(PlayerHealth health, DamageInfo damage_info)
        {
            if (health == null || _respawn_coroutines.ContainsKey(health))
                return;

            _respawn_coroutines.Add(health, StartCoroutine(RespawnPlayer(health)));
        }

        private IEnumerator RespawnPlayer(PlayerHealth health)
        {
            yield return new WaitForSeconds(_respawn_delay);

            _respawn_coroutines.Remove(health);

            if (health == null)
                yield break;

            Transform spawn_point = GameController.LevelCreator.Level.GetRandomSpawnPoint();
            health.Respawn(spawn_point);
        }
    }
}
