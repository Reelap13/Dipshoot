using System.Collections.Generic;
using Server.Lobby;

namespace Game.MatchMode
{
    public class TeamRoster
    {
        private readonly Dictionary<int, TeamId> _player_teams = new();

        public void AssignLobbyTeams(IReadOnlyList<LobbyPlayerData> players)
        {
            _player_teams.Clear();

            for (int i = 0; i < players.Count; i++)
                _player_teams[players[i].PlayerId] = players[i].Team == TeamId.None ? TeamId.Blue : players[i].Team;
        }

        public TeamId GetTeam(int player_id)
        {
            return _player_teams.TryGetValue(player_id, out TeamId team_id)
                ? team_id
                : TeamId.None;
        }
    }
}
