using System.Collections.Generic;
using Server.Lobby;
using UnityEngine;
using UnityEngine.UI;

namespace Core.ClientPresentation
{
    public class ClientLobbyLayer : MonoBehaviour
    {
        [SerializeField] private Text _lobby_code_text;
        [SerializeField] private Text _capacity_text;
        [SerializeField] private List<Text> _player_rows;
        [SerializeField] private Button _leave_button;
        [SerializeField] private Button _start_game_button;

        private ClientUiLayer _layer;

        private void Awake()
        {
            _layer = GetOrAddLayer();
            _layer.Initialize(ClientUiLayerKind.Lobby);

            _leave_button.onClick.AddListener(Leave);
            _start_game_button.onClick.AddListener(StartGame);

            ClientAppRoot app_root = ClientAppRoot.Instance;
            app_root.LobbyStore.OnLobbyUpdated += UpdateView;
            app_root.SessionStore.OnUpdated += UpdateView;
            UpdateView(app_root.LobbyStore.CurrentLobby);
        }

        private void OnDestroy()
        {
            _leave_button.onClick.RemoveListener(Leave);
            _start_game_button.onClick.RemoveListener(StartGame);

            if (!ClientAppRoot.HasInstance)
                return;

            ClientAppRoot app_root = ClientAppRoot.Instance;
            app_root.LobbyStore.OnLobbyUpdated -= UpdateView;
            app_root.SessionStore.OnUpdated -= UpdateView;
        }

        private void UpdateView() => UpdateView(ClientAppRoot.Instance.LobbyStore.CurrentLobby);
        private void UpdateView(LobbyData lobby)
        {
            if (lobby == null)
            {
                ClearView();
                return;
            }

            _lobby_code_text.text = $"Code: {lobby.Code}";
            int players_count = lobby.Players != null ? lobby.Players.Count : 0;
            _capacity_text.text = $"Players: {players_count}/{lobby.PlayersCapacity}";

            for (int i = 0; i < _player_rows.Count; i++)
            {
                if (lobby.Players == null || lobby.Players.Count <= i)
                {
                    _player_rows[i].text = string.Empty;
                    continue;
                }

                LobbyPlayerData player = lobby.Players[i];
                _player_rows[i].text = $"{i + 1}. {player.Nickname} ({FormatPlayerType(player.Type)})";
            }

            int player_id = ClientAppRoot.Instance.SessionStore.PlayerId;
            LobbyPlayerData local_player = lobby.GetPlayer(player_id);
            bool is_host = local_player != null && local_player.Type == LobbyPlayerType.HOST;
            _start_game_button.gameObject.SetActive(is_host);
        }

        private void ClearView()
        {
            _lobby_code_text.text = "Code: -";
            _capacity_text.text = "Players: 0/0";
            foreach (Text row in _player_rows)
                row.text = string.Empty;

            _start_game_button.gameObject.SetActive(false);
        }

        private string FormatPlayerType(LobbyPlayerType type)
        {
            return type == LobbyPlayerType.HOST ? "Host" : "Client";
        }

        private void Leave()
        {
            ClientAppRoot.Instance.LobbyActions.LeaveFromLobby();
        }

        private void StartGame()
        {
            ClientAppRoot.Instance.LobbyActions.StartGame();
        }

        private ClientUiLayer GetOrAddLayer()
        {
            ClientUiLayer layer = gameObject.GetComponent<ClientUiLayer>();
            return layer != null ? layer : gameObject.AddComponent<ClientUiLayer>();
        }
    }
}
