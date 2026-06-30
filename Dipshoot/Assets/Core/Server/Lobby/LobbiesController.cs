using System.Collections.Generic;
using Game.MatchConfig;
using Game.MatchMode;
using Server.Data;
using Server.Match;
using Server.PlayerHub;
using Server.ServerSide;
using UnityEngine;

namespace Server.Lobby
{
    public class LobbiesController : Singleton<LobbiesController>
    {
        private const string LobbyCodeCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        [SerializeField] private int _default_lobby_map = 0;
        [SerializeField] private int _default_lobby_capacity = 16;

        private int _id = 0;
        private Dictionary<string, int> _lobbies_codes = new();
        private Dictionary<int, LobbyData> _lobbies_data = new();
        private Dictionary<int, List<PlayerHubController>> _lobbies_players = new();
        private Dictionary<int, int> _player_lobbies = new();
        private Dictionary<int, PlayerHubController> _player_hubs = new();
        private List<int> _public_lobby_queue = new();
        private List<MatchPreset> _preset_rotation = new();
        private int _preset_rotation_index;
        private string _last_preset_id;

        private void Awake()
        {
            PlayersController players_controller = PlayersController.Instance;
            players_controller.OnConnected.AddListener(ProcessPlayerConnectionChanged);
            players_controller.OnReconnected.AddListener(ProcessPlayerConnectionChanged);
            players_controller.OnDisconnected.AddListener(ProcessPlayerDisconnection);
            players_controller.OnDeleted.AddListener(ProcessPlayerDeletion);
        }

        private void OnDestroy()
        {
            PlayersController players_controller = PlayersController.Instance;
            if (players_controller == null)
                return;

            players_controller.OnConnected.RemoveListener(ProcessPlayerConnectionChanged);
            players_controller.OnReconnected.RemoveListener(ProcessPlayerConnectionChanged);
            players_controller.OnDisconnected.RemoveListener(ProcessPlayerDisconnection);
            players_controller.OnDeleted.RemoveListener(ProcessPlayerDeletion);
        }

        public void RegisterPlayerHub(PlayerHubController player_hub)
        {
            if (player_hub?.Player == null)
                return;

            _player_hubs[player_hub.Player.PlayerId] = player_hub;
            player_hub.UpdatePopulationData(CreatePopulationSnapshot());
        }

        public bool IsPlayerInLobby(PlayerHubController player, LobbyData lobby)
        {
            if (player == null || lobby == null || player.Player == null)
                return false;

            return _lobbies_data.TryGetValue(lobby.Id, out LobbyData active_lobby) &&
                _player_lobbies.TryGetValue(player.Player.PlayerId, out int lobby_id) &&
                lobby_id == lobby.Id &&
                active_lobby.GetPlayer(player.Player.PlayerId) != null &&
                _lobbies_players.TryGetValue(lobby.Id, out List<PlayerHubController> players) &&
                players.Contains(player);
        }

        public void CreateLobby(PlayerHubController player, string lobby_code)
        {
            if (player?.Player == null || _player_lobbies.ContainsKey(player.Player.PlayerId))
            {
                player?.RegisterError("Player is already in a lobby");
                return;
            }

            if (_lobbies_codes.ContainsKey(lobby_code))
            {
                player.RegisterError("Lobby code alrady exists");
                return;
            }

            CreateLobbyInternal(player, lobby_code, LobbyAccessMode.Private);
        }

        public void FindOrCreatePublicLobby(PlayerHubController player)
        {
            if (player?.Player == null || _player_lobbies.ContainsKey(player.Player.PlayerId))
            {
                player?.RegisterError("Player is already in a lobby");
                return;
            }

            LobbyData lobby = FindAvailablePublicLobby();
            if (lobby != null)
            {
                EnterToLobbyInternal(player, lobby);
                return;
            }

            CreateLobbyInternal(player, GenerateLobbyCode(), LobbyAccessMode.Public);
        }

        public void OpenLobby(PlayerHubController player, int lobby_id)
        {
            if (!_lobbies_data.TryGetValue(lobby_id, out LobbyData lobby))
            {
                player.RegisterError("Lobby doesn't exist");
                return;
            }

            LobbyPlayerData player_data = lobby.GetPlayer(player.Player.PlayerId);
            if (player_data == null || player_data.Type != LobbyPlayerType.HOST)
            {
                player.RegisterError("Only the host can open the lobby");
                return;
            }

            if (lobby.IsConnectionBlocked)
            {
                player.RegisterError("Lobby has already started");
                return;
            }

            if (lobby.AccessMode == LobbyAccessMode.Public)
                return;

            lobby.AccessMode = LobbyAccessMode.Public;
            _public_lobby_queue.Add(lobby.Id);
            UpdateClientsData(lobby.Id);
            NotifyPopulationChanged();
        }

        public void EnterToLobby(PlayerHubController player, string lobby_code)
        {
            if (!_lobbies_codes.TryGetValue(lobby_code, out var lobby_id))
            {
                player.RegisterError("Lobby doesn't exist");
                return;
            }

            LobbyData lobby = _lobbies_data[lobby_id];
            if (lobby.IsConnectionBlocked)
            {
                player.RegisterError("Lobby is blocked");
                return;
            }
            if (!lobby.IsHasPlace())
            {
                player.RegisterError("Lobby is overfull");
                return;
            }

            if (_player_lobbies.ContainsKey(player.Player.PlayerId))
            {
                player.RegisterError("Player is already in a lobby");
                return;
            }

            EnterToLobbyInternal(player, lobby);
        }

        public void LeaveFromLobby(PlayerHubController player, int lobby_id)
        {
            if (!_lobbies_data.TryGetValue(lobby_id, out var lobby))
            {
                player.UpdateLobbyData(null);
                return;
            }

            RemovePlayerFromWaitingLobby(player.Player.PlayerId, player, lobby);
        }

        public void LeaveStartedLobby(PlayerHubController player, int lobby_id)
        {
            if (player == null)
                return;

            if (!_lobbies_data.TryGetValue(lobby_id, out var lobby))
            {
                player.UpdateLobbyData(null);
                return;
            }

            int player_id = player.Player.PlayerId;
            lobby.RemovePlayer(player_id);
            if (_lobbies_players.TryGetValue(lobby.Id, out List<PlayerHubController> players))
                players.Remove(player);

            _player_lobbies.Remove(player_id);
            player.UpdateLobbyData(null);
            NotifyPopulationChanged();
        }

        public void StartGame(PlayerHubController player, int lobby_id)
        {
            if (!_lobbies_data.TryGetValue(lobby_id, out var lobby))
            {
                player.RegisterError("Lobby doesn't exist");
                return;
            }

            LobbyPlayerData player_data = lobby.GetPlayer(player.Player.PlayerId);
            if (player_data == null || player_data.Type != LobbyPlayerType.HOST)
            {
                player.RegisterError("Error 11: Attempt to start lobby without host role");
                return;
            }

            MatchPreset preset = GetNextRandomPreset();
            if (preset == null)
            {
                player.RegisterError("No match presets are configured");
                return;
            }

            ApplyPreset(lobby, preset);
            lobby.IsTutorialMode = false;
            lobby.IsConnectionBlocked = true;
            _public_lobby_queue.Remove(lobby.Id);
            UpdateClientsData(lobby.Id);
            MatchesContoller.Instance.StartMatch(lobby);
        }

        public void SwitchTeam(PlayerHubController player, int lobby_id)
        {
            if (!_lobbies_data.TryGetValue(lobby_id, out var lobby))
            {
                player.RegisterError("Lobby doesn't exist");
                return;
            }

            LobbyPlayerData player_data = lobby.GetPlayer(player.Player.PlayerId);
            if (player_data == null)
                return;

            player_data.Team = GetNextTeam(player_data.Team);
            UpdateClientsData(lobby.Id);
        }

        public void SwitchPlayerTeam(PlayerHubController player, int lobby_id, int target_player_id)
        {
            if (!_lobbies_data.TryGetValue(lobby_id, out var lobby))
            {
                player.RegisterError("Lobby doesn't exist");
                return;
            }

            LobbyPlayerData requester = lobby.GetPlayer(player.Player.PlayerId);
            if (requester == null || requester.Type != LobbyPlayerType.HOST)
            {
                player.RegisterError("Error 14: Attempt to switch player team without host role");
                return;
            }

            LobbyPlayerData target = lobby.GetPlayer(target_player_id);
            if (target == null || target.PlayerId == player.Player.PlayerId)
                return;

            target.Team = GetNextTeam(target.Team);
            UpdateClientsData(lobby.Id);
        }

        public void SelectPreset(PlayerHubController player, int lobby_id, string preset_id)
        {
            if (!_lobbies_data.TryGetValue(lobby_id, out var lobby))
            {
                player.RegisterError("Lobby doesn't exist");
                return;
            }

            LobbyPlayerData player_data = lobby.GetPlayer(player.Player.PlayerId);
            if (player_data == null || player_data.Type != LobbyPlayerType.HOST)
            {
                player.RegisterError("Error 12: Attempt to select preset without host role");
                return;
            }

            MatchPreset preset = MatchPresetRegistry.GetPreset(preset_id);
            if (preset == null)
            {
                player.RegisterError("Match preset doesn't exist");
                return;
            }

            ApplyPreset(lobby, preset);
            UpdateClientsData(lobby.Id);
        }

        public void SetTutorialMode(PlayerHubController player, int lobby_id, bool enabled)
        {
            if (!_lobbies_data.TryGetValue(lobby_id, out var lobby))
            {
                player.RegisterError("Lobby doesn't exist");
                return;
            }

            LobbyPlayerData player_data = lobby.GetPlayer(player.Player.PlayerId);
            if (player_data == null || player_data.Type != LobbyPlayerType.HOST)
            {
                player.RegisterError("Error 13: Attempt to set tutorial mode without host role");
                return;
            }

            lobby.IsTutorialMode = enabled;
            UpdateClientsData(lobby.Id);
        }

        public void NotifyPopulationChanged()
        {
            ServerPopulationSnapshot snapshot = CreatePopulationSnapshot();
            foreach (PlayerHubController player_hub in _player_hubs.Values)
            {
                if (player_hub?.Player != null && player_hub.Player.IsHasClient())
                    player_hub.UpdatePopulationData(snapshot);
            }
        }

        private void CreateLobbyInternal(
            PlayerHubController player,
            string lobby_code,
            LobbyAccessMode access_mode)
        {
            LobbyData lobby = new(
                _id++,
                lobby_code,
                _default_lobby_capacity,
                _default_lobby_map,
                access_mode);
            lobby.AddPlayer(new LobbyPlayerData(player.Player, LobbyPlayerType.HOST, TeamId.Red));

            _lobbies_codes.Add(lobby.Code, lobby.Id);
            _lobbies_data.Add(lobby.Id, lobby);
            _lobbies_players.Add(lobby.Id, new List<PlayerHubController> { player });
            _player_lobbies[player.Player.PlayerId] = lobby.Id;

            if (access_mode == LobbyAccessMode.Public)
                _public_lobby_queue.Add(lobby.Id);

            UpdateClientsData(lobby.Id);
            NotifyPopulationChanged();
        }

        private void EnterToLobbyInternal(PlayerHubController player, LobbyData lobby)
        {
            lobby.AddPlayer(new LobbyPlayerData(
                player.Player,
                LobbyPlayerType.CLIENT,
                GetDefaultTeam(lobby)));
            _lobbies_players[lobby.Id].Add(player);
            _player_lobbies[player.Player.PlayerId] = lobby.Id;

            UpdateClientsData(lobby.Id);
            NotifyPopulationChanged();
        }

        private LobbyData FindAvailablePublicLobby()
        {
            for (int i = 0; i < _public_lobby_queue.Count;)
            {
                int lobby_id = _public_lobby_queue[i];
                if (!_lobbies_data.TryGetValue(lobby_id, out LobbyData lobby) ||
                    lobby.AccessMode != LobbyAccessMode.Public ||
                    lobby.IsConnectionBlocked)
                {
                    _public_lobby_queue.RemoveAt(i);
                    continue;
                }

                if (lobby.IsHasPlace())
                    return lobby;

                i++;
            }

            return null;
        }

        private void RemovePlayerFromWaitingLobby(
            int player_id,
            PlayerHubController player_hub,
            LobbyData lobby)
        {
            LobbyPlayerData player_data = lobby.RemovePlayer(player_id);
            if (player_data == null)
            {
                _player_lobbies.Remove(player_id);
                player_hub?.UpdateLobbyData(null);
                return;
            }

            if (_lobbies_players.TryGetValue(lobby.Id, out List<PlayerHubController> players))
            {
                players.RemoveAll(candidate =>
                    candidate == null ||
                    candidate == player_hub ||
                    candidate.Player?.PlayerId == player_id);
            }

            _player_lobbies.Remove(player_id);
            player_hub?.UpdateLobbyData(null);

            if (lobby.Players.Count == 0)
            {
                DestroyLobby(lobby.Id);
                return;
            }

            if (player_data.Type == LobbyPlayerType.HOST && !TryTransferHost(lobby))
            {
                DestroyLobby(lobby.Id);
                return;
            }

            UpdateClientsData(lobby.Id);
            NotifyPopulationChanged();
        }

        private bool TryTransferHost(LobbyData lobby)
        {
            for (int i = 0; i < lobby.Players.Count; i++)
                lobby.Players[i].Type = LobbyPlayerType.CLIENT;

            for (int i = 0; i < lobby.Players.Count; i++)
            {
                LobbyPlayerData candidate = lobby.Players[i];
                Player player = PlayersController.Instance.GetPlayer(candidate.PlayerId);
                if (player == null || !player.IsHasClient())
                    continue;

                candidate.Type = LobbyPlayerType.HOST;
                return true;
            }

            return false;
        }

        private void ProcessPlayerConnectionChanged(Player player)
        {
            NotifyPopulationChanged();
        }

        private void ProcessPlayerDisconnection(Player player)
        {
            if (player == null ||
                !_player_lobbies.TryGetValue(player.PlayerId, out int lobby_id) ||
                !_lobbies_data.TryGetValue(lobby_id, out LobbyData lobby))
            {
                NotifyPopulationChanged();
                return;
            }

            if (!lobby.IsConnectionBlocked)
            {
                _player_hubs.TryGetValue(player.PlayerId, out PlayerHubController player_hub);
                RemovePlayerFromWaitingLobby(player.PlayerId, player_hub, lobby);
                return;
            }

            NotifyPopulationChanged();
        }

        private void ProcessPlayerDeletion(Player player)
        {
            if (player != null)
                _player_hubs.Remove(player.PlayerId);

            NotifyPopulationChanged();
        }

        private MatchPreset GetNextRandomPreset()
        {
            if (_preset_rotation_index >= _preset_rotation.Count)
                RefillPresetRotation();

            if (_preset_rotation.Count == 0)
                return null;

            MatchPreset preset = _preset_rotation[_preset_rotation_index++];
            _last_preset_id = preset.Id;
            return preset;
        }

        private void RefillPresetRotation()
        {
            _preset_rotation.Clear();
            _preset_rotation_index = 0;

            MatchPresetRegistry registry = MatchPresetRegistry.LoadDefault();
            if (registry == null)
                return;

            for (int i = 0; i < registry.Presets.Count; i++)
            {
                MatchPreset preset = registry.Presets[i];
                if (preset != null)
                    _preset_rotation.Add(preset);
            }

            for (int i = _preset_rotation.Count - 1; i > 0; i--)
            {
                int swap_index = Random.Range(0, i + 1);
                (_preset_rotation[i], _preset_rotation[swap_index]) =
                    (_preset_rotation[swap_index], _preset_rotation[i]);
            }

            if (_preset_rotation.Count > 1 && _preset_rotation[0].Id == _last_preset_id)
                (_preset_rotation[0], _preset_rotation[1]) = (_preset_rotation[1], _preset_rotation[0]);
        }

        private string GenerateLobbyCode()
        {
            string code;
            do
            {
                char[] characters = new char[6];
                for (int i = 0; i < characters.Length; i++)
                    characters[i] = LobbyCodeCharacters[Random.Range(0, LobbyCodeCharacters.Length)];

                code = new string(characters);
            }
            while (_lobbies_codes.ContainsKey(code));

            return code;
        }

        private ServerPopulationSnapshot CreatePopulationSnapshot()
        {
            int public_lobbies = 0;
            int players_in_public_lobbies = 0;
            foreach (LobbyData lobby in _lobbies_data.Values)
            {
                if (lobby.AccessMode != LobbyAccessMode.Public || lobby.IsConnectionBlocked)
                    continue;

                public_lobbies++;
                players_in_public_lobbies += lobby.Players?.Count ?? 0;
            }

            MatchesContoller matches_controller = MatchesContoller.Instance;
            return new ServerPopulationSnapshot(
                PlayersController.Instance?.ConnectedPlayersCount ?? 0,
                matches_controller?.ConnectedPlayersCount ?? 0,
                matches_controller?.ActiveMatchesCount ?? 0,
                players_in_public_lobbies,
                public_lobbies);
        }

        private void DestroyLobby(int lobby_id)
        {
            if (!_lobbies_data.TryGetValue(lobby_id, out var lobby))
                return;

            if (_lobbies_players.TryGetValue(lobby_id, out List<PlayerHubController> players))
            {
                for (int i = 0; i < players.Count; i++)
                {
                    PlayerHubController player = players[i];
                    if (player?.Player == null)
                        continue;

                    _player_lobbies.Remove(player.Player.PlayerId);
                    player.UpdateLobbyData(null);
                }
            }

            _public_lobby_queue.Remove(lobby_id);
            _lobbies_players.Remove(lobby_id);
            _lobbies_data.Remove(lobby_id);
            _lobbies_codes.Remove(lobby.Code);
            NotifyPopulationChanged();
        }

        public void CloseFinishedLobby(int lobby_id)
        {
            if (!_lobbies_data.TryGetValue(lobby_id, out var lobby))
                return;

            if (_lobbies_players.TryGetValue(lobby_id, out List<PlayerHubController> players))
            {
                for (int i = 0; i < players.Count; i++)
                {
                    PlayerHubController player = players[i];
                    if (player?.Player == null)
                        continue;

                    _player_lobbies.Remove(player.Player.PlayerId);
                    player.UpdateLobbyData(null);
                }
            }

            _public_lobby_queue.Remove(lobby_id);
            _lobbies_players.Remove(lobby_id);
            _lobbies_data.Remove(lobby_id);
            _lobbies_codes.Remove(lobby.Code);
            NotifyPopulationChanged();
        }

        private void UpdateClientsData(int lobby_id)
        {
            if (!_lobbies_data.TryGetValue(lobby_id, out var lobby))
                return;

            if (!_lobbies_players.TryGetValue(lobby_id, out List<PlayerHubController> players))
                return;

            for (int i = 0; i < players.Count; i++)
                players[i]?.UpdateLobbyData(lobby);
        }

        private static void ApplyPreset(LobbyData lobby, MatchPreset preset)
        {
            if (preset == null)
                return;

            lobby.SelectedPresetId = preset.Id;
            lobby.SelectedSeed = preset.Seed;
            lobby.SelectedRoundsCount = Mathf.Max(1, preset.RoundsCount);
            lobby.SelectedRoundDurationSeconds = preset.RoundDurationSeconds > 0f
                ? preset.RoundDurationSeconds
                : 180f;
            lobby.SelectedMaxCaptureScore = preset.MaxCaptureScore > 0f
                ? preset.MaxCaptureScore
                : 100f;
            lobby.SelectedResultUrl = preset.ResultUrl;
        }

        private static TeamId GetDefaultTeam(LobbyData lobby)
        {
            int red = 0;
            int blue = 0;
            for (int i = 0; i < lobby.Players.Count; i++)
            {
                if (lobby.Players[i].Team == TeamId.Red)
                    red++;
                else if (lobby.Players[i].Team == TeamId.Blue)
                    blue++;
            }

            return red <= blue ? TeamId.Red : TeamId.Blue;
        }

        private static TeamId GetNextTeam(TeamId team)
        {
            return team switch
            {
                TeamId.Red => TeamId.Blue,
                TeamId.Blue => TeamId.Spectator,
                _ => TeamId.Red,
            };
        }
    }
}
