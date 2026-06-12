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
        [SerializeField] private SpectatorPawn _spectator_prefab;
        [SerializeField] private float _respawn_delay = 2f;
        [SerializeField] private float _spawn_occupied_radius = 1.25f;
        [SerializeField] private float _spawn_slot_spacing = 1f;
        [SerializeField] private float _spawn_height_offset = 1f;

        private readonly Dictionary<int, PlayerCharacter> _characters = new();
        private readonly Dictionary<int, SpectatorPawn> _spectators = new();
        private readonly Dictionary<PlayerHealth, int> _health_owners = new();
        private readonly Dictionary<uint, int> _net_id_owners = new();
        private readonly Dictionary<int, Coroutine> _respawn_coroutines = new();
        private readonly Dictionary<int, Coroutine> _spawn_wait_coroutines = new();
        private readonly List<Vector3> _reserved_spawn_positions = new();
        private static readonly Vector2[] SpawnSlotOffsets =
        {
            Vector2.zero,
            new(-1f, -1f),
            new(1f, -1f),
            new(-1f, 1f),
            new(1f, 1f)
        };

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

            _reserved_spawn_positions.Clear();

            foreach (var lobby_player in GameController.MatchController.MatchData.LobbyData.Players)
            {
                TeamId team_id = _team_roster.GetTeam(lobby_player.PlayerId);
                _stats_controller?.RegisterPlayer(lobby_player.PlayerId, team_id, lobby_player.Nickname);
                if (team_id == TeamId.Spectator)
                {
                    SpawnOrRespawnSpectator(lobby_player.PlayerId);
                    continue;
                }

                SpawnOrRespawnPlayer(lobby_player.PlayerId, team_id);
            }

            _reserved_spawn_positions.Clear();
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
            foreach (Coroutine coroutine in _spawn_wait_coroutines.Values)
                StopCoroutine(coroutine);

            _respawn_coroutines.Clear();
            _spawn_wait_coroutines.Clear();
        }

        private void SpawnOrRespawnPlayer(int player_id, TeamId team_id)
        {
            Transform spawn_point = GameController.LevelCreator.Level.GetRandomSpawnPoint(team_id);
            Quaternion spawn_rotation = spawn_point == null ? Quaternion.identity : spawn_point.rotation;
            if (!TryGetFreeSpawnPosition(player_id, spawn_point, out Vector3 spawn_position))
            {
                QueueSpawn(player_id, team_id);
                return;
            }

            if (_characters.TryGetValue(player_id, out PlayerCharacter character) && character != null)
            {
                character.GetComponent<PlayerMatchIdentity>()?.InitializeServer(player_id, team_id);
                character.Health.Respawn(spawn_position, spawn_rotation);
                RegisterCharacter(player_id, character);
                ReserveSpawnPosition(spawn_position);
                return;
            }

            Player player = PlayersController.Instance.GetPlayer(player_id);
            character = NetworkUtils.NetworkMatchInstantiate(
                _character_prefab,
                GameController.Scene,
                GameController.MatchId,
                spawn_position,
                spawn_rotation,
                transform);

            player.AddNetworkObject(character.netIdentity);
            character.GetComponent<PlayerMatchIdentity>()?.InitializeServer(player_id, team_id);
            character.InitializeServerSimulation(GameController.TickManager);
            RegisterCharacter(player_id, character);
            ReserveSpawnPosition(spawn_position);
        }

        private void SpawnOrRespawnSpectator(int player_id)
        {
            Vector3 position = GetSpectatorSpawnPosition();
            Quaternion rotation = Quaternion.Euler(35f, 0f, 0f);

            if (_spectators.TryGetValue(player_id, out SpectatorPawn spectator) && spectator != null)
            {
                spectator.InitializeServer(player_id, TeamId.Spectator);
                spectator.transform.SetPositionAndRotation(position, rotation);
                return;
            }

            if (_spectator_prefab == null)
            {
                Debug.LogError($"{nameof(PlayerSpawnController)} spectator prefab is not assigned.", this);
                return;
            }

            Player player = PlayersController.Instance.GetPlayer(player_id);
            spectator = NetworkUtils.NetworkMatchInstantiate(
                _spectator_prefab,
                GameController.Scene,
                GameController.MatchId,
                position,
                rotation,
                transform);

            player.AddNetworkObject(spectator.netIdentity);
            spectator.InitializeServer(player_id, TeamId.Spectator);
            _spectators[player_id] = spectator;
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
            if (TryGetFreeSpawnPosition(player_id, spawn_point, out Vector3 spawn_position))
            {
                character.Health.Respawn(
                    spawn_position,
                    spawn_point == null ? Quaternion.identity : spawn_point.rotation);
                ReserveSpawnPosition(spawn_position);
                yield break;
            }

            QueueSpawn(player_id, team_id);
        }

        private void QueueSpawn(int player_id, TeamId team_id)
        {
            if (_spawn_wait_coroutines.ContainsKey(player_id))
                return;

            _spawn_wait_coroutines[player_id] = StartCoroutine(WaitAndSpawnPlayer(player_id, team_id));
        }

        private IEnumerator WaitAndSpawnPlayer(int player_id, TeamId team_id)
        {
            while (true)
            {
                Transform spawn_point = GameController.LevelCreator.Level.GetRandomSpawnPoint(team_id);
                if (TryGetFreeSpawnPosition(player_id, spawn_point, out Vector3 spawn_position))
                {
                    _spawn_wait_coroutines.Remove(player_id);
                    SpawnOrRespawnPlayerAt(player_id, team_id, spawn_point, spawn_position);
                    yield break;
                }

                yield return null;
            }
        }

        private void SpawnOrRespawnPlayerAt(
            int player_id,
            TeamId team_id,
            Transform spawn_point,
            Vector3 spawn_position)
        {
            Quaternion spawn_rotation = spawn_point == null ? Quaternion.identity : spawn_point.rotation;
            if (_characters.TryGetValue(player_id, out PlayerCharacter character) && character != null)
            {
                character.GetComponent<PlayerMatchIdentity>()?.InitializeServer(player_id, team_id);
                character.Health.Respawn(spawn_position, spawn_rotation);
                RegisterCharacter(player_id, character);
                ReserveSpawnPosition(spawn_position);
                return;
            }

            Player player = PlayersController.Instance.GetPlayer(player_id);
            character = NetworkUtils.NetworkMatchInstantiate(
                _character_prefab,
                GameController.Scene,
                GameController.MatchId,
                spawn_position,
                spawn_rotation,
                transform);

            player.AddNetworkObject(character.netIdentity);
            character.GetComponent<PlayerMatchIdentity>()?.InitializeServer(player_id, team_id);
            character.InitializeServerSimulation(GameController.TickManager);
            RegisterCharacter(player_id, character);
            ReserveSpawnPosition(spawn_position);
        }

        private bool TryGetFreeSpawnPosition(int player_id, Transform spawn_point, out Vector3 spawn_position)
        {
            spawn_position = spawn_point == null ? transform.position : spawn_point.position;
            spawn_position.y += _spawn_height_offset;
            if (spawn_point == null || GameController?.LevelCreator?.Level == null)
                return !IsSpawnPositionOccupied(player_id, spawn_position);

            for (int i = 0; i < SpawnSlotOffsets.Length; i++)
            {
                Vector2 offset = SpawnSlotOffsets[i] * _spawn_slot_spacing;
                Vector3 local_offset = new(offset.x, 0f, offset.y);
                Vector3 candidate = spawn_point.position + spawn_point.TransformDirection(local_offset);
                candidate.y = spawn_point.position.y + _spawn_height_offset;

                if (IsSpawnPositionOccupied(player_id, candidate))
                    continue;

                spawn_position = candidate;
                return true;
            }

            return false;
        }

        private bool IsSpawnPositionOccupied(int player_id, Vector3 position)
        {
            float radius_sqr = _spawn_occupied_radius * _spawn_occupied_radius;

            for (int i = 0; i < _reserved_spawn_positions.Count; i++)
            {
                if (HorizontalSqrDistance(position, _reserved_spawn_positions[i]) <= radius_sqr)
                    return true;
            }

            foreach (KeyValuePair<int, PlayerCharacter> pair in _characters)
            {
                if (pair.Key == player_id || pair.Value == null || pair.Value.Health == null || !pair.Value.Health.IsAlive)
                    continue;

                if (HorizontalSqrDistance(position, pair.Value.transform.position) <= radius_sqr)
                    return true;
            }

            return false;
        }

        private void ReserveSpawnPosition(Vector3 position)
        {
            _reserved_spawn_positions.Add(position);
            StartCoroutine(ReleaseReservedSpawnPosition(position));
        }

        private IEnumerator ReleaseReservedSpawnPosition(Vector3 position)
        {
            yield return null;
            _reserved_spawn_positions.Remove(position);
        }

        private Vector3 GetSpectatorSpawnPosition()
        {
            Transform capture_point = GameController?.LevelCreator?.Level?.CapturePoint;
            if (capture_point != null)
                return capture_point.position + Vector3.up * 18f;

            return transform.position + Vector3.up * 18f;
        }

        private static float HorizontalSqrDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return dx * dx + dz * dz;
        }
    }
}
