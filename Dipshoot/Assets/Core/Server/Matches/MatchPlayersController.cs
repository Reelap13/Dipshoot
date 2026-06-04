using System.Collections.Generic;
using Mirror;
using Server.Data;
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

        }

        private void ProcessPlayerReconnection(Player player)
        {

        }

        private void ProcessPlayerDeletion(Player player)
        {
            _players.Remove(player.PlayerId);
            if (_players.Count == 0)
                MatchController.DestroyMatch();
        }
    }
}
