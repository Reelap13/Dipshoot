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
            string preset_id = GameController.MatchController.MatchData.LobbyData.SelectedPresetId;
            MatchPreset preset = MatchPresetRegistry.GetPreset(preset_id);
            if (preset == null)
            {
                MatchPresetRegistry registry = MatchPresetRegistry.LoadDefault();
                preset = registry == null ? null : registry.GetDefault();
                Debug.LogWarning($"Server match preset not found: {preset_id}. Fallback to {preset?.Id ?? "none"}.");
            }

            if (preset != null && preset.Recipe != null)
                Level.GenerateWarehouseLevel(preset.Recipe, GameController.MatchController.MatchData.LobbyData.SelectedSeed);
        }
    }
}
