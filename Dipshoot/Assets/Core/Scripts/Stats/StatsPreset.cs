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
            if (_stats == null)
                return data;

            foreach (var stat_value in _stats)
            {
                if (stat_value.Preset == null)
                    continue;

                data.Add(stat_value.GetStatData());
            }

            return data;
        }

        [System.Serializable]
        private class StatValue
        {
            public StatPreset Preset = default;
            public float Value = default;

            public StatData GetStatData() => new(Preset.Stat, Value);
        }
    }
}
