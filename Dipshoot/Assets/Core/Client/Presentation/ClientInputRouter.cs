using System;
using UnityEngine;

namespace Core.ClientPresentation
{
    public class ClientInputRouter : MonoBehaviour
    {
        public event Action<ClientInputMode> OnModeUpdated;

        public ClientInputMode Mode { get; private set; } = ClientInputMode.Menu;
        public bool IsGameplayInputAllowed => Mode == ClientInputMode.Gameplay;

        public void SetMode(ClientInputMode mode)
        {
            if (Mode == mode)
                return;

            Mode = mode;
            OnModeUpdated?.Invoke(Mode);
        }
    }
}
