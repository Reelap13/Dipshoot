using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;

namespace Settings.Applicator
{
    public class AudioApplicator : SettingsApplicator
    {
        [SerializeField] private AudioMixer _audio_mixer;

        public override void Initialize()
        {
            SubscribeSettings();
            SetSettings();
        }

        private void SubscribeSettings()
        {
            ((FloatValueSetting)SettingsController.Instance.GetSetting(SettingType.MASTER_VOLUME)).OnUpdatedValue.AddListener(SetMasterVolume);
            ((FloatValueSetting)SettingsController.Instance.GetSetting(SettingType.SFX_VOLUME)).OnUpdatedValue.AddListener(SetSFXVolume);
            ((FloatValueSetting)SettingsController.Instance.GetSetting(SettingType.MUSIC_VOLUME)).OnUpdatedValue.AddListener(SetMusicVolume);
            ((FloatValueSetting)SettingsController.Instance.GetSetting(SettingType.VOICE_VOLUME)).OnUpdatedValue.AddListener(SetVoiceVolume);
        }

        private void SetSettings()
        {
            SetMasterVolume((float)SettingsController.Instance.GetSetting(SettingType.MASTER_VOLUME).GetValue());
            SetSFXVolume((float)SettingsController.Instance.GetSetting(SettingType.SFX_VOLUME).GetValue());
            SetMusicVolume((float)SettingsController.Instance.GetSetting(SettingType.MUSIC_VOLUME).GetValue());
            SetVoiceVolume((float)SettingsController.Instance.GetSetting(SettingType.VOICE_VOLUME).GetValue());
        }

        private void SetMasterVolume(float value)
        {
            SetAudioMixerValue("MasterVolume", value);
        }
        private void SetSFXVolume(float value)
        {
            SetAudioMixerValue("SFXVolume", value);
        }
        private void SetMusicVolume(float value)
        {
            SetAudioMixerValue("MusicVolume", value);
        }
        private void SetVoiceVolume(float value)
        {
            SetAudioMixerValue("VoiceVolume", value);
        }

        private void SetAudioMixerValue(string key, float value)
        {
            _audio_mixer.SetFloat(key, Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20);
        }
    }
}