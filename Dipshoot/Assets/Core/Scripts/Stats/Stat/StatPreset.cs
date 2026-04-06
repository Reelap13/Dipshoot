using UnityEngine;

namespace Scripts.Stats
{
    [CreateAssetMenu(fileName = "Stat", menuName = "Game/Stats/StatPreset")]
    public class StatPreset : ScriptableObject
    {
        public Stat Stat;
        public string Description;
    }
}
