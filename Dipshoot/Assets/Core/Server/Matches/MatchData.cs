using System;
using Server.Lobby;
using UnityEngine;

namespace Server.Match
{
    public class MatchData
    {
        public int MatchId;
        public Guid Guid;
        public LobbyData LobbyData;

        public MatchData() { }
        public MatchData(int match_id, Guid guid, LobbyData lobby_data)
        {
            MatchId = match_id;
            Guid = guid;
            LobbyData = lobby_data;
        }
    }
}