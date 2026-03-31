using UnityEngine;
using UnityEngine.UI;

namespace Settings.UI.ValueChader
{
    public class ToggleSUI : SettingsUIValueChanger
    {
        [SerializeField] private Toggle _toggle;

        public override void Initialize(ISetting setting)
        {
            BoolValueSetting bool_setting = (BoolValueSetting)setting;
            ConfigureToggle(bool_setting);

            _toggle.onValueChanged.AddListener(OnToggleValueUdated);
        }

        public override void UpdateValue(object value)
        {
            _toggle.isOn = (bool)value;
        }

        private void OnToggleValueUdated(bool value)
        {
            OnValueUpdated.Invoke(value);
        }

        private void ConfigureToggle(BoolValueSetting setting)
        {
            UpdateValue(setting.Value);
        }
    }
}