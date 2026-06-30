using Game.MatchConfig;
using Server.PlayerHub;
using Server.Match;
using UnityEngine;

namespace Core.ClientPresentation
{
    public class ClientLobbyActions : MonoBehaviour
    {
        private PlayerHubConnector _connector;
        private bool _is_leaving_match;

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

        public void FindOrCreatePublicLobby()
        {
            if (_connector == null)
                return;

            _connector.CommandFindOrCreatePublicLobby();
        }

        public void OpenLobby()
        {
            if (_connector == null)
                return;

            _connector.CommandOpenLobby();
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

        public void SwitchTeam()
        {
            if (_connector == null)
                return;

            _connector.CommandSwitchTeam();
        }

        public void SwitchPlayerTeam(int player_id)
        {
            if (_connector == null)
                return;

            _connector.CommandSwitchPlayerTeam(player_id);
        }

        public void SelectPreset(string preset_id)
        {
            if (_connector == null)
                return;

            _connector.CommandSelectPreset(preset_id);
        }

        public void SetTutorialMode(bool enabled)
        {
            if (_connector == null)
                return;

            _connector.CommandSetTutorialMode(enabled);
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
            if (_is_leaving_match)
                return;

            _is_leaving_match = true;
            StartCoroutine(LeaveMatchViewRoutine());
        }

        public void RequestLeaveMatch()
        {
            if (MatchPlayer.Local != null)
            {
                MatchPlayer.Local.RequestLeaveMatch();
                return;
            }

            if (_connector != null)
                _connector.CommandRequestLeaveMatch();

            LeaveMatchView();
        }

        private System.Collections.IEnumerator LeaveMatchViewRoutine()
        {
            ClientAppRoot app_root = ClientAppRoot.Instance;
            app_root.LobbyStore.Clear();
            yield return app_root.SceneFlow.ReturnToMenuFromMatch();
            ClientMatchPresetState.Clear();
            _is_leaving_match = false;
        }
    }
}
