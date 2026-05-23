using Game.TickSystem;
using Game.MatchMode;
using Server.Match;
using UnityEngine;

namespace Game
{
    public class DipshootGameController : GameController
    {
        [field: SerializeField] 
        public LevelCreator LevelCreator { get; private set; }
        [field: SerializeField]
        public TickManager TickManager { get; private set; }
        [field: SerializeField]
        public TeamControlModeController MatchModeController { get; private set; }

        public override void LoadGame(MatchController match_controller)
        {
            base.LoadGame(match_controller);
            LevelCreator.CreateLevel();
            MatchModeController.Initialize(this);
            MatchModeController.BeginMatch();
        }
    }
}
