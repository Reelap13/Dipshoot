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
            _stats = new();
            foreach (var stat_data in _preset.GetData())
                _stats.Add(stat_data.Stat, stat_data);
        }

        public float GetStatValue(Stat stat)
        {
            if (_stats.TryGetValue(stat, out StatData data))
                return data.GetStat();
            Debug.LogError($"Error: Try to get unsetted stat '{stat.ToString()}' on object {name}");
            return 0f;
        }
    }
}