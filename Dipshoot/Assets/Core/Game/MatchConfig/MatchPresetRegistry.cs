using System.Collections.Generic;
using UnityEngine;

namespace Game.MatchConfig
{
    [CreateAssetMenu(menuName = "Dipshoot/Match/Match Preset Registry", fileName = "MatchPresetRegistry")]
    public sealed class MatchPresetRegistry : ScriptableObject
    {
        private const string ResourceName = "MatchPresetRegistry";

        [SerializeField] private List<MatchPreset> _presets = new();

        private static MatchPresetRegistry _cached;

        public IReadOnlyList<MatchPreset> Presets => _presets;

        public static MatchPresetRegistry LoadDefault()
        {
            if (_cached == null)
                _cached = Resources.Load<MatchPresetRegistry>(ResourceName);

            return _cached;
        }

        public static MatchPreset GetPreset(string id)
        {
            MatchPresetRegistry registry = LoadDefault();
            return registry == null ? null : registry.GetById(id);
        }

        public MatchPreset GetDefault()
        {
            return _presets != null && _presets.Count > 0 ? _presets[0] : null;
        }

        public MatchPreset GetById(string id)
        {
            if (_presets == null)
                return null;

            for (int i = 0; i < _presets.Count; i++)
            {
                MatchPreset preset = _presets[i];
                if (preset != null && preset.Id == id)
                    return preset;
            }

            return null;
        }
    }
}
