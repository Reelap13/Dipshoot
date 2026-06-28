using System.Collections.Generic;
using UnityEngine;

namespace Game.Players
{
    [DefaultExecutionOrder(-5000)]
    public class GameAudioService : MonoBehaviour
    {
        private const int DefaultPoolSize = 16;

        private static GameAudioService _instance;

        [SerializeField] private int _initial_pool_size = DefaultPoolSize;

        private readonly List<AudioSource> _sources = new();
        private readonly Dictionary<long, float> _last_play_times = new();

        public static GameAudioService Instance
        {
            get
            {
                if (_instance == null)
                    CreateInstance();

                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            Warmup();
        }

        public void Play(AudioCue cue, Vector3 position, bool force_2d)
        {
            Play(cue, position, force_2d, 0, false);
        }

        public void Play(
            AudioCue cue,
            Vector3 position,
            bool force_2d,
            int emitter_id,
            bool ignore_cooldown = false)
        {
            if (cue == null ||
                !cue.TryGetClip(out AudioClip clip) ||
                !CanPlay(cue, emitter_id, ignore_cooldown))
            {
                return;
            }

            AudioSource source = GetSource();
            source.transform.position = position;
            source.outputAudioMixerGroup = cue.OutputMixerGroup;
            source.clip = clip;
            source.volume = cue.GetVolume();
            source.pitch = cue.GetPitch();
            source.spatialBlend = force_2d ? 0f : cue.SpatialBlend;
            source.minDistance = cue.MinDistance;
            source.maxDistance = cue.MaxDistance;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.loop = false;
            source.Play();

            if (!ignore_cooldown && cue.Cooldown > 0f)
                _last_play_times[GetCooldownKey(cue, emitter_id)] = Time.unscaledTime;
        }

        private static void CreateInstance()
        {
            GameObject target = new(nameof(GameAudioService));
            _instance = target.AddComponent<GameAudioService>();
        }

        private void Warmup()
        {
            for (int i = _sources.Count; i < _initial_pool_size; i++)
                CreateSource();
        }

        private bool CanPlay(
            AudioCue cue,
            int emitter_id,
            bool ignore_cooldown)
        {
            if (ignore_cooldown || cue.Cooldown <= 0f)
                return true;

            long key = GetCooldownKey(cue, emitter_id);
            return !_last_play_times.TryGetValue(key, out float last_time) ||
                Time.unscaledTime - last_time >= cue.Cooldown;
        }

        private static long GetCooldownKey(AudioCue cue, int emitter_id)
        {
            return ((long)cue.GetInstanceID() << 32) ^ (uint)emitter_id;
        }

        private AudioSource GetSource()
        {
            for (int i = 0; i < _sources.Count; i++)
            {
                if (!_sources[i].isPlaying)
                    return _sources[i];
            }

            return CreateSource();
        }

        private AudioSource CreateSource()
        {
            GameObject target = new("AudioSource");
            target.transform.SetParent(transform, false);
            AudioSource source = target.AddComponent<AudioSource>();
            source.playOnAwake = false;
            _sources.Add(source);
            return source;
        }
    }
}
