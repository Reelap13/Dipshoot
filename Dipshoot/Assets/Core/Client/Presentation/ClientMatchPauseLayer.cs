using Game.MatchMode;
using Game.Level;
using Game.Players;
using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Core.ClientPresentation
{
    public class ClientMatchPauseLayer : MonoBehaviour
    {
        [SerializeField] private Slider _mouse_sensitivity_slider;
        [SerializeField] private TextMeshProUGUI _mouse_sensitivity_value_text;
        [SerializeField] private Slider _master_volume_slider;
        [SerializeField] private TextMeshProUGUI _master_volume_value_text;
        [SerializeField] private RawImage _map_image;
        [SerializeField] private Camera _map_camera;
        [SerializeField] private GameObject _intro_timer_panel;
        [SerializeField] private TextMeshProUGUI _intro_timer_text;
        [SerializeField] private TextMeshProUGUI _player_team_text;
        [SerializeField] private Image _red_spawn_flag;
        [SerializeField] private Image _blue_spawn_flag;
        [SerializeField] private AudioMixer _audio_mixer;
        [SerializeField] private string _master_volume_parameter = "MasterVolume";
        [SerializeField] private float _map_padding = 1.12f;
        [SerializeField] private float _map_rotation_degrees = 90f;
        [SerializeField] private float _map_camera_pitch_degrees = 58f;
        [SerializeField] private float _map_camera_fov = 35f;
        [SerializeField] private float _map_camera_distance_multiplier = 1.25f;
        [SerializeField] private float _map_texture_resolution_scale = 1.5f;
        [SerializeField] private int _map_texture_max_size = 2048;

        private ClientUiLayer _layer;
        private RenderTexture _map_texture;
        private bool _is_intro_auto_open;
        private int _last_intro_round = -1;
        private Vector2 _last_map_rect_size;

        private void Awake()
        {
            _layer = GetOrAddLayer();
            _layer.Initialize(ClientUiLayerKind.MatchPause);

            CachePrefabReferences();
            InitializeSliders();
            InitializeMapCamera();
            ApplyMasterVolume(ClientGameplaySettings.MasterVolume);
        }

        private void OnDestroy()
        {
            if (_mouse_sensitivity_slider != null)
                _mouse_sensitivity_slider.onValueChanged.RemoveListener(SetMouseSensitivity);
            if (_master_volume_slider != null)
                _master_volume_slider.onValueChanged.RemoveListener(SetMasterVolume);

            if (_map_texture != null)
            {
                _map_texture.Release();
                Destroy(_map_texture);
            }
        }

        private void Update()
        {
            HandleEscape();
            HandleIntroAutoOpen();
            UpdateIntroTimer();
            UpdatePlayerTeamText();

            bool is_visible = ClientAppRoot.Instance.PresentationRoot.State == ClientPresentationState.MatchPause;
            if (_map_camera != null)
                _map_camera.enabled = is_visible;

            if (is_visible)
                RefreshMapFrameIfNeeded();
        }

        private void HandleEscape()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame)
                return;

            ClientPresentationRoot presentation = ClientAppRoot.Instance.PresentationRoot;
            if (presentation.State == ClientPresentationState.Match)
            {
                OpenPause(false);
                return;
            }

            if (presentation.State == ClientPresentationState.MatchPause)
                ClosePause();
        }

        private void HandleIntroAutoOpen()
        {
            ClientMatchStore match_store = ClientAppRoot.Instance.MatchStore;
            TeamControlModeController mode = match_store.ModeController;
            if (mode == null)
            {
                if (!match_store.HasActiveMatch)
                    _is_intro_auto_open = false;

                _last_intro_round = -1;
                return;
            }

            if (mode.Phase == RoundPhase.Intro && mode.CurrentRound != _last_intro_round)
            {
                if (OpenPause(true))
                    _last_intro_round = mode.CurrentRound;
                return;
            }

            if (_is_intro_auto_open &&
                mode.Phase == RoundPhase.Playing &&
                ClientAppRoot.Instance.PresentationRoot.State == ClientPresentationState.MatchPause)
            {
                ClosePause();
            }
        }

        private bool OpenPause(bool is_intro_auto_open)
        {
            ClientPresentationRoot presentation = ClientAppRoot.Instance.PresentationRoot;
            if (presentation.State != ClientPresentationState.Match)
                return false;

            _is_intro_auto_open = is_intro_auto_open;
            FrameMapCamera();
            presentation.SetState(ClientPresentationState.MatchPause);
            return true;
        }

        private void ClosePause()
        {
            _is_intro_auto_open = false;
            ClientAppRoot.Instance.PresentationRoot.SetState(ClientPresentationState.Match);
        }

        private void InitializeSliders()
        {
            if (_mouse_sensitivity_slider != null)
            {
                _mouse_sensitivity_slider.minValue = ClientGameplaySettings.MinMouseSensitivity;
                _mouse_sensitivity_slider.maxValue = ClientGameplaySettings.MaxMouseSensitivity;
                _mouse_sensitivity_slider.SetValueWithoutNotify(ClientGameplaySettings.MouseSensitivity);
                _mouse_sensitivity_slider.onValueChanged.AddListener(SetMouseSensitivity);
            }

            if (_master_volume_slider != null)
            {
                _master_volume_slider.minValue = 0f;
                _master_volume_slider.maxValue = 1f;
                _master_volume_slider.SetValueWithoutNotify(ClientGameplaySettings.MasterVolume);
                _master_volume_slider.onValueChanged.AddListener(SetMasterVolume);
            }

            UpdateValueTexts();
        }

        private void CachePrefabReferences()
        {
            if (_intro_timer_panel == null)
                _intro_timer_panel = FindChild("IntroTimerPanel")?.gameObject;
            if (_intro_timer_text == null)
                _intro_timer_text = FindChildComponent<TextMeshProUGUI>("IntroTimerText");
            if (_player_team_text == null)
                _player_team_text = FindChildComponent<TextMeshProUGUI>("PlayerTeamText") ??
                    FindChildComponent<TextMeshProUGUI>("YourTeamText");
            if (_red_spawn_flag == null)
                _red_spawn_flag = FindChildComponent<Image>("RedSpawnFlag");
            if (_blue_spawn_flag == null)
                _blue_spawn_flag = FindChildComponent<Image>("BlueSpawnFlag");

            SetSpawnFlagsVisible(false);

            if (_player_team_text != null)
                _player_team_text.richText = true;
        }

        private Transform FindChild(string child_name)
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];
                if (child != null && child.name == child_name)
                    return child;
            }

            return null;
        }

        private T FindChildComponent<T>(string child_name)
            where T : Component
        {
            Transform child = FindChild(child_name);
            return child != null && child.TryGetComponent(out T component) ? component : null;
        }

        private void SetMouseSensitivity(float value)
        {
            ClientGameplaySettings.SetMouseSensitivity(value);
            UpdateValueTexts();
        }

        private void SetMasterVolume(float value)
        {
            ClientGameplaySettings.SetMasterVolume(value);
            ApplyMasterVolume(value);
            UpdateValueTexts();
        }

        private void ApplyMasterVolume(float value)
        {
            if (_audio_mixer == null || string.IsNullOrWhiteSpace(_master_volume_parameter))
                return;

            float decibels = Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20f;
            _audio_mixer.SetFloat(_master_volume_parameter, decibels);
        }

        private void UpdateValueTexts()
        {
            if (_mouse_sensitivity_value_text != null)
                _mouse_sensitivity_value_text.text = $"{ClientGameplaySettings.MouseSensitivity:0.00}x";

            if (_master_volume_value_text != null)
                _master_volume_value_text.text = $"{Mathf.RoundToInt(ClientGameplaySettings.MasterVolume * 100f)}%";
        }

        private void InitializeMapCamera()
        {
            EnsureMapTexture();

            if (_map_camera == null)
            {
                Debug.LogError($"{nameof(ClientMatchPauseLayer)} map camera is not assigned.", this);
                return;
            }

            _map_camera.enabled = false;
            _map_camera.orthographic = false;
            _map_camera.fieldOfView = _map_camera_fov;
            _map_camera.clearFlags = CameraClearFlags.SolidColor;
            _map_camera.backgroundColor = new Color(0.04f, 0.045f, 0.05f, 1f);
            _map_camera.cullingMask = CreateMapCullingMask();
            _map_camera.targetTexture = _map_texture;
            _last_map_rect_size = Vector2.zero;
        }

        private void UpdateIntroTimer()
        {
            TeamControlModeController mode = ClientAppRoot.Instance.MatchStore.ModeController;
            bool show = mode != null && mode.Phase == RoundPhase.Intro;
            if (_intro_timer_panel != null)
                _intro_timer_panel.SetActive(show);

            if (!show || _intro_timer_text == null)
                return;

            _intro_timer_text.text = FormatTime(mode.PhaseTimeRemaining);
        }

        private void UpdatePlayerTeamText()
        {
            if (_player_team_text == null)
                return;

            TeamId team_id = GetLocalTeam();
            string team_name = FormatTeamName(team_id);
            string color = ColorUtility.ToHtmlStringRGB(GetTeamColor(team_id));
            _player_team_text.text = $"Ваша команда: <color=#{color}>{team_name}</color>";
        }

        private void SetSpawnFlagsVisible(bool visible)
        {
            if (_red_spawn_flag != null)
                _red_spawn_flag.gameObject.SetActive(visible);
            if (_blue_spawn_flag != null)
                _blue_spawn_flag.gameObject.SetActive(visible);
        }

        private void UpdateSpawnFlags()
        {
            LevelController level = FindFirstObjectByType<LevelController>();
            if (level == null || _map_camera == null || _map_image == null)
                return;

            UpdateSpawnFlag(_red_spawn_flag, GetAveragePosition(level.RedSpawnPoints));
            UpdateSpawnFlag(_blue_spawn_flag, GetAveragePosition(level.BlueSpawnPoints));
        }

        private void UpdateSpawnFlag(Image flag, Vector3? world_position)
        {
            if (flag == null)
                return;

            if (!world_position.HasValue)
            {
                flag.gameObject.SetActive(false);
                return;
            }

            UpdateMapMarker(flag.rectTransform, world_position.Value);
        }

        private void UpdateMapMarker(RectTransform marker, Vector3 world_position)
        {
            if (marker == null)
                return;

            Vector3 viewport = _map_camera.WorldToViewportPoint(world_position);
            bool visible = viewport.z > 0f &&
                viewport.x >= 0f && viewport.x <= 1f &&
                viewport.y >= 0f && viewport.y <= 1f;
            marker.gameObject.SetActive(visible);
            if (!visible)
                return;

            RectTransform map_rect = _map_image.rectTransform;
            Rect rect = map_rect.rect;
            marker.anchoredPosition = new Vector2(
                (viewport.x - 0.5f) * rect.width,
                (viewport.y - 0.5f) * rect.height);
        }

        private static Vector3? GetAveragePosition(IReadOnlyList<Transform> points)
        {
            if (points == null || points.Count == 0)
                return null;

            Vector3 sum = Vector3.zero;
            int count = 0;
            for (int i = 0; i < points.Count; i++)
            {
                Transform point = points[i];
                if (point == null)
                    continue;

                sum += point.position;
                count++;
            }

            return count == 0 ? null : sum / count;
        }

        private static TeamId GetLocalTeam()
        {
            PlayerMatchIdentity[] identities = FindObjectsByType<PlayerMatchIdentity>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            for (int i = 0; i < identities.Length; i++)
            {
                PlayerMatchIdentity identity = identities[i];
                if (identity != null && identity.isOwned)
                    return identity.TeamId;
            }

            SpectatorPawn[] spectators = FindObjectsByType<SpectatorPawn>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            for (int i = 0; i < spectators.Length; i++)
            {
                SpectatorPawn spectator = spectators[i];
                if (spectator != null && spectator.isOwned)
                    return spectator.TeamId;
            }

            return TeamId.None;
        }

        private static string FormatTeamName(TeamId team_id)
        {
            return team_id switch
            {
                TeamId.Red => "Red Team",
                TeamId.Blue => "Blue Team",
                TeamId.Spectator => "Spectators",
                _ => "None"
            };
        }

        private static Color GetTeamColor(TeamId team_id)
        {
            return team_id switch
            {
                TeamId.Red => new Color(0.95f, 0.18f, 0.14f, 1f),
                TeamId.Blue => new Color(0.16f, 0.45f, 1f, 1f),
                TeamId.Spectator => new Color(1f, 0.82f, 0.18f, 1f),
                _ => Color.gray
            };
        }

        private void FrameMapCamera()
        {
            if (_map_camera == null)
                return;

            EnsureMapTexture();
            _last_map_rect_size = GetMapRectSize();

            if (!TryGetMapBounds(out Bounds bounds))
            {
                _map_camera.transform.SetPositionAndRotation(
                    new Vector3(0f, 90f, -90f),
                    Quaternion.Euler(_map_camera_pitch_degrees, _map_rotation_degrees, 0f));
                _map_camera.fieldOfView = _map_camera_fov;
                return;
            }

            float aspect = GetMapRectAspect();
            Vector3 center = bounds.center;
            center.y = bounds.center.y + bounds.extents.y * 0.25f;
            float vertical_fov = Mathf.Clamp(_map_camera_fov, 10f, 90f) * Mathf.Deg2Rad;
            float frame_size = Mathf.Max(bounds.size.z, bounds.size.x / Mathf.Max(0.1f, aspect));
            float distance = frame_size * 0.5f / Mathf.Tan(vertical_fov * 0.5f);
            distance *= Mathf.Max(0.1f, _map_padding * _map_camera_distance_multiplier);
            Quaternion rotation = Quaternion.Euler(_map_camera_pitch_degrees, _map_rotation_degrees, 0f);

            _map_camera.transform.SetPositionAndRotation(
                center - rotation * Vector3.forward * distance,
                rotation);
            _map_camera.orthographic = false;
            _map_camera.aspect = aspect;
            _map_camera.fieldOfView = _map_camera_fov;
            _map_camera.nearClipPlane = 0.1f;
            _map_camera.farClipPlane = distance + bounds.size.magnitude + 50f;
        }

        private void RefreshMapFrameIfNeeded()
        {
            if (EnsureMapTexture())
            {
                FrameMapCamera();
                return;
            }

            Vector2 map_rect_size = GetMapRectSize();
            if (Mathf.Abs(map_rect_size.x - _last_map_rect_size.x) < 0.5f &&
                Mathf.Abs(map_rect_size.y - _last_map_rect_size.y) < 0.5f)
            {
                return;
            }

            FrameMapCamera();
        }

        private float GetMapRectAspect()
        {
            Vector2 size = GetMapRectSize();
            if (size.x > 1f && size.y > 1f)
                return size.x / size.y;

            return _map_texture != null && _map_texture.height > 0
                ? (float)_map_texture.width / _map_texture.height
                : 1.5f;
        }

        private Vector2 GetMapRectSize()
        {
            if (_map_image == null)
                return Vector2.zero;

            Rect rect = _map_image.rectTransform.rect;
            return new Vector2(Mathf.Abs(rect.width), Mathf.Abs(rect.height));
        }

        private bool EnsureMapTexture()
        {
            Vector2 size = GetMapRectSize();
            Canvas canvas = _map_image != null ? _map_image.canvas : null;
            float canvas_scale = canvas != null ? canvas.scaleFactor : 1f;
            int max_size = Mathf.Max(256, _map_texture_max_size);
            int width = Mathf.Clamp(
                Mathf.CeilToInt(Mathf.Max(768f, size.x * canvas_scale * _map_texture_resolution_scale)),
                256,
                max_size);
            int height = Mathf.Clamp(
                Mathf.CeilToInt(Mathf.Max(512f, size.y * canvas_scale * _map_texture_resolution_scale)),
                256,
                max_size);

            if (_map_texture != null &&
                _map_texture.width == width &&
                _map_texture.height == height)
            {
                return false;
            }

            if (_map_texture != null)
            {
                _map_texture.Release();
                Destroy(_map_texture);
            }

            _map_texture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
            {
                name = "ClientPauseMap",
                antiAliasing = 2
            };
            _map_texture.Create();

            if (_map_image != null)
                _map_image.texture = _map_texture;
            if (_map_camera != null)
                _map_camera.targetTexture = _map_texture;

            return true;
        }

        private bool TryGetMapBounds(out Bounds bounds)
        {
            bounds = default;
            Scene active_scene = SceneManager.GetActiveScene();
            Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            int culling_mask = CreateMapCullingMask();
            bool has_bounds = false;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null ||
                    renderer.gameObject.scene != active_scene ||
                    !renderer.enabled ||
                    (culling_mask & (1 << renderer.gameObject.layer)) == 0 ||
                    !IsMapRenderer(renderer))
                {
                    continue;
                }

                if (!has_bounds)
                {
                    bounds = renderer.bounds;
                    has_bounds = true;
                    continue;
                }

                bounds.Encapsulate(renderer.bounds);
            }

            if (has_bounds)
                return true;

            return TryGetLevelBounds(out bounds);
        }

        private static bool IsMapRenderer(Renderer renderer)
        {
            return renderer.GetComponentInParent<PlayerCharacter>() == null &&
                renderer.GetComponentInParent<SpectatorPawn>() == null &&
                renderer.GetComponentInParent<Camera>() == null;
        }

        private static bool TryGetLevelBounds(out Bounds bounds)
        {
            bounds = default;
            LevelController level = FindFirstObjectByType<LevelController>();
            if (level == null)
                return false;

            bool has_bounds = false;
            EncapsulatePoints(ref bounds, ref has_bounds, level.RedSpawnPoints);
            EncapsulatePoints(ref bounds, ref has_bounds, level.BlueSpawnPoints);

            Transform capture = level.CapturePoint;
            if (capture != null)
                EncapsulatePoint(ref bounds, ref has_bounds, capture.position);

            if (!has_bounds)
                return false;

            Vector3 size = bounds.size;
            size.x = Mathf.Max(size.x, 1f);
            size.z = Mathf.Max(size.z, 1f);
            bounds.size = size;
            return true;
        }

        private static void EncapsulatePoints(
            ref Bounds bounds,
            ref bool has_bounds,
            IReadOnlyList<Transform> points)
        {
            if (points == null)
                return;

            for (int i = 0; i < points.Count; i++)
            {
                Transform point = points[i];
                if (point != null)
                    EncapsulatePoint(ref bounds, ref has_bounds, point.position);
            }
        }

        private static void EncapsulatePoint(ref Bounds bounds, ref bool has_bounds, Vector3 point)
        {
            if (!has_bounds)
            {
                bounds = new Bounds(point, Vector3.zero);
                has_bounds = true;
                return;
            }

            bounds.Encapsulate(point);
        }

        private static int CreateMapCullingMask()
        {
            int mask = ~0;
            ClearLayer(ref mask, "UI");
            ClearLayer(ref mask, "Ignore Raycast");
            ClearLayer(ref mask, "CharacterControllers");
            ClearLayer(ref mask, "CharacterFirstPerson");
            ClearLayer(ref mask, "CharacterExternal");
            ClearLayer(ref mask, "CharacterPhysics");
            ClearLayer(ref mask, "CharacterRagdoll");
            ClearLayer(ref mask, "CharacterNonColliding");
            ClearLayer(ref mask, "WieldablesFirstPerson");
            ClearLayer(ref mask, "WieldablesExternal");
            ClearLayer(ref mask, "Effects");
            return mask;
        }

        private static void ClearLayer(ref int mask, string layer_name)
        {
            int layer = LayerMask.NameToLayer(layer_name);
            if (layer >= 0)
                mask &= ~(1 << layer);
        }

        private static string FormatTime(float seconds)
        {
            int whole_seconds = Mathf.CeilToInt(seconds);
            int minutes = whole_seconds / 60;
            int seconds_part = whole_seconds % 60;
            return $"{minutes:00}:{seconds_part:00}";
        }

        private ClientUiLayer GetOrAddLayer()
        {
            ClientUiLayer layer = gameObject.GetComponent<ClientUiLayer>();
            return layer != null ? layer : gameObject.AddComponent<ClientUiLayer>();
        }
    }
}
