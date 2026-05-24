using System;
using Server.Lobby;
using UnityEngine;

namespace Core.ClientPresentation
{
    public class ClientLobbyStore : MonoBehaviour
    {
        public event Action<LobbyData> OnLobbyUpdated;
        public event Action<string> OnErrorRegistered;

        public LobbyData CurrentLobby { get; private set; }
        public string LastError { get; private set; }
        public bool HasLobby => CurrentLobby != null;

        public void SetLobby(LobbyData lobby)
        {
            CurrentLobby = lobby;
            OnLobbyUpdated?.Invoke(CurrentLobby);
        }

        public void RegisterError(string error)
        {
            LastError = error;
            OnErrorRegistered?.Invoke(error);
        }

        public void Clear()
        {
            CurrentLobby = null;
            LastError = null;
            OnLobbyUpdated?.Invoke(CurrentLobby);
        }
    }
}
