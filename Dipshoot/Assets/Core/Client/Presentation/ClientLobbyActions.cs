using Server.PlayerHub;
using UnityEngine;

namespace Core.ClientPresentation
{
    public class ClientLobbyActions : MonoBehaviour
    {
        private PlayerHubConnector _connector;

        public bool IsBound => _connector != null;

        public void Bind(PlayerHubConnector connector)
        {
            _connector = connector;
        }

        public void CreateLobby(string lobby_code)
        {
            if (_connector == null)
                return;

            _connector.CommandCreateLobby(lobby_code);
        }

        public void EnterToLobby(string lobby_code)
        {
            if (_connector == null)
                return;

            _connector.CommandEnterToLobby(lobby_code);
        }

        public void LeaveFromLobby()
        {
            if (_connector == null)
                return;

            _connector.CommandLeaveFromLobby();
        }

        public void StartGame()
        {
            if (_connector == null)
                return;

            _connector.CommandStartGame();
        }

        public void OpenMatchMenu()
        {
            ClientAppRoot app_root = ClientAppRoot.Instance;
            if (!app_root.MatchStore.HasActiveMatch)
                return;

            app_root.PresentationRoot.SetState(ClientPresentationState.MatchMenu);
        }

        public void ReturnToMatch()
        {
            ClientAppRoot app_root = ClientAppRoot.Instance;
            if (!app_root.MatchStore.HasActiveMatch)
                return;

            app_root.PresentationRoot.SetState(ClientPresentationState.Match);
        }

        public void LeaveMatchView()
        {
            StartCoroutine(ClientAppRoot.Instance.SceneFlow.ReturnToMenuFromMatch());
        }
    }
}
