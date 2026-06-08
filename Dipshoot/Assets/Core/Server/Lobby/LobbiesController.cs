using System.Collections.Generic;
using Game.MatchConfig;
using Game.MatchMode;
using Server.Match;
using Server.PlayerHub;
using Server.ServerSide;
using UnityEngine;

namespace Server.Lobby
{
    public class LobbiesController : Singleton<LobbiesController>
    {
        [SerializeField] private int _default_lobby_map = 0;
        [SerializeField] private int _default_lobby_capacity = 4;

        private int _id = 0;
        private Dictionary<string, int> _lobbies_codes = new();
        private Dictionary<int, LobbyData> _lobbies_data = new();
        private Dictionary<int, List<PlayerHubController>> _lobbies_players = new();

        public bool IsPlayerInLobby(PlayerHubController player, LobbyData lobby)
        {
            if (player == null || lobby == null || player.Player == null)
                return false;

            return _lobbies_data.TryGetValue(lobby.Id, out LobbyData active_lobby) &&
                active_lobby.GetPlayer(player.Player.PlayerId) != null &&
                _lobbies_players.TryGetValue(lobby.Id, out List<PlayerHubController> players) &&
                players.Contains(player);
        }

        public void CreateLobby(PlayerHubController player, string lobby_code)
        {
            if (_lobbies_codes.ContainsKey(lobby_code))
            {
                player.RegisterError("Lobby code alrady exists");
                return;
            }

            LobbyData lobby = new(_id++, lobby_code, _default_lobby_capacity, _default_lobby_map);
            ApplyDefaultPreset(lobby);
            lobby.AddPlayer(new(player.Player, LobbyPlayerType.HOST, TeamId.Red));

            _lobbies_codes.Add(lobby_code, lobby.Id);
            _lobbies_data.Add(lobby.Id, lobby);
            _lobbies_players.Add(lobby.Id, new() { player });
            
            UpdateClientsData(lobby.Id);
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

            lobby.AddPlayer(new(player.Player, LobbyPlayerType.CLIENT, GetDefaultTeam(lobby)));
            _lobbies_players[lobby.Id].Add(player);

            UpdateClientsData(lobby.Id);
        }

        public void LeaveFromLobby(PlayerHubController player, int lobby_id)
        {
            if (!_lobbies_data.TryGetValue(lobby_id, out var lobby))
            {
                player.RegisterError("Lobby doesn't exist");
                return;
            }

            LobbyPlayerData player_data = lobby.RemovePlayer(player.Player.PlayerId);
            _lobbies_players[lobby.Id].Remove(player);

            player.UpdateLobbyData(null);
            if (player_data != null && player_data.Type == LobbyPlayerType.HOST)
                DestroyLobby(lobby_id);
            else UpdateClientsData(lobby.Id);
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

            lobby.RemovePlayer(player.Player.PlayerId);
            if (_lobbies_players.TryGetValue(lobby.Id, out List<PlayerHubController> players))
                players.Remove(player);

            player.UpdateLobbyData(null);
        }

        public void StartGame(PlayerHubController player, int lobby_id)
        {
            if (!_lobbies_data.TryGetValue(lobby_id, out var lobby))
            {
                player.RegisterError("Lobby doesn't exist");
                return;
            }

            LobbyPlayerData player_data = lobby.GetPlayer(player.Player.PlayerId);
            if (player_data.Type != LobbyPlayerType.HOST)
            {
                player.RegisterError("Error 11: Attempt to start lobby without host role");
                return;
            }

            if (MatchPresetRegistry.GetPreset(lobby.SelectedPresetId) == null)
                ApplyDefaultPreset(lobby);

            lobby.IsConnectionBlocked = true;
            MatchesContoller.Instance.StartMatch(lobby);
            //Start game logic
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

            player_data.Team = player_data.Team switch
            {
                TeamId.Red => TeamId.Blue,
                TeamId.Blue => TeamId.Spectator,
                _ => TeamId.Red,
            };
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

        private void DestroyLobby(int lobby_id)
        {
            if (!_lobbies_data.TryGetValue(lobby_id, out var lobby))
                return;

            foreach (var player in _lobbies_players[lobby_id])
            {
                player.RegisterError($"Host destroy the lobby");
                player.UpdateLobbyData(null);
            }

            _lobbies_players.Remove(lobby_id);
            _lobbies_data.Remove(lobby_id);
            _lobbies_codes.Remove(lobby.Code);
        }

        public void CloseFinishedLobby(int lobby_id)
        {
            if (!_lobbies_data.TryGetValue(lobby_id, out var lobby))
                return;

            foreach (var player in _lobbies_players[lobby_id])
                player.UpdateLobbyData(null);

            _lobbies_players.Remove(lobby_id);
            _lobbies_data.Remove(lobby_id);
            _lobbies_codes.Remove(lobby.Code);
        }

        private void UpdateClientsData(int lobby_id)
        {
            if (!_lobbies_data.TryGetValue(lobby_id, out var lobby))
                return;

            foreach (var player in _lobbies_players[lobby_id])
                player.UpdateLobbyData(lobby);
        }

        private static void ApplyDefaultPreset(LobbyData lobby)
        {
            MatchPresetRegistry registry = MatchPresetRegistry.LoadDefault();
            ApplyPreset(lobby, registry == null ? null : registry.GetDefault());
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
    }
}
