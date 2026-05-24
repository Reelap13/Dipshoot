using System;
using UnityEngine;

namespace Core.ClientPresentation
{
    public class ClientCameraRouter : MonoBehaviour
    {
        public event Action<ClientCameraContext> OnContextUpdated;

        public ClientCameraContext Context { get; private set; } = ClientCameraContext.Menu;

        public void SetContext(ClientCameraContext context)
        {
            if (Context == context)
                return;

            Context = context;
            OnContextUpdated?.Invoke(Context);
        }
    }
}
