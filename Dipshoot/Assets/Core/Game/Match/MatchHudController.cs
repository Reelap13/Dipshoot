using Core.ClientPresentation;
using Mirror;
using UnityEngine;

namespace Game.MatchMode
{
    public class MatchHudController : NetworkBehaviour
    {
        [SerializeField] private TeamControlModeController _mode_controller;

        public override void OnStartClient()
        {
            base.OnStartClient();
            CacheReferences();
            ClientAppRoot.Instance.MatchStore.SetModeController(_mode_controller);
        }

        private void OnDestroy()
        {
            if (!ClientAppRoot.HasInstance)
                return;

            ClientAppRoot.Instance.MatchStore.ClearModeController(_mode_controller);
        }

        private void CacheReferences()
        {
            if (_mode_controller == null)
                _mode_controller = GetComponent<TeamControlModeController>();
        }
    }
}
