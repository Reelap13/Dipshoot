using System;

namespace Server.ServerSide
{
    [Serializable]
    public struct ServerPopulationSnapshot
    {
        public int TotalOnlinePlayers;
        public int PlayersInMatches;
        public int ActiveMatches;
        public int PlayersInPublicLobbies;
        public int PublicLobbies;

        public ServerPopulationSnapshot(
            int total_online_players,
            int players_in_matches,
            int active_matches,
            int players_in_public_lobbies,
            int public_lobbies)
        {
            TotalOnlinePlayers = total_online_players;
            PlayersInMatches = players_in_matches;
            ActiveMatches = active_matches;
            PlayersInPublicLobbies = players_in_public_lobbies;
            PublicLobbies = public_lobbies;
        }
    }
}
