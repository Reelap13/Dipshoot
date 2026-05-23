using System.Collections;
using System.Collections.Generic;
using Game.Players;
using Mirror;
using Server.Data;
using Server.ServerSide;
using UnityEngine;

namespace Game.MatchMode
{
    public class PlayerSpawnController : MonoBehaviour
    {
        [field: SerializeField]
        public DipshootGameController GameController { get; private set; }
        [SerializeField] private PlayerCharacter _character_prefab;
        [SerializeField] private float _respawn_delay = 2f;

        private readonly Dictionary<int, PlayerCharacter> _characters = new();
        private readonly Dictionary<PlayerHealth, int> _health_owners = new();
        private readonly Dictionary<uint, int> _net_id_owners = new();
        private readonly Dictionary<int, Coroutine> _respawn_coroutines = new();

        private TeamRoster _team_roster;
        private MatchStatsController _stats_controller;

        public bool IsRespawnEnabled { get; set; }

        public void Initialize(
            DipshootGameController game_controller,
            TeamRoster team_roster,
            MatchStatsController stats_controller)
        {
            GameController = game_controller;
            _team_roster = team_roster;
            _stats_controller = stats_controller;
        }

        public IEnumerable<PlayerCharacter> ActiveCharacters => _characters.Values;

        public void SpawnRoundPlayers()
        {
            if (GameController == null || _team_roster == null)
                return;

            foreach (var lobby_player in GameController.MatchController.MatchData.LobbyData.Players)
            {
                TeamId team_id = _team_roster.GetTeam(lobby_player.PlayerId);
                _stats_controller?.RegisterPlayer(lobby_player.PlayerId, team_id);
                SpawnOrRespawnPlayer(lobby_player.PlayerId, team_id);
            }
        }

        public TeamId GetTeam(PlayerCharacter character)
        {
            if (character == null)
                return TeamId.None;

            PlayerMatchIdentity identity = character.GetComponent<PlayerMatchIdentity>();
            return identity == null ? TeamId.None : identity.TeamId;
        }

        public int GetPlayerId(PlayerCharacter character)
        {
            if (character == null)
                return -1;

            PlayerMatchIdentity identity = character.GetComponent<PlayerMatchIdentity>();
            return identity == null ? -1 : identity.PlayerId;
        }

        public bool TryGetPlayerId(uint net_id, out int player_id)
        {
            return _net_id_owners.TryGetValue(net_id, out player_id);
        }

        public void StopRespawns()
        {
            IsRespawnEnabled = false;

            foreach (Coroutine coroutine in _respawn_coroutines.Values)
                StopCoroutine(coroutine);

            _respawn_coroutines.Clear();
        }

        private void SpawnOrRespawnPlayer(int player_id, TeamId team_id)
        {
            Transform spawn_point = GameController.LevelCreator.Level.GetRandomSpawnPoint(team_id);

            if (_characters.TryGetValue(player_id, out PlayerCharacter character) && character != null)
            {
                character.GetComponent<PlayerMatchIdentity>()?.InitializeServer(player_id, team_id);
                character.Health.Respawn(spawn_point);
                RegisterCharacter(player_id, character);
                return;
            }

            Player player = PlayersController.Instance.GetPlayer(player_id);
            character = NetworkUtils.NetworkMatchInstantiate(
                _character_prefab,
                GameController.Scene,
                GameController.MatchId,
                spawn_point,
                transform);

            player.AddNetworkObject(character.netIdentity);
            character.GetComponent<PlayerMatchIdentity>()?.InitializeServer(player_id, team_id);
            character.InitializeServerSimulation(GameController.TickManager);
            RegisterCharacter(player_id, character);
        }

        private void RegisterCharacter(int player_id, PlayerCharacter character)
        {
            _characters[player_id] = character;
            _net_id_owners[character.netId] = player_id;

            if (character.Health == null)
                return;

            if (!_health_owners.ContainsKey(character.Health))
            {
                character.Health.OnDamageApplied += HandleDamageApplied;
                character.Health.OnDied += HandlePlayerDied;
            }

            _health_owners[character.Health] = player_id;
        }

        private void HandleDamageApplied(PlayerHealth health, DamageInfo damage_info)
        {
            if (health == null || !_health_owners.TryGetValue(health, out int victim_player_id))
                return;

            if (!TryGetPlayerId(damage_info.SourceNetId, out int attacker_player_id))
                return;

            _stats_controller?.RegisterDamage(attacker_player_id, victim_player_id, damage_info.Damage);
        }

        private void HandlePlayerDied(PlayerHealth health, DamageInfo damage_info)
        {
            if (health == null || !_health_owners.TryGetValue(health, out int victim_player_id))
                return;

            if (TryGetPlayerId(damage_info.SourceNetId, out int killer_player_id))
                _stats_controller?.RegisterDeath(killer_player_id, victim_player_id);

            if (!IsRespawnEnabled || _respawn_coroutines.ContainsKey(victim_player_id))
                return;

            _respawn_coroutines.Add(victim_player_id, StartCoroutine(RespawnPlayer(victim_player_id)));
        }

        private IEnumerator RespawnPlayer(int player_id)
        {
            yield return new WaitForSeconds(_respawn_delay);

            _respawn_coroutines.Remove(player_id);

            if (!IsRespawnEnabled || !_characters.TryGetValue(player_id, out PlayerCharacter character))
                yield break;

            TeamId team_id = _team_roster.GetTeam(player_id);
            Transform spawn_point = GameController.LevelCreator.Level.GetRandomSpawnPoint(team_id);
            character.Health.Respawn(spawn_point);
        }
    }
}
