using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Server.Match
{
    public abstract class GameController : NetworkBehaviour
    {
        [NonSerialized] public UnityEvent OnStartingGame = new UnityEvent();
        [NonSerialized] public UnityEvent OnEndingGame = new UnityEvent();
        [NonSerialized] public UnityEvent OnDestroingGame = new UnityEvent();
        
        public MatchController MatchController { get; private set; }

        public Scene Scene => MatchController.SceneManager.Scene;
        public Guid MatchId => MatchController.MatchData.Guid;

        public virtual void LoadGame(MatchController match_controller)
        {
            MatchController = match_controller;
            MatchLogContext log_context = gameObject.AddComponent<MatchLogContext>();
            log_context.Initialize(match_controller);
            match_controller.OnDestroingMatch.AddListener(DestoryGame);
        }

        public virtual void StartGame()
        {
            OnStartingGame.Invoke();
        }

        protected virtual void EndGame()
        {
            OnEndingGame.Invoke();
        }

        public void FinishGame()
        {
            EndGame();
            MatchController.FinishMatch();
        }

        public void EndGameplay()
        {
            EndGame();
        }

        protected void DestoryGame()
        {
            OnDestroingGame.Invoke();
            NetworkServer.Destroy(gameObject);
        }
    }
}
