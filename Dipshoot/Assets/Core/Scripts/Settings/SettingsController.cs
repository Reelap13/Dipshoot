using System.Collections.Generic;
using Settings.Applicator;
using UnityEngine;

namespace Settings
{
    public class SettingsController : Singleton<SettingsController>
    {
        [SerializeField] private SettingsSection[] _sections_array;
        [SerializeField] private List<SettingsApplicator> _applicators;

        private Dictionary<int, SettingsSection> _sections;
        private Dictionary<int, ISetting> _settings;

        private void Awake()
        {
            InitializeSettings();
            LoadSettings();
            InitializeApplicators();
        }

        private void InitializeSettings()
        {
            _sections = new();
            _settings = new();
            foreach (var section in _sections_array)
            {
                _sections.Add(section.Id, section);
                foreach (var setting in section.Settings)
                {
                    if (!_settings.ContainsKey(setting.Id))
                        _settings[setting.Id] = setting;
                    else Debug.LogError($"Two settings has the save SettingType {setting.Type}");
                }
            }
        }

        private void InitializeApplicators()
        {
            foreach (var applicator in _applicators)
                applicator.Initialize();
        }

        private void LoadSettings()
        {
            foreach (var setting in _settings.Values)
                setting.Load();
        }

        public SettingsSection GetSection(SettingSectionType type) => GetSection((int)type);  
        private SettingsSection GetSection(int id) => _sections[id];

        public ISetting GetSetting(SettingType type) => GetSetting((int)type);
        private ISetting GetSetting(int id) => _settings[id];
    }
}