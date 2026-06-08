using System.Collections.Generic;
using Mirror;
using Server.Data;
using Server.Lobby;
using Server.PlayerHub;
using Server.ServerSide;
using UnityEngine;

namespace Server.Match
{
    public class MatchPlayersController : MonoBehaviour
    {
        [field: SerializeField]
        public MatchController MatchController;
        [SerializeField] private MatchPlayer _match_player_prefab;

        private Dictionary<int, bool> _readiness;
        private Dictionary<int, MatchPlayer> _players;

        private void OnDestroy()
        {
            PlayersController players_controller = PlayersController.Instance;
            if (players_controller == null)
                return;

            players_controller.OnDisconnected.RemoveListener(ProcessPlayerDisconnection);
            players_controller.OnReconnected.RemoveListener(ProcessPlayerReconnection);
            players_controller.OnDeleted.RemoveListener(ProcessPlayerDeletion);
        }

        public void InitializePlayers()
        {
            _readiness = new();
            _players = new();
            GameObject match_players = new GameObject("MatchPlayers");
            MatchController.SceneManager.AddObjectToScene(match_players);
            foreach (var lobby_player in MatchController.MatchData.LobbyData.Players)
            {
                Player player = PlayersController.Instance.GetPlayer(lobby_player.PlayerId);
                MatchPlayer match_player = NetworkUtils.NetworkMatchInstantiate(
                    _match_player_prefab, MatchController.SceneManager.Scene, MatchController.MatchData.Guid, match_players.transform, match_players.transform);
                player.AddNetworkObject(match_player.netIdentity);
                match_player.Initialize(player, this);

                _readiness.Add(player.PlayerId, false);
                _players.Add(player.PlayerId, match_player);

                match_player.TargetLoadGameScene(
                    MatchController.GameSceneName,
                    MatchController.MatchData.LobbyData.SelectedPresetId,
                    MatchController.MatchData.LobbyData.SelectedSeed,
                    MatchController.MatchData.LobbyData.SelectedResultUrl);
            }

            PlayersController.Instance.OnDisconnected.AddListener(ProcessPlayerDisconnection);
            PlayersController.Instance.OnReconnected.AddListener(ProcessPlayerReconnection);
            PlayersController.Instance.OnDeleted.AddListener(ProcessPlayerDeletion);
        }

        public void MarkReadiness(Player player)
        {
            _readiness[player.PlayerId] = true;
            if (IsAllPlayersReady())
            {
                _readiness.Clear();
                MatchController.StartMatch();
            }
        }

        private bool IsAllPlayersReady()
        {
            foreach (var ready in _readiness.Values)
                if (!ready)
                    return false;
            return true;
        }

        public void ReturnPlayersToMenu()
        {
            if (_players == null)
                return;

            foreach (MatchPlayer player in _players.Values)
            {
                if (player != null)
                    player.TargetReturnToMenu();
            }
        }

        private void ProcessPlayerDisconnection(Player player)
        {
            RemovePlayer(player, false);
        }

        private void ProcessPlayerReconnection(Player player)
        {

        }

        private void ProcessPlayerDeletion(Player player)
        {
            RemovePlayer(player, false);
        }

        public bool ProcessPlayerLeave(Player player)
        {
            return RemovePlayer(player, true);
        }

        private bool RemovePlayer(Player player, bool return_to_menu)
        {
            if (player == null || _players == null)
                return false;

            if (!_players.TryGetValue(player.PlayerId, out MatchPlayer match_player))
                return false;

            _players.Remove(player.PlayerId);
            _readiness?.Remove(player.PlayerId);
            RemovePlayerFromLobby(player);

            if (return_to_menu && match_player != null)
                match_player.TargetReturnToMenu();

            player.RemoveNetworkObject(match_player.netIdentity);
            DestroyPlayerMatchObjects(player, match_player);

            if (match_player != null)
                NetworkServer.Destroy(match_player.gameObject);

            if (_players.Count == 0)
                MatchController.FinishMatch();

            return true;
        }

        private void DestroyPlayerMatchObjects(Player player, MatchPlayer match_player)
        {
            List<NetworkIdentity> objects = new(player.OwnObjects);
            for (int i = 0; i < objects.Count; i++)
            {
                NetworkIdentity identity = objects[i];
                if (identity == null || match_player != null && identity == match_player.netIdentity)
                    continue;

                NetworkMatch network_match = identity.GetComponent<NetworkMatch>();
                if (network_match == null || network_match.matchId != MatchController.MatchData.Guid)
                    continue;

                player.RemoveNetworkObject(identity);
                NetworkServer.Destroy(identity.gameObject);
            }
        }

        private void RemovePlayerFromLobby(Player player)
        {
            MatchController.MatchData.LobbyData.RemovePlayer(player.PlayerId);

            PlayerHubController player_hub = FindPlayerHub(player);
            if (player_hub == null)
                return;

            LobbiesController.Instance.LeaveStartedLobby(player_hub, MatchController.MatchData.LobbyData.Id);
        }

        private static PlayerHubController FindPlayerHub(Player player)
        {
            if (player == null || player.OwnObjects == null)
                return null;

            for (int i = 0; i < player.OwnObjects.Count; i++)
            {
                NetworkIdentity identity = player.OwnObjects[i];
                if (identity == null)
                    continue;

                PlayerHubController player_hub = identity.GetComponent<PlayerHubController>();
                if (player_hub != null)
                    return player_hub;
            }

            return null;
        }
    }
}
