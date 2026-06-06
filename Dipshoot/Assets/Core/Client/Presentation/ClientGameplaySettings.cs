using System;
using UnityEngine;

namespace Core.ClientPresentation
{
    public static class ClientGameplaySettings
    {
        private const string MouseSensitivityKey = "Dipshoot.MouseSensitivity";
        private const string MasterVolumeKey = "Dipshoot.MasterVolume";

        public const float DefaultMouseSensitivity = 1f;
        public const float MinMouseSensitivity = 0.2f;
        public const float MaxMouseSensitivity = 3f;
        public const float DefaultMasterVolume = 1f;

        public static event Action<float> OnMasterVolumeUpdated;

        public static float MouseSensitivity =>
            Mathf.Clamp(PlayerPrefs.GetFloat(MouseSensitivityKey, DefaultMouseSensitivity), MinMouseSensitivity, MaxMouseSensitivity);

        public static float MasterVolume =>
            Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, DefaultMasterVolume));

        public static void SetMouseSensitivity(float value)
        {
            PlayerPrefs.SetFloat(MouseSensitivityKey, Mathf.Clamp(value, MinMouseSensitivity, MaxMouseSensitivity));
            PlayerPrefs.Save();
        }

        public static void SetMasterVolume(float value)
        {
            float clamped = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MasterVolumeKey, clamped);
            PlayerPrefs.Save();
            OnMasterVolumeUpdated?.Invoke(clamped);
        }
    }
}
