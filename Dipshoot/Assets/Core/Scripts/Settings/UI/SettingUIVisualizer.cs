using TMPro;
using UnityEngine;

namespace Settings.UI
{
    public class SettingUIVisualizer : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _name_area;

        [SerializeField, TextArea()] private string _name_text;
        [SerializeField, TextArea()] private string _description_text;

        private SettingsUISection _section;

        public TextMeshProUGUI DescriptionField => _section.DescriptionField;

        public void Initialize(SettingsUISection section)
        {
            _name_area.text = _name_text;
            _section = section;
        }

        public void ShowDescription()
        {
            DescriptionField.text = _description_text;
        }

        public void HideDescription()
        {
            DescriptionField.text = "";
        }
    }
}