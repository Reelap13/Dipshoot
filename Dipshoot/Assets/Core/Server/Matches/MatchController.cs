using System;
using Server.Match;
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

        /*public void DisconnectPlayer(Client player)
        {                                                
            PlayerList.DisconnectPLayer(player);
            OnDisconnectedPlayer.Invoke(player);
        }*/

        public void DestroyMatch()
        {
            OnDestroingMatch.Invoke();
            Destroy(gameObject);
        }
    }
}