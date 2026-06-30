using System.Collections;
using System.Collections.Generic;
using Game.MatchConfig;
using Game.ProcGen.Warehouse;
using UnityEngine;

namespace Game.ProcGen
{
    public enum MapSwitchSpeedFormula
    {
        Linear,
        AccelerateTowardsEnd,
    }

    public sealed class GenerationShower : MonoBehaviour
    {
        [SerializeField] private MatchPresetRegistry _preset_registry;
        [SerializeField] private Transform _maps_parent;
        [SerializeField] private Camera _map_camera;
        [SerializeField] private int _shuffle_seed = 12345;
        [SerializeField, Min(0.05f)] private float _switch_interval = 3f;
        [SerializeField] private MapSwitchSpeedFormula _speed_formula = MapSwitchSpeedFormula.Linear;
        [SerializeField, Range(0.05f, 1f)] private float _end_interval_multiplier = 0.25f;
        [SerializeField, Min(1)] private int _cycles_to_full_speed = 2;
        [SerializeField] private float _map_padding = 1.12f;
        [SerializeField] private float _map_rotation_degrees = 90f;
        [SerializeField] private float _map_camera_pitch_degrees = 58f;
        [SerializeField] private float _map_camera_fov = 35f;
        [SerializeField] private float _map_camera_distance_multiplier = 1.25f;
        [SerializeField] private bool _loop = true;
        [SerializeField] private bool _play_on_start = true;
        [SerializeField] private bool _log_diagnostics;

        private readonly List<PreloadedMap> _maps = new();
        private Coroutine _show_routine;
        private int _current_index = -1;

        public MatchPreset CurrentPreset =>
            _current_index >= 0 && _current_index < _maps.Count
                ? _maps[_current_index].Preset
                : null;

        private void Awake()
        {
            _map_camera ??= Camera.main;
            PreloadMaps();
            if (_maps.Count > 0)
                ShowMap(0);
        }

        private void Start()
        {
            if (_play_on_start)
                Play();
        }

        public void Play()
        {
            if (_show_routine != null || _maps.Count == 0)
                return;

            _show_routine = StartCoroutine(ShowMaps());
        }

        public void Stop()
        {
            if (_show_routine == null)
                return;

            StopCoroutine(_show_routine);
            _show_routine = null;
        }

        public void ShowNext()
        {
            if (_maps.Count == 0)
                return;

            int next_index = (_current_index + 1) % _maps.Count;
            ShowMap(next_index);
        }

        private void PreloadMaps()
        {
            _preset_registry ??= MatchPresetRegistry.LoadDefault();
            if (_preset_registry == null)
            {
                Debug.LogError($"{nameof(GenerationShower)} cannot find MatchPresetRegistry.", this);
                return;
            }

            _maps_parent = GetOrCreateMapsParent();
            List<MatchPreset> presets = GetShuffledPresets();
            for (int i = 0; i < presets.Count; i++)
            {
                MatchPreset preset = presets[i];

                GameObject map_root = new($"Map_{i:000}_{preset.Id}");
                map_root.transform.SetParent(_maps_parent, false);

                try
                {
                    WarehouseLevelGenerator.GenerateInto(
                        map_root.transform,
                        preset.Recipe,
                        preset.Seed,
                        "GeneratedWarehouse",
                        _log_diagnostics);
                    HideSpawnMarkerRenderers(map_root.transform);
                    map_root.SetActive(false);
                    _maps.Add(new PreloadedMap(preset, map_root));
                }
                catch (System.Exception exception)
                {
                    Debug.LogError(
                        $"{nameof(GenerationShower)} failed to preload preset '{preset.Id}': {exception}",
                        this);
                    Destroy(map_root);
                }
            }
        }

        private List<MatchPreset> GetShuffledPresets()
        {
            List<MatchPreset> presets = new();
            for (int i = 0; i < _preset_registry.Presets.Count; i++)
            {
                MatchPreset preset = _preset_registry.Presets[i];
                if (preset != null && preset.Recipe != null)
                    presets.Add(preset);
            }

            System.Random random = new(_shuffle_seed);
            for (int i = presets.Count - 1; i > 0; i--)
            {
                int swap_index = random.Next(i + 1);
                (presets[i], presets[swap_index]) = (presets[swap_index], presets[i]);
            }

            return presets;
        }

        private IEnumerator ShowMaps()
        {
            int cycle_index = 0;
            do
            {
                for (int i = 0; i < _maps.Count; i++)
                {
                    ShowMap(i);
                    yield return new WaitForSecondsRealtime(GetInterval(i, cycle_index));
                }

                cycle_index++;
            }
            while (_loop || cycle_index < Mathf.Max(1, _cycles_to_full_speed));

            _show_routine = null;
        }

        private void ShowMap(int index)
        {
            if (index < 0 || index >= _maps.Count || index == _current_index)
                return;

            if (_current_index >= 0 && _current_index < _maps.Count)
                _maps[_current_index].Root.SetActive(false);

            _current_index = index;
            _maps[_current_index].Root.SetActive(true);
            FrameMapCamera(_maps[_current_index].Root.transform);
        }

        private float GetInterval(int index, int cycle_index)
        {
            float interval = Mathf.Max(0.05f, _switch_interval);
            if (_speed_formula == MapSwitchSpeedFormula.Linear || _maps.Count <= 1)
                return interval;

            int cycles = Mathf.Max(1, _cycles_to_full_speed);
            int total_steps = Mathf.Max(1, _maps.Count * cycles - 1);
            int current_step = cycle_index * _maps.Count + index;
            float progress = Mathf.Clamp01((float)current_step / total_steps);
            float multiplier = Mathf.Lerp(
                1f,
                Mathf.Clamp(_end_interval_multiplier, 0.05f, 1f),
                progress);
            return interval * multiplier;
        }

        private Transform GetOrCreateMapsParent()
        {
            if (_maps_parent != null)
                return _maps_parent;

            Transform existing = transform.Find("PreloadedMaps");
            if (existing != null)
                return existing;

            Transform parent = new GameObject("PreloadedMaps").transform;
            parent.SetParent(transform, false);
            return parent;
        }

        private void FrameMapCamera(Transform map_root)
        {
            if (_map_camera == null || !TryGetMapBounds(map_root, out Bounds bounds))
                return;

            float aspect = Mathf.Max(0.1f, _map_camera.aspect);
            float vertical_fov = Mathf.Clamp(_map_camera_fov, 10f, 90f) * Mathf.Deg2Rad;
            float frame_size = Mathf.Max(bounds.size.z, bounds.size.x / aspect);
            float distance = frame_size * 0.5f / Mathf.Tan(vertical_fov * 0.5f);
            distance *= Mathf.Max(0.1f, _map_padding * _map_camera_distance_multiplier);

            Vector3 center = bounds.center;
            center.y = bounds.center.y + bounds.extents.y * 0.25f;
            Quaternion rotation = Quaternion.Euler(
                _map_camera_pitch_degrees,
                _map_rotation_degrees,
                0f);

            _map_camera.transform.SetPositionAndRotation(
                center - rotation * Vector3.forward * distance,
                rotation);
            _map_camera.orthographic = false;
            _map_camera.fieldOfView = _map_camera_fov;
            _map_camera.clearFlags = CameraClearFlags.SolidColor;
            _map_camera.backgroundColor = new Color(0.04f, 0.045f, 0.05f, 1f);
            _map_camera.nearClipPlane = 0.1f;
            _map_camera.farClipPlane = distance + bounds.size.magnitude + 50f;
        }

        private static bool TryGetMapBounds(Transform map_root, out Bounds bounds)
        {
            bounds = default;
            if (map_root == null)
                return false;

            Renderer[] renderers = map_root.GetComponentsInChildren<Renderer>(false);
            bool has_bounds = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled)
                    continue;

                if (!has_bounds)
                {
                    bounds = renderer.bounds;
                    has_bounds = true;
                    continue;
                }

                bounds.Encapsulate(renderer.bounds);
            }

            return has_bounds;
        }

        private static void HideSpawnMarkerRenderers(Transform root)
        {
            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];
                if (child == null || child.name != "SpawnA" && child.name != "SpawnB")
                    continue;

                Renderer[] renderers = child.GetComponentsInChildren<Renderer>(true);
                for (int j = 0; j < renderers.Length; j++)
                {
                    if (renderers[j] != null)
                        renderers[j].enabled = false;
                }
            }
        }

        private readonly struct PreloadedMap
        {
            public readonly MatchPreset Preset;
            public readonly GameObject Root;

            public PreloadedMap(MatchPreset preset, GameObject root)
            {
                Preset = preset;
                Root = root;
            }
        }
    }
}
