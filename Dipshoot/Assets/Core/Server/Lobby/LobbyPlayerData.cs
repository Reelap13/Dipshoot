using Server.Data;
using UnityEngine;

namespace Server.Lobby
{
    [System.Serializable]
    public class LobbyPlayerData
    {
        public int PlayerId;
        public string Nickname;
        public LobbyPlayerType Type;

        public LobbyPlayerData() { }
        public LobbyPlayerData(Player player, LobbyPlayerType type)
        {
            PlayerId = player.PlayerId;
            Nickname = player.Nickname;
            Type = type;
        }
    }
}