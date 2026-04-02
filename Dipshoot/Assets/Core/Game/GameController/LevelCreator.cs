using Game.Level;
using Server.Match;
using UnityEngine;

namespace Game
{
    public class LevelCreator : MonoBehaviour
    {
        [field: SerializeField]
        public GameController GameController { get; private set; }
        [SerializeField] private LevelController _level_prefab;

        public LevelController Level { get; private set; }

        public void CreateLevel()
        {
            Level = NetworkUtils.NetworkMatchInstantiate(_level_prefab, GameController.Scene, GameController.MatchId, transform, transform);
            GameController.StartGame();
        }
    }
}