using System.Collections.Generic;
using System.Security.Cryptography;
using Settings.Types;
using UnityEngine;

namespace Settings.Applicator
{
    public class VideoApplicator : SettingsApplicator
    {
        private Dictionary<ResolutionType, Resolution> _resolutions = new()
        {
            { ResolutionType.R1920_1080, new Resolution { width = 1920, height = 1080 } },
            { ResolutionType.R2560_1440, new Resolution { width = 2560, height = 1440 } },
            { ResolutionType.R3840_2160, new Resolution { width = 3840, height = 2160 } },
            { ResolutionType.R3440_1440, new Resolution { width = 3440, height = 1440 } },

        };

        public override void Initialize()
        {
            SubscribeSettings();
            SetSettings();
        }

        private void SubscribeSettings()
        {
            ((IntListSetting)SettingsController.Instance.GetSetting(SettingType.RESOLUTION)).OnUpdatedValue.AddListener(SetResolution);
            ((BoolValueSetting)SettingsController.Instance.GetSetting(SettingType.FULLSCREEN)).OnUpdatedValue.AddListener(SetFullScreen);
        }

        private void SetSettings()
        {
            SetResolution((int)SettingsController.Instance.GetSetting(SettingType.RESOLUTION).GetValue());
            SetFullScreen((bool)SettingsController.Instance.GetSetting(SettingType.FULLSCREEN).GetValue());
        }

        private void SetResolution(int id)
        {
            Resolution resolution = _resolutions[(ResolutionType)id];
            Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        }

        private void SetFullScreen(bool is_full_screen)
        {
            Screen.SetResolution(Screen.width, Screen.height, is_full_screen);
        }
    }
}