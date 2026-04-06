using UnityEngine;

namespace Scripts.Stats
{
    [System.Serializable]
    public class StatData
    {
        public Stat Stat;
        public float BaseValue;
        public float FlatBonus;
        public float PercentBonus;

        public StatData() { }
        public StatData(Stat stat, float base_value)
        {
            Stat = stat;
            BaseValue = base_value;
            FlatBonus = 0;
            PercentBonus = 1f;
        }

        public float GetStat() => (BaseValue + FlatBonus) * PercentBonus;
    }
}