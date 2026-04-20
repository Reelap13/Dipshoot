using Game.TickSystem;
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

        public override void LoadGame(MatchController match_controller)
        {
            base.LoadGame(match_controller);
            LevelCreator.CreateLevel();
        }
    }
}