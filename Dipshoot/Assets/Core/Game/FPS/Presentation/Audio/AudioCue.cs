using System;
using UnityEngine;
using UnityEngine.Audio;

namespace Game.Players
{
    [CreateAssetMenu(fileName = "AudioCue", menuName = "Game/Audio/AudioCue")]
    public class AudioCue : ScriptableObject
    {
        [SerializeField] private AudioClip[] _clips = Array.Empty<AudioClip>();
        [SerializeField] private AudioMixerGroup _output_mixer_group;
        [SerializeField] private Vector2 _volume_range = Vector2.one;
        [SerializeField] private Vector2 _pitch_range = Vector2.one;
        [SerializeField] private float _spatial_blend = 1f;
        [SerializeField] private float _min_distance = 1f;
        [SerializeField] private float _max_distance = 24f;
        [SerializeField] private AudioRolloffMode _rolloff_mode = AudioRolloffMode.Logarithmic;
        [SerializeField] private float _cooldown = 0f;

        public AudioMixerGroup OutputMixerGroup => _output_mixer_group;
        public float SpatialBlend => _spatial_blend;
        public float MinDistance => _min_distance;
        public float MaxDistance => _max_distance;
        public AudioRolloffMode RolloffMode => _rolloff_mode;
        public float Cooldown => _cooldown;

        public bool TryGetClip(out AudioClip clip)
        {
            clip = null;
            if (_clips == null || _clips.Length == 0)
                return false;

            clip = _clips[UnityEngine.Random.Range(0, _clips.Length)];
            return clip != null;
        }

        public float GetVolume()
        {
            return UnityEngine.Random.Range(_volume_range.x, _volume_range.y);
        }

        public float GetPitch()
        {
            return UnityEngine.Random.Range(_pitch_range.x, _pitch_range.y);
        }
    }
}
