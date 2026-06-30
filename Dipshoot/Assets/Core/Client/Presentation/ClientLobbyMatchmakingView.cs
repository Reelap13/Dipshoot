using Server.Lobby;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Core.ClientPresentation
{
    public class ClientLobbyMatchmakingView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _status_text;
        [SerializeField] private Button _open_lobby_button;

        public Button OpenLobbyButton => _open_lobby_button;

        private void Awake()
        {
            _status_text ??= transform.Find("LobbyStatusText")?.GetComponent<TextMeshProUGUI>();
            _open_lobby_button ??= transform.Find("OpenLobbyButton")?.GetComponent<Button>();
        }

        public void SetState(LobbyData lobby, bool is_host)
        {
            bool is_public = lobby != null && lobby.AccessMode == LobbyAccessMode.Public;
            _status_text.text = lobby == null
                ? "Статус: -"
                : is_public
                    ? "Статус: поиск игроков"
                    : "Статус: закрытое лобби";
            _open_lobby_button.gameObject.SetActive(
                lobby != null &&
                is_host &&
                !is_public &&
                !lobby.IsConnectionBlocked);
        }
    }
}
