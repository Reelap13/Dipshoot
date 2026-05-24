using Core.ClientPresentation;
using Mirror;
using Server.Lobby;

namespace Server.PlayerHub
{
    public class PlayerHubConnector : NetworkBehaviour
    {
        // Server
        private PlayerHubController _controller;

        // Client
        public LobbyData Lobby { get; private set; }
        public int PlayerId { get; private set; }
        public string PlayerNickname { get; private set; }

        public static PlayerHubConnector Local { get; private set; }

        public void Initialize(PlayerHubController controller)
        {
            _controller = controller;
            TargetInitialize(controller.Player.PlayerId, controller.Player.Nickname);
        }

        [TargetRpc]
        private void TargetInitialize(int player_id, string player_nickname)
        {
            PlayerId = player_id;
            PlayerNickname = player_nickname;
            Local = this;

            ClientAppRoot app_root = ClientAppRoot.Instance;
            app_root.SessionStore.SetPlayer(player_id, player_nickname);
            app_root.LobbyActions.Bind(this);
            app_root.PresentationRoot.SetState(GetPresentationState(app_root, app_root.LobbyStore.CurrentLobby));
        }

        [TargetRpc]
        public void TargetUpdateLobbyData(LobbyData data)
        {
            Lobby = data;
            ClientAppRoot app_root = ClientAppRoot.Instance;
            app_root.LobbyStore.SetLobby(Lobby);
            app_root.PresentationRoot.SetState(GetPresentationState(app_root, Lobby));
        }

        [TargetRpc]
        public void TargetRegisterError(string error)
        {
            ClientAppRoot.Instance.LobbyStore.RegisterError(error);
        }

        private ClientPresentationState GetPresentationState(ClientAppRoot app_root, LobbyData data)
        {
            if (data != null)
                return ClientPresentationState.Lobby;

            return app_root.MatchStore.HasActiveMatch
                ? ClientPresentationState.MatchMenu
                : ClientPresentationState.MainMenu;
        }

        [Command]
        public void CommandCreateLobby(string lobby_code) => _controller.CreateLobby(lobby_code);
        [Command]
        public void CommandEnterToLobby(string lobby_code) => _controller.EnterToLobby(lobby_code);
        [Command]
        public void CommandLeaveFromLobby() => _controller.LeaveFromLobby();
        [Command]
        public void CommandStartGame() => _controller.StartGame();
    }
}
