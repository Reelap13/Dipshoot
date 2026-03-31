using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Server.Match
{
    public abstract class GameController : NetworkBehaviour
    {
        [NonSerialized] public UnityEvent OnStartingGame = new UnityEvent();
        [NonSerialized] public UnityEvent OnEndingGame = new UnityEvent();
        [NonSerialized] public UnityEvent OnDestroingGame = new UnityEvent();
        
        protected MatchController MatchController;

        public virtual void LoadGame(MatchController match_controller)
        {
            MatchController = match_controller;
            match_controller.OnDestroingMatch.AddListener(DestoryGame);
        }

        protected virtual void StartGame()
        {
            OnStartingGame.Invoke();
        }

        protected virtual void EndGame()
        {
            OnEndingGame.Invoke();
        }

        protected void DestoryGame()
        {
            OnDestroingGame.Invoke();
            NetworkServer.Destroy(gameObject);
        }
    }
}