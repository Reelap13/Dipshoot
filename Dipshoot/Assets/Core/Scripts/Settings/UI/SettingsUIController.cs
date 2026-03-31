using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace Settings.UI
{
    public class SettingsUIController : MonoBehaviour
    {
        [SerializeField] private List<SettingsUISection> _sections;

        private SettingsUISection _active_sections;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            foreach (var section in _sections)
            {
                section.Initialize();
                section.Disactivate();
                section.OnActivationButtonPushed.AddListener(UpdateActiveSection);
            }
            UpdateActiveSection(_sections[0]);
        }

        private void UpdateActiveSection(SettingsUISection section)
        {
            if (_active_sections)
            {
                _active_sections.Disactivate();
            }

            _active_sections = section;
            _active_sections.Activate();
        }

        public void ResetSettings()
        {
            foreach (var section in _sections) 
                section.ResetSettingsInSection();
        }
    }
}