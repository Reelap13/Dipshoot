using Server.Lobby;
using UnityEngine;
using UnityEngine.UI;

namespace Core.ClientPresentation
{
    public class ClientMainMenuLayer : MonoBehaviour
    {
        [SerializeField] private Text _nickname_text;
        [SerializeField] private InputField _lobby_code_field;
        [SerializeField] private Button _create_lobby_button;
        [SerializeField] private Button _enter_lobby_button;
        [SerializeField] private Button _return_to_match_button;
        [SerializeField] private Button _leave_match_button;
        [SerializeField] private Button _exit_button;

        private ClientUiLayer _layer;

        private void Awake()
        {
            _layer = GetOrAddLayer();
            _layer.Initialize(ClientUiLayerKind.MainMenu);

            _create_lobby_button.onClick.AddListener(CreateLobby);
            _enter_lobby_button.onClick.AddListener(EnterLobby);
            _return_to_match_button.onClick.AddListener(ReturnToMatch);
            _leave_match_button.onClick.AddListener(LeaveMatch);
            _exit_button.onClick.AddListener(Exit);

            ClientAppRoot app_root = ClientAppRoot.Instance;
            app_root.SessionStore.OnUpdated += UpdateView;
            app_root.LobbyStore.OnLobbyUpdated += UpdateView;
            app_root.MatchStore.OnUpdated += UpdateView;
            app_root.PresentationRoot.OnStateUpdated += UpdateView;
            UpdateView();
        }

        private void OnDestroy()
        {
            _create_lobby_button.onClick.RemoveListener(CreateLobby);
            _enter_lobby_button.onClick.RemoveListener(EnterLobby);
            _return_to_match_button.onClick.RemoveListener(ReturnToMatch);
            _leave_match_button.onClick.RemoveListener(LeaveMatch);
            _exit_button.onClick.RemoveListener(Exit);

            if (!ClientAppRoot.HasInstance)
                return;

            ClientAppRoot app_root = ClientAppRoot.Instance;
            app_root.SessionStore.OnUpdated -= UpdateView;
            app_root.LobbyStore.OnLobbyUpdated -= UpdateView;
            app_root.MatchStore.OnUpdated -= UpdateView;
            app_root.PresentationRoot.OnStateUpdated -= UpdateView;
        }

        private void UpdateView() => UpdateView(null);
        private void UpdateView(LobbyData lobby)
        {
            ClientAppRoot app_root = ClientAppRoot.Instance;
            _nickname_text.text = app_root.SessionStore.IsInitialized
                ? $"Nickname: {app_root.SessionStore.PlayerNickname}"
                : "Nickname: unknown";

            bool has_active_match = app_root.MatchStore.HasActiveMatch;
            _lobby_code_field.gameObject.SetActive(!has_active_match);
            _create_lobby_button.gameObject.SetActive(!has_active_match);
            _enter_lobby_button.gameObject.SetActive(!has_active_match);
            _return_to_match_button.gameObject.SetActive(has_active_match);
            _leave_match_button.gameObject.SetActive(has_active_match);
        }

        private void UpdateView(ClientPresentationState state) => UpdateView();

        private void CreateLobby()
        {
            string lobby_code = _lobby_code_field.text.Trim();
            if (string.IsNullOrWhiteSpace(lobby_code))
            {
                ClientAppRoot.Instance.LobbyStore.RegisterError("Lobby code is empty");
                return;
            }

            ClientAppRoot.Instance.LobbyActions.CreateLobby(lobby_code);
        }

        private void EnterLobby()
        {
            string lobby_code = _lobby_code_field.text.Trim();
            if (string.IsNullOrWhiteSpace(lobby_code))
            {
                ClientAppRoot.Instance.LobbyStore.RegisterError("Lobby code is empty");
                return;
            }

            ClientAppRoot.Instance.LobbyActions.EnterToLobby(lobby_code);
        }

        private void ReturnToMatch()
        {
            ClientAppRoot.Instance.LobbyActions.ReturnToMatch();
        }

        private void LeaveMatch()
        {
            ClientAppRoot.Instance.LobbyActions.LeaveMatchView();
        }

        private void Exit()
        {
            Application.Quit();
        }

        private ClientUiLayer GetOrAddLayer()
        {
            ClientUiLayer layer = gameObject.GetComponent<ClientUiLayer>();
            return layer != null ? layer : gameObject.AddComponent<ClientUiLayer>();
        }
    }
}
