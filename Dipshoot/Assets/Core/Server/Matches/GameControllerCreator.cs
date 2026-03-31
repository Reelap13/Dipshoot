using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Server.Match
{
    public class GameControllerCreator : MonoBehaviour
    {
        [field: SerializeField]
        public MatchController MatchController;

        [SerializeField] private GameController _game_controller_pref;

        private void Awake()
        {
            MatchController.OnStartingMatch.AddListener(StartGame);
        }

        private void StartGame()
        {
            GameController game_controller = NetworkUtils.NetworkMatchInstantiate(
                _game_controller_pref, 
                MatchController.SceneManager.Scene, 
                MatchController.MatchData.Guid);
            game_controller.LoadGame(MatchController);
        }
    }
}