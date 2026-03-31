using System.Collections.Generic;
using Settings;
using Settings.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace Settings.UI.ValueChader
{
    public class DropdownSUI : SettingsUIValueChanger
    {
        [SerializeField] private List<DropdownFieldData> _fields;
        [SerializeField] private TMP_Dropdown _dropdown;

        public override void Initialize(ISetting setting)
        {
            IntListSetting int_list_setting = (IntListSetting)setting;
            ConfigurateDropdown(int_list_setting);

            _dropdown.onValueChanged.AddListener(OnDropdownValueUdated);
        }

        public override void UpdateValue(object value)
        {
            _dropdown.value = FindIndex((int)value);
            _dropdown.RefreshShownValue();
        }

        private void OnDropdownValueUdated(int index)
        {
            int value = _fields[index].Id;
            OnValueUpdated.Invoke(value);
        }

        private void ConfigurateDropdown(IntListSetting setting)
        {
            _dropdown.ClearOptions();

            List<string> options = new();
            foreach (var field in _fields)
                options.Add(field.Name);
            _dropdown.AddOptions(options);

            UpdateValue(setting.Value);
        }

        private int FindIndex(int id)
        {
            for (int i = 0; i < _fields.Count; ++i)
                if (_fields[i].Id == id)
                    return i;
            return 0;
        }

        [System.Serializable]
        private class DropdownFieldData
        {
            public int Id;
            public string Name;
        }
    }
}