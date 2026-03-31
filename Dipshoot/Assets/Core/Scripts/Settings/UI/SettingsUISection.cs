using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Settings.UI
{
    public class SettingsUISection : MonoBehaviour
    {
        [NonSerialized] public UnityEvent<SettingsUISection> OnActivationButtonPushed = new();

        [field: SerializeField]
        public SettingSectionType Type { get; private set; }
        [field: SerializeField]
        public Button SectionButton { get; private set; }
        [field: SerializeField]
        public TextMeshProUGUI DescriptionField { get; private set; }

        [SerializeField] private List<SettingUI> _settings;

        private SettingUI _active_setting;

        public void Initialize()
        {
            SectionButton.onClick.AddListener(() => OnActivationButtonPushed.Invoke(this));
            foreach (var setting in _settings)
            {
                setting.Initialize(this);
                setting.OnHoveredOver.AddListener(UpdateActiveSetting);
            }
        }

        private void UpdateActiveSetting(SettingUI setting)
        {
            if (_active_setting)
            {
                _active_setting.Disactivate();
            }

            _active_setting = setting;
            _active_setting.Activate();
        }

        public void Activate()
        {
            gameObject.SetActive(true);
        }

        public void Disactivate()
        {
            _active_setting?.Disactivate();
            _active_setting = null;
            gameObject.SetActive(false);
        }

        public void ResetSettingsInSection()
        {
            foreach (var setting in _settings)
                setting.ResetSetting();
        }
    }
}