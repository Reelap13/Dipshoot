using System;
using Game.MatchMode;
using UnityEngine;

namespace Core.ClientPresentation
{
    public class ClientMatchStore : MonoBehaviour
    {
        public event Action OnUpdated;

        public bool HasActiveMatch { get; private set; }
        public bool IsMatchSceneLoaded { get; private set; }
        public string MatchSceneName { get; private set; }
        public TeamControlModeController ModeController { get; private set; }

        public void BeginLoading(string scene_name)
        {
            HasActiveMatch = true;
            IsMatchSceneLoaded = false;
            MatchSceneName = scene_name;
            OnUpdated?.Invoke();
        }

        public void MarkSceneLoaded()
        {
            HasActiveMatch = true;
            IsMatchSceneLoaded = true;
            OnUpdated?.Invoke();
        }

        public void SetModeController(TeamControlModeController mode_controller)
        {
            ModeController = mode_controller;
            HasActiveMatch = mode_controller != null || HasActiveMatch;
            OnUpdated?.Invoke();
        }

        public void ClearModeController(TeamControlModeController mode_controller)
        {
            if (ModeController != mode_controller)
                return;

            ModeController = null;
            OnUpdated?.Invoke();
        }

        public void FinishMatch()
        {
            HasActiveMatch = false;
            IsMatchSceneLoaded = false;
            MatchSceneName = null;
            ModeController = null;
            OnUpdated?.Invoke();
        }
    }
}
