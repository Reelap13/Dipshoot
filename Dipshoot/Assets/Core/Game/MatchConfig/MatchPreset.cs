using Game.ProcGen.Warehouse;
using UnityEngine;

namespace Game.MatchConfig
{
    [CreateAssetMenu(menuName = "Dipshoot/Match/Match Preset", fileName = "MatchPreset")]
    public sealed class MatchPreset : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        public WarehouseRecipe Recipe;
        public int Seed;
        public int RoundsCount = 1;
        public string ResultUrl;
    }
}
