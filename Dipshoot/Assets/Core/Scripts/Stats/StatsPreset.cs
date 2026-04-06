using System.Collections.Generic;
using UnityEngine;

namespace Scripts.Stats
{
    [CreateAssetMenu(fileName = "Stat", menuName = "Game/Stats/StatsPreset")]
    public class StatsPreset : ScriptableObject
    {
        [SerializeField] private List<StatValue> _stats;

        public List<StatData> GetData()
        {
            List<StatData> data = new();
            foreach (var stat_value in _stats)
                data.Add(stat_value.GetStatData());
            return data;
        }

        [System.Serializable]
        private class StatValue
        {
            public StatPreset Preset;
            public float Value;

            public StatData GetStatData() => new(Preset.Stat, Value);
        }
    }
}