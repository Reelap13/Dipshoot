using System;
using System.Collections.Generic;
using Mirror;
using Server.Lobby;
using UnityEngine;

namespace Server.Match
{
    public class MatchesContoller : Singleton<MatchesContoller>
    {
        [SerializeField] private MatchController _match_controller_pref;

        private int _id = 0;
        private Dictionary<int, MatchController> _matches = new();

        public void StartMatch(LobbyData data)
        {
            MatchData match_data = new(_id++, Guid.NewGuid(), data);
            MatchController match_controller = CreateMatchController();
            _matches.Add(match_data.MatchId, match_controller);
            match_controller.LoadMatch(match_data);
        }

        public MatchController CreateMatchController() => Instantiate(_match_controller_pref, transform);
        public MatchController GetMatchController(int match_id) => _matches[match_id];
    }
}