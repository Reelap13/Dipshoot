using Server.Lobby;
using UnityEngine;

namespace Server.Match
{
    public class MatchesContoller : Singleton<MatchesContoller>
    {
        public void StartMatch(LobbyData data)
        {
            Debug.Log("StartMatch");
        }
    }
}