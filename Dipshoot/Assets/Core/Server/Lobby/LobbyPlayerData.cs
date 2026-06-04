using Server.Data;
using Game.MatchMode;

namespace Server.Lobby
{
    [System.Serializable]
    public class LobbyPlayerData
    {
        public int PlayerId;
        public string Nickname;
        public LobbyPlayerType Type;
        public TeamId Team;

        public LobbyPlayerData() { }
        public LobbyPlayerData(Player player, LobbyPlayerType type, TeamId team)
        {
            PlayerId = player.PlayerId;
            Nickname = player.Nickname;
            Type = type;
            Team = team;
        }
    }
}
