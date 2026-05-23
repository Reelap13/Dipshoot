using Game.MatchMode;
using Mirror;
using UnityEngine;

namespace Game.Players
{
    [DisallowMultipleComponent]
    public class PlayerMatchIdentity : NetworkBehaviour
    {
        [SyncVar] private int _player_id = -1;
        [SyncVar] private TeamId _team_id = TeamId.None;

        public int PlayerId => _player_id;
        public TeamId TeamId => _team_id;
        public bool IsAssigned => _player_id >= 0 && _team_id != TeamId.None;

        public void InitializeServer(int player_id, TeamId team_id)
        {
            if (!isServer)
                return;

            _player_id = player_id;
            _team_id = team_id;
        }
    }
}
