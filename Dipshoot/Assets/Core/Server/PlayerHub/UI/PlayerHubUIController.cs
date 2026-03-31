using System;
using Server.Lobby;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Server.PlayerHub
{
    public class PlayerHubUIController : MonoBehaviour
    {
        [NonSerialized] public UnityEvent<LobbyData> OnLobbyDataupdated = new();
        [NonSerialized] public UnityEvent<string> OnErrorRegistered = new();

        [SerializeField] private PlayerHubConnector _connector;
        [SerializeField] private GameObject _ui;
        [SerializeField] private TextMeshProUGUI _nickname_field;
        [SerializeField] private Transform _camera_point;

        private void Awake()
        {
            _ui.SetActive(false);
        }

        public void Initialize()
        {
            _ui.SetActive(true);
            _nickname_field.text = $"Nickname: {_connector.PlayerNickname}";
            Camera.main.transform.position = _camera_point.position;
            Camera.main.transform.rotation = _camera_point.rotation;

            _connector.OnLobbyDataUpdated.AddListener(UpdateUI);
            _connector.OnErrorRegistered.AddListener(RegisterError);
            
            UpdateUI(null);
        }

        private void UpdateUI(LobbyData data) => OnLobbyDataupdated.Invoke(data);
        private void RegisterError(string error) => OnErrorRegistered.Invoke(error);

        public void CreateLobby(string lobby_code) => _connector.CommandCreateLobby(lobby_code);
        public void EnterToLobby(string lobby_code) => _connector.CommandEnterToLobby(lobby_code);
        public void LeaveFromLobby() => _connector.CommandLeaveFromLobby();
        public void StartGame() => _connector.CommandStartGame();
        public void ExitFromGame()
        {

        }
    }
}