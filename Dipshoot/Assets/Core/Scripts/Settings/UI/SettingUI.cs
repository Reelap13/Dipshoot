using System;
using UnityEngine;
using UnityEngine.Events;

namespace Settings.UI
{
    public class SettingUI : MonoBehaviour
    {
        [NonSerialized] public UnityEvent<SettingUI> OnHoveredOver = new();

        [field: SerializeField]
        public SettingType Type { get; private set; }
        [SerializeField] private SettingUIVisualizer _visualizer;
        [SerializeField] private SettingsUIValueChanger _value_changer;

        public ISetting _setting;

        public void Initialize(SettingsUISection section)
        {
            _setting = SettingsController.Instance.GetSetting(Type);
            _visualizer.Initialize(section);
            _value_changer.Initialize(_setting);

            _value_changer.OnValueUpdated.AddListener(UpdateValue);
        }

        public void Activate()
        {
            _visualizer.ShowDescription();
        }

        public void Disactivate()
        {
            _visualizer.HideDescription();
        }

        public void ResetSetting()
        {
            _setting.ResetToDefault();
            _value_changer.UpdateValue(_setting.GetValue());
        }

        private void UpdateValue(object new_value)
        {
            _setting.SetValue(new_value);
        }

        private void OnMouseEnter()
        {
            OnHoveredOver.Invoke(this);
        }
    }
}