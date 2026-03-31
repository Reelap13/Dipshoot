using System.Collections.Generic;
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

        public void CreateLobby(PlayerHubController player, string lobby_code)
        {
            if (_lobbies_codes.ContainsKey(lobby_code))
            {
                player.RegisterError("Lobby code alrady exists");
                return;
            }

            LobbyData lobby = new(_id++, lobby_code, _default_lobby_capacity, _default_lobby_map);
            lobby.AddPlayer(new(player.Player, LobbyPlayerType.HOST));

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

            lobby.AddPlayer(new(player.Player, LobbyPlayerType.CLIENT));
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
            if (player_data.Type == LobbyPlayerType.HOST)
                DestroyLobby(lobby_id);
            else UpdateClientsData(lobby.Id);
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

            lobby.IsConnectionBlocked = true;
            MatchesContoller.Instance.StartMatch(lobby);
            //Start game logic
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

        private void UpdateClientsData(int lobby_id)
        {
            if (!_lobbies_data.TryGetValue(lobby_id, out var lobby))
                return;

            foreach (var player in _lobbies_players[lobby_id])
                player.UpdateLobbyData(lobby);
        }
    }
}