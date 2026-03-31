using Mirror;
using Server.Data;
using Server.Lobby;
using UnityEngine;

namespace Server.PlayerHub
{
    public class PlayerHubController : NetworkBehaviour
    {
        [SerializeField] private PlayerHubConnector _connector;

        public Player Player { get; private set; }
        public LobbyData Lobby { get; private set; }

        public void Initialize(Player player)
        {
            Player = player;
            _connector.Initialize(this);
        }

        public void UpdateLobbyData(LobbyData lobby)
        {
            Lobby = lobby;
            _connector.TargetUpdateLobbyData(lobby);
        }

        public void RegisterError(string error) => _connector.TargetRegisterError(error);

        public void CreateLobby(string lobby_code)
        {
            if (Lobby != null)
            {
                RegisterError("Error 01: Attempt to create a lobby from another lobby");
                return;
            }
            LobbiesController.Instance.CreateLobby(this, ParceLobbyCode(lobby_code));
        }

        public void EnterToLobby(string lobby_code)
        {
            if (Lobby != null)
            {
                RegisterError("Error 02: Attempt to enter the lobby from another lobby");
                return;
            }
            LobbiesController.Instance.EnterToLobby(this, lobby_code);
        }

        public void LeaveFromLobby()
        {
            if (Lobby == null)
            {
                RegisterError("Error 03: Attempt to leave the lobby without being a member of the lobby");
                return;
            }
            LobbiesController.Instance.LeaveFromLobby(this, Lobby.Id);
        }

        public void StartGame()
        {
            if (Lobby == null)
            {
                RegisterError("Error 04: Attempt to start the lobby without being a member of the lobby");
                return;
            }

            LobbiesController.Instance.StartGame(this, Lobby.Id);
        }

        private const string _chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        private string ParceLobbyCode(string lobby_code)
        {
            lobby_code ??= "";

            if (lobby_code.Length > 6)
                return lobby_code[..6];
            while (lobby_code.Length < 3)
                lobby_code += _chars[Random.Range(0, _chars.Length)];

            return lobby_code;
        }
    }
}
