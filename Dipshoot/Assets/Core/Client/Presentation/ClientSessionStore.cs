using System;
using UnityEngine;

namespace Core.ClientPresentation
{
    public class ClientSessionStore : MonoBehaviour
    {
        public event Action OnUpdated;

        public int PlayerId { get; private set; } = -1;
        public string PlayerNickname { get; private set; }
        public bool IsInitialized => PlayerId >= 0;

        public void SetPlayer(int player_id, string player_nickname)
        {
            PlayerId = player_id;
            PlayerNickname = player_nickname;
            OnUpdated?.Invoke();
        }

        public void Clear()
        {
            PlayerId = -1;
            PlayerNickname = null;
            OnUpdated?.Invoke();
        }
    }
}
