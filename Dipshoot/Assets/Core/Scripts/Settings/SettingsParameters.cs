using UnityEngine;

namespace Settings
{
    public class SettingsParameters : Singleton<SettingsParameters>
    {
        [SerializeField] private float mouseSensitivity;

        public void SetMouseSensitivity(float value)
        {
            mouseSensitivity = value;
        }

        public float MouseSensitivity { get { return mouseSensitivity; } private set { } }
    }
}
