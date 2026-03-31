using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

namespace Settings.UI.ValueChader
{
    public class FloatSliderSUI : SettingsUIValueChanger
    {
        [SerializeField] private Slider _slider;
        [SerializeField] private bool _in_text_displayed = true;
        [SerializeField] private TextMeshProUGUI _text_field;

        public override void Initialize(ISetting setting)
        {
            FloatValueSetting float_setting = (FloatValueSetting)setting;
            ConfigureSlider(float_setting);

            _slider.onValueChanged.AddListener(OnSliderValueUdated);
        }
        
        public override void UpdateValue(object value)
        {
            _slider.value = (float)value;
        }

        private void OnSliderValueUdated(float value)
        {
            if (_in_text_displayed)
                _text_field.text = value.ToString("F2");
            OnValueUpdated.Invoke(value);
        }

        private void ConfigureSlider(FloatValueSetting setting)
        {
            _slider.minValue = setting.Limits.x;
            _slider.maxValue = setting.Limits.y;

            if (_text_field == null)
                _in_text_displayed = false;
            if (_in_text_displayed)
            {
                _text_field.gameObject.SetActive(true);
                _text_field.text = setting.Value.ToString("F2");
            }

            UpdateValue(setting.Value);
        }
    }
}