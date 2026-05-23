using System.Collections.Generic;
using Server.Lobby;

namespace Game.MatchMode
{
    public class TeamRoster
    {
        private readonly Dictionary<int, TeamId> _player_teams = new();

        public void AssignBalancedTeams(IReadOnlyList<LobbyPlayerData> players)
        {
            _player_teams.Clear();

            for (int i = 0; i < players.Count; i++)
                _player_teams[players[i].PlayerId] = i % 2 == 0 ? TeamId.Red : TeamId.Blue;
        }

        public TeamId GetTeam(int player_id)
        {
            return _player_teams.TryGetValue(player_id, out TeamId team_id)
                ? team_id
                : TeamId.None;
        }
    }
}
