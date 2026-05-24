using System.Collections.Generic;
using UnityEngine;

namespace Scripts.Stats
{
    public class StatsController : MonoBehaviour
    {
        [SerializeField] private StatsPreset _preset;

        private Dictionary<Stat, StatData> _stats;

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (_stats != null)
                return;

            _stats = new();
            if (_preset == null)
                return;

            foreach (var stat_data in _preset.GetData())
                _stats[stat_data.Stat] = stat_data;
        }

        public float GetStatValue(Stat stat)
        {
            if (TryGetStatValue(stat, out float value))
                return value;

            Debug.LogError($"Error: Try to get unsetted stat '{stat.ToString()}' on object {name}");
            return 0f;
        }

        public float GetStatValue(Stat stat, float fallback_value)
        {
            return TryGetStatValue(stat, out float value)
                ? value
                : fallback_value;
        }

        public bool TryGetStatValue(Stat stat, out float value)
        {
            Initialize();

            if (_stats.TryGetValue(stat, out StatData data))
            {
                value = data.GetStat();
                return true;
            }

            value = 0f;
            return false;
        }
    }
}
