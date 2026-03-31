using System.Collections.Generic;
using UnityEngine;

namespace Settings
{
    public class SettingsSection : MonoBehaviour
    {
        [field: SerializeField]
        public SettingSectionType Type { get; private set; }

        [field: SerializeField] 
        public List<GameObject> SettingObjects { get; private set; }

        private List<ISetting> _settings;

        public List<ISetting> Settings
        {
            get
            {
                if (_settings == null)
                    Initialize();

                return _settings;
            }
        }

        private void Initialize()
        {
            _settings = new List<ISetting>();
            foreach (var obj in SettingObjects)
            {
                ISetting[] settings = obj.GetComponents<ISetting>();
                if (settings.Length == 0)
                    Debug.LogError($"Section {Type.ToString()} try load {obj.transform.GetFullPath()} without ISetting components");
                foreach (var setting in settings) 
                    _settings.Add(setting);
            }
        }

        public int Id => (int)Type;
    }
}