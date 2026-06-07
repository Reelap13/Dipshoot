using System;
using System.Collections;
using Server.Lobby;
using UnityEngine;
using UnityEngine.Events;

namespace Server.Match
{
    public class MatchController : MonoBehaviour
    {
        [NonSerialized] public UnityEvent OnLoadingMatch = new UnityEvent();
        [NonSerialized] public UnityEvent OnStartingMatch = new UnityEvent();
        [NonSerialized] public UnityEvent OnDestroingMatch = new UnityEvent();
        [NonSerialized] public UnityEvent OnDisconnectedPlayer = new UnityEvent();

        [field: SerializeField]
        public MatchSceneManager SceneManager { get; private set; }
        [field: SerializeField]
        public MatchPlayersController PlayersController { get; private set; }
        [field: SerializeField]
        public string GameSceneName { get; private set; }

        public MatchData MatchData { get; private set; }

        private bool _is_finishing;
        private bool _is_destroyed;
        private bool _is_stats_logged;

        public void LoadMatch(MatchData match_data)
        {
            MatchData = match_data;
            SceneManager.CreateMatchScene();
            //StartMatch();
            //OnLoadingMatch.Invoke();
        }

        public void ConnectPlayers()
        {
            PlayersController.InitializePlayers();
        }

        public void StartMatch()
        {
            OnStartingMatch.Invoke();
        }

        public void FinishMatch()
        {
            if (_is_finishing)
                return;

            _is_finishing = true;
            LogMatchStatsOnce();
            StartCoroutine(FinishMatchRoutine());
        }

        public void LogMatchStatsOnce()
        {
            if (_is_stats_logged)
                return;

            _is_stats_logged = true;
            MatchStatsFileLogger.Write(this);
        }

        private IEnumerator FinishMatchRoutine()
        {
            PlayersController.ReturnPlayersToMenu();
            LobbiesController.Instance.CloseFinishedLobby(MatchData.LobbyData.Id);
            yield return new WaitForSeconds(0.25f);
            DestroyMatch();
        }

        /*public void DisconnectPlayer(Client player)
        {
            PlayerList.DisconnectPLayer(player);
            OnDisconnectedPlayer.Invoke(player);
        }*/

        public void DestroyMatch()
        {
            if (_is_destroyed)
                return;

            _is_destroyed = true;
            OnDestroingMatch.Invoke();
            MatchesContoller.Instance.RemoveMatch(MatchData.MatchId);
            Destroy(gameObject);
        }
    }
}
