using System;
using Mirror;
using Server.Lobby;
using UnityEngine;
using UnityEngine.Events;

namespace Server.PlayerHub
{
    public class PlayerHubConnector : NetworkBehaviour
    {
        // Server
        private PlayerHubController _controller;

        // Client
        [NonSerialized] public UnityEvent<LobbyData> OnLobbyDataUpdated = new();
        [NonSerialized] public UnityEvent<string> OnErrorRegistered = new();

        [SerializeField] private PlayerHubUIController _ui;
        
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
            _ui.Initialize();
        }

        [TargetRpc]
        public void TargetUpdateLobbyData(LobbyData data)
        {
            Lobby = data;
            OnLobbyDataUpdated.Invoke(Lobby);
        }

        [TargetRpc]
        public void TargetRegisterError(string error) => OnErrorRegistered.Invoke(error);

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