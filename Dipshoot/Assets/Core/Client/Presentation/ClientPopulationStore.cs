using System;
using Server.ServerSide;
using UnityEngine;

namespace Core.ClientPresentation
{
    public class ClientPopulationStore : MonoBehaviour
    {
        public event Action OnUpdated;

        public ServerPopulationSnapshot Snapshot { get; private set; }

        public void SetSnapshot(ServerPopulationSnapshot snapshot)
        {
            Snapshot = snapshot;
            OnUpdated?.Invoke();
        }
    }
}
