using System.Collections.Generic;
using Server.Lobby;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Server.PlayerHub
{
    public class LobbyUIController : MonoBehaviour
    {
        [SerializeField] private PlayerHubUIController _controller;

        [SerializeField] private Button _leave_button;
        [SerializeField] private TextMeshProUGUI _lobby_code;
        [SerializeField] private Button _start_game_button;
        [SerializeField] private List<CharacterModel> _characters;

        private void Awake()
        {
            _controller.OnLobbyDataupdated.AddListener(UpdateUI);

            _leave_button.onClick.AddListener(Leave);
            _start_game_button.onClick.AddListener(StartGame);
        }

        private void UpdateUI(LobbyData data)
        {
            if (data == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            _lobby_code.text = data.Code;

            for (int i = 0; i < _characters.Count; ++i)
            {
                if (data.Players.Count <= i)
                {
                    _characters[i].gameObject.SetActive(false);
                    continue;
                }

                _characters[i].gameObject.SetActive(true);
                _characters[i].NicknameText.text = data.Players[i].Nickname;
            }

            LobbyPlayerData player = data.GetPlayer(PlayerHubConnector.Local.PlayerId);
            if (player == null || player.Type != LobbyPlayerType.HOST)
            {
                _start_game_button.gameObject.SetActive(false);
                return;
            }
            _start_game_button.gameObject.SetActive(true);
        }

        private void Leave() => _controller.LeaveFromLobby();
        private void StartGame() => _controller.StartGame();
    }
}