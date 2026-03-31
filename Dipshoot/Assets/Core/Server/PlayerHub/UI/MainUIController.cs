using Server.Lobby;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Server.PlayerHub
{
    public class MainUIController : MonoBehaviour
    {
        [SerializeField] private PlayerHubUIController _controller;

        [SerializeField] private Button _create_lobby_button;
        [SerializeField] private Button _enter_to_lobby_button;
        [SerializeField] private Button _exit_button;

        [SerializeField] private TMP_InputField _lobby_code_field;

        private void Awake()
        {
            _controller.OnLobbyDataupdated.AddListener(UpdateUI);

            _create_lobby_button.onClick.AddListener(CreateLobby);
            _enter_to_lobby_button.onClick.AddListener(EnterToLobby);
            _exit_button.onClick.AddListener(Exit);
        }


        private void UpdateUI(LobbyData data)
        {
            if (data != null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
        }

        private void CreateLobby() => _controller.CreateLobby(_lobby_code_field.text);
        private void EnterToLobby() => _controller.EnterToLobby(_lobby_code_field.text);
        private void Exit() => _controller.ExitFromGame();
    }
}