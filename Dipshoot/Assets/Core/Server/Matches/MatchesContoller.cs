using System;
using System.Collections.Generic;
using Mirror;
using Server.Data;
using Server.Lobby;
using Server.ServerSide;
using UnityEngine;

namespace Server.Match
{
    public class MatchesContoller : Singleton<MatchesContoller>
    {
        [SerializeField] private MatchController _match_controller_pref;

        private int _id = 0;
        private Dictionary<int, MatchController> _matches = new();

        public int ActiveMatchesCount => _matches.Count;

        public int ConnectedPlayersCount
        {
            get
            {
                int count = 0;
                foreach (MatchController match_controller in _matches.Values)
                {
                    LobbyData lobby = match_controller?.MatchData?.LobbyData;
                    if (lobby?.Players == null)
                        continue;

                    for (int i = 0; i < lobby.Players.Count; i++)
                    {
                        Player player = PlayersController.Instance.GetPlayer(lobby.Players[i].PlayerId);
                        if (player != null && player.IsHasClient())
                            count++;
                    }
                }

                return count;
            }
        }

        public void StartMatch(LobbyData data)
        {
            MatchData match_data = new(_id++, Guid.NewGuid(), data);
            MatchController match_controller = CreateMatchController();
            _matches.Add(match_data.MatchId, match_controller);
            match_controller.LoadMatch(match_data);
            NotifyPopulationChanged();
        }

        public MatchController CreateMatchController() => Instantiate(_match_controller_pref, transform);
        public MatchController GetMatchController(int match_id) => _matches[match_id];
        public void RemoveMatch(int match_id)
        {
            _matches.Remove(match_id);
            NotifyPopulationChanged();
        }

        public void NotifyPopulationChanged()
        {
            if (LobbiesController.Instance != null)
                LobbiesController.Instance.NotifyPopulationChanged();
        }

        public bool TryProcessPlayerLeave(Player player, int lobby_id)
        {
            foreach (MatchController match_controller in _matches.Values)
            {
                if (match_controller == null ||
                    match_controller.MatchData == null ||
                    match_controller.MatchData.LobbyData == null ||
                    match_controller.MatchData.LobbyData.Id != lobby_id)
                    continue;

                return match_controller.PlayersController != null &&
                    match_controller.PlayersController.ProcessPlayerLeave(player);
            }

            return false;
        }
    }
}
