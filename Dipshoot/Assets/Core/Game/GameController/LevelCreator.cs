using Game.Level;
using Game.MatchConfig;
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
            MatchPreset preset = MatchPresetRegistry.GetPreset(GameController.MatchController.MatchData.LobbyData.SelectedPresetId);
            if (preset != null && preset.Recipe != null)
                Level.GenerateWarehouseLevel(preset.Recipe, GameController.MatchController.MatchData.LobbyData.SelectedSeed);
        }
    }
}
