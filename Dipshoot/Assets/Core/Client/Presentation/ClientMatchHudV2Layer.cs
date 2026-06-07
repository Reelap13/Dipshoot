using System;
using Game.Level;
using Game.MatchMode;
using Game.Players;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Core.ClientPresentation
{
    public class ClientMatchHudV2Layer : MonoBehaviour
    {
        private const float HitMarkerDuration = 0.42f;
        private const float HitMarkerFadeInDuration = 0.055f;
        private const float HitMarkerHoldDuration = 0.12f;
        private const float HitMarkerStartOffset = 8f;
        private const float HitMarkerEndOffset = 22f;

        private static ClientMatchHudV2Layer _active_instance;

        [SerializeField] private Color _red_color = new(0.95f, 0.18f, 0.14f, 1f);
        [SerializeField] private Color _blue_color = new(0.16f, 0.45f, 1f, 1f);
        [SerializeField] private Color _neutral_color = new(0.7f, 0.7f, 0.7f, 1f);
        [SerializeField] private Color _contested_color = new(1f, 0.78f, 0.18f, 1f);

        [SerializeField] private Image _blue_score_background;
        [SerializeField] private Image _red_score_background;
        [SerializeField] private TextMeshProUGUI _blue_score_text;
        [SerializeField] private TextMeshProUGUI _red_score_text;
        [SerializeField] private TextMeshProUGUI _timer_text;
        [SerializeField] private Image _capture_progress_fill;

        [SerializeField] private GameObject _weapon_panel;
        [SerializeField] private TextMeshProUGUI _weapon_name_text;
        [SerializeField] private TextMeshProUGUI _weapon_ammo_text;
        [SerializeField] private TextMeshProUGUI _weapon_reserve_text;
        [SerializeField] private TextMeshProUGUI _weapon_reload_text;
        [SerializeField] private Image _weapon_reload_progress_fill;
        [SerializeField] private TextMeshProUGUI _health_text;

        [SerializeField] private GameObject _crosshair;
        [SerializeField] private Color _crosshair_color = new(1f, 1f, 1f, 0.86f);
        [SerializeField] private float _base_crosshair_gap = 9f;
        [SerializeField] private float _spread_crosshair_gap_scale = 5f;
        [SerializeField] private float _max_crosshair_gap = 42f;
        [SerializeField] private float _crosshair_lerp_speed = 18f;
        [SerializeField] private GameObject _hit_marker;
        [SerializeField] private Color _hit_marker_color = new(1f, 0.96f, 0.72f, 1f);
        [SerializeField] private Image _damage_overlay;
        [SerializeField] private Image[] _damage_edge_images = Array.Empty<Image>();
        [SerializeField] private Color _damage_overlay_color = new(0.95f, 0.05f, 0.02f, 0.38f);
        [SerializeField] private float _damage_overlay_duration = 0.45f;

        private TeamControlModeController _mode_controller;
        private WeaponController _local_weapon_controller;
        private ClientUiLayer _layer;
        private Image[] _hit_marker_lines = Array.Empty<Image>();
        private RectTransform[] _hit_marker_line_rects = Array.Empty<RectTransform>();
        private RectTransform _crosshair_top;
        private RectTransform _crosshair_bottom;
        private RectTransform _crosshair_left;
        private RectTransform _crosshair_right;
        private float _current_crosshair_gap;
        private float _hit_marker_started_at = -1f;
        private Color _active_hit_marker_color;
        private int _last_health_current = -1;
        private float _damage_overlay_started_at = -1f;
        private Renderer[] _capture_marker_renderers = Array.Empty<Renderer>();
        private TeamId _last_capture_marker_owner = TeamId.None;
        private bool _last_capture_marker_contested;

        public static void PlayLocalHitMarker(bool is_kill = false)
        {
            if (_active_instance != null)
                _active_instance.PlayHitMarker(is_kill);
        }

        private void Awake()
        {
            _layer = GetOrAddLayer();
            _layer.Initialize(ClientUiLayerKind.MatchHud);
            CachePrefabReferences();
            CacheCrosshairLines();
            CacheHitMarkerLines();
            if (_hit_marker != null)
                _hit_marker.SetActive(false);
            if (_damage_overlay != null)
                _damage_overlay.gameObject.SetActive(false);
            SetDamageEdgesActive(false);
        }

        private void OnEnable()
        {
            _active_instance = this;
        }

        private void OnDisable()
        {
            if (_active_instance == this)
                _active_instance = null;
        }

        private void Update()
        {
            UpdateCrosshairSpread();
            UpdateHitMarker();
            UpdateDamageOverlay();
            UpdateWeaponPanel();
            UpdateHealth();

            _mode_controller = ClientAppRoot.Instance.MatchStore.ModeController;
            if (_mode_controller == null)
                return;

            UpdateScorePanel();
            UpdateCaptureProgress();
            UpdateCapturePointMarker();
        }

        private void UpdateWeaponPanel()
        {
            if (_weapon_panel == null)
                return;

            WeaponController weapon_controller = ResolveLocalWeaponController();
            bool has_weapon_controller = weapon_controller != null;
            _weapon_panel.SetActive(has_weapon_controller);
            if (!has_weapon_controller)
                return;

            if (_weapon_name_text != null)
                _weapon_name_text.text = weapon_controller.ActiveWeaponDisplayName;
            if (_weapon_ammo_text != null)
                _weapon_ammo_text.text = weapon_controller.ActiveAmmo.ToString();
            if (_weapon_reserve_text != null)
                _weapon_reserve_text.text = $"/ {weapon_controller.ActiveReserveAmmo}";

            bool is_reloading = weapon_controller.IsActiveReloading;
            if (_weapon_reload_text != null)
                _weapon_reload_text.text = is_reloading ? "Reloading" : string.Empty;
            if (_weapon_reload_progress_fill != null)
                _weapon_reload_progress_fill.rectTransform.anchorMax =
                    new Vector2(is_reloading ? weapon_controller.ActiveReloadProgress : 0f, 1f);
        }

        private void UpdateHealth()
        {
            if (_health_text == null)
                return;

            WeaponController weapon_controller = ResolveLocalWeaponController();
            PlayerHealth health = weapon_controller == null
                ? null
                : weapon_controller.GetComponent<PlayerHealth>();

            _health_text.text = health == null
                ? string.Empty
                : $"HP {health.CurrentHealth}/{health.MaxHealth}";

            if (health == null)
            {
                _last_health_current = -1;
                return;
            }

            if (_last_health_current >= 0 && health.CurrentHealth < _last_health_current)
                PlayDamageOverlay();

            _last_health_current = health.CurrentHealth;
        }

        private void UpdateScorePanel()
        {
            if (_timer_text != null)
                _timer_text.text = FormatTime(_mode_controller.PhaseTimeRemaining);
            if (_blue_score_text != null)
                _blue_score_text.text = _mode_controller.BlueScore.ToString();
            if (_red_score_text != null)
                _red_score_text.text = _mode_controller.RedScore.ToString();

            ApplyTeamScoreStyle(_blue_score_background, _blue_score_text, TeamId.Blue);
            ApplyTeamScoreStyle(_red_score_background, _red_score_text, TeamId.Red);
        }

        private void ApplyTeamScoreStyle(Image background, TextMeshProUGUI text, TeamId team)
        {
            bool owns_point = _mode_controller.CaptureOwner == team;
            Color team_color = GetTeamColor(team);

            if (background != null)
                background.color = owns_point ? team_color : new Color(0f, 0f, 0f, 0.58f);
            if (text != null)
                text.color = owns_point ? Color.white : team_color;
        }

        private void UpdateCaptureProgress()
        {
            if (_capture_progress_fill == null)
                return;

            Color point_color = _mode_controller.IsCaptureContested
                ? _contested_color
                : GetTeamColor(GetPointDisplayTeam());

            _capture_progress_fill.color = point_color;
            _capture_progress_fill.rectTransform.anchorMax =
                new Vector2(Mathf.Clamp01(_mode_controller.CaptureProgress), 1f);
        }

        private TeamId GetPointDisplayTeam()
        {
            return _mode_controller.CapturingTeam != TeamId.None
                ? _mode_controller.CapturingTeam
                : _mode_controller.CaptureOwner;
        }

        private void UpdateCapturePointMarker()
        {
            if (_mode_controller.CaptureOwner == _last_capture_marker_owner &&
                _mode_controller.IsCaptureContested == _last_capture_marker_contested &&
                _capture_marker_renderers.Length > 0)
                return;

            _last_capture_marker_owner = _mode_controller.CaptureOwner;
            _last_capture_marker_contested = _mode_controller.IsCaptureContested;

            if (_capture_marker_renderers.Length == 0)
                CacheCaptureMarkerRenderers();

            Color color = _mode_controller.IsCaptureContested
                ? _contested_color
                : GetTeamColor(_mode_controller.CaptureOwner);

            MaterialPropertyBlock block = new();
            for (int i = 0; i < _capture_marker_renderers.Length; i++)
            {
                Renderer renderer = _capture_marker_renderers[i];
                if (renderer == null)
                    continue;

                renderer.GetPropertyBlock(block);
                block.SetColor("_BaseColor", color);
                block.SetColor("_Color", color);
                renderer.SetPropertyBlock(block);
            }
        }

        private void CacheCaptureMarkerRenderers()
        {
            LevelController level = FindFirstObjectByType<LevelController>();
            Transform capture_point = level == null ? null : level.CapturePoint;
            _capture_marker_renderers = capture_point == null
                ? Array.Empty<Renderer>()
                : capture_point.GetComponentsInChildren<Renderer>(true);
        }

        private void UpdateCrosshairSpread()
        {
            if (_crosshair == null)
                return;

            WeaponController weapon_controller = ResolveLocalWeaponController();
            float target_gap = _base_crosshair_gap;
            if (weapon_controller != null)
                target_gap += weapon_controller.CurrentEffectiveSpreadDegrees * _spread_crosshair_gap_scale;

            target_gap = Mathf.Clamp(target_gap, _base_crosshair_gap, _max_crosshair_gap);
            float lerp = 1f - Mathf.Exp(-Mathf.Max(0f, _crosshair_lerp_speed) * Time.unscaledDeltaTime);
            _current_crosshair_gap = _current_crosshair_gap <= 0f
                ? target_gap
                : Mathf.Lerp(_current_crosshair_gap, target_gap, lerp);

            ApplyCrosshairGap(_current_crosshair_gap);
        }

        private void ApplyCrosshairGap(float gap)
        {
            if (_crosshair_top != null)
                _crosshair_top.anchoredPosition = new Vector2(0f, gap);
            if (_crosshair_bottom != null)
                _crosshair_bottom.anchoredPosition = new Vector2(0f, -gap);
            if (_crosshair_left != null)
                _crosshair_left.anchoredPosition = new Vector2(-gap, 0f);
            if (_crosshair_right != null)
                _crosshair_right.anchoredPosition = new Vector2(gap, 0f);
        }

        private void PlayHitMarker(bool is_kill)
        {
            if (_hit_marker == null)
                return;

            _active_hit_marker_color = is_kill ? Color.red : _hit_marker_color;
            _hit_marker_started_at = Time.unscaledTime;
            _hit_marker.SetActive(true);
        }

        private void UpdateHitMarker()
        {
            if (_hit_marker == null || _hit_marker_started_at < 0f)
                return;

            float elapsed = Time.unscaledTime - _hit_marker_started_at;
            if (elapsed >= HitMarkerDuration)
            {
                _hit_marker_started_at = -1f;
                _hit_marker.SetActive(false);
                return;
            }

            float alpha = GetHitMarkerAlpha(elapsed);
            float offset = Mathf.Lerp(
                HitMarkerStartOffset,
                HitMarkerEndOffset,
                Mathf.SmoothStep(0f, 1f, elapsed / HitMarkerDuration));

            ApplyHitMarkerLine(0, new Vector2(1f, 1f), offset, alpha);
            ApplyHitMarkerLine(1, new Vector2(-1f, 1f), offset, alpha);
            ApplyHitMarkerLine(2, new Vector2(-1f, -1f), offset, alpha);
            ApplyHitMarkerLine(3, new Vector2(1f, -1f), offset, alpha);
        }

        private float GetHitMarkerAlpha(float elapsed)
        {
            if (elapsed < HitMarkerFadeInDuration)
                return Mathf.Lerp(0f, 1f, elapsed / HitMarkerFadeInDuration);
            if (elapsed < HitMarkerFadeInDuration + HitMarkerHoldDuration)
                return 1f;

            return Mathf.Lerp(
                1f,
                0f,
                (elapsed - HitMarkerFadeInDuration - HitMarkerHoldDuration) /
                Mathf.Max(0.001f, HitMarkerDuration - HitMarkerFadeInDuration - HitMarkerHoldDuration));
        }

        private void ApplyHitMarkerLine(int index, Vector2 direction, float offset, float alpha)
        {
            if (index < 0 || index >= _hit_marker_lines.Length || index >= _hit_marker_line_rects.Length)
                return;

            Image image = _hit_marker_lines[index];
            RectTransform rect_transform = _hit_marker_line_rects[index];
            if (image == null || rect_transform == null)
                return;

            rect_transform.anchoredPosition = direction.normalized * offset;
            image.color = new Color(
                _active_hit_marker_color.r,
                _active_hit_marker_color.g,
                _active_hit_marker_color.b,
                _active_hit_marker_color.a * alpha);
        }

        private void PlayDamageOverlay()
        {
            if (_damage_overlay == null && (_damage_edge_images == null || _damage_edge_images.Length == 0))
                return;

            _damage_overlay_started_at = Time.unscaledTime;
            ApplyDamageOverlayColor(_damage_overlay_color);
            SetDamageEdgesActive(true);
        }

        private void UpdateDamageOverlay()
        {
            if (_damage_overlay == null && (_damage_edge_images == null || _damage_edge_images.Length == 0) ||
                _damage_overlay_started_at < 0f)
                return;

            float elapsed = Time.unscaledTime - _damage_overlay_started_at;
            if (elapsed >= _damage_overlay_duration)
            {
                _damage_overlay_started_at = -1f;
                SetDamageEdgesActive(false);
                return;
            }

            float alpha = Mathf.Lerp(
                _damage_overlay_color.a,
                0f,
                elapsed / Mathf.Max(0.001f, _damage_overlay_duration));
            ApplyDamageOverlayColor(new Color(
                _damage_overlay_color.r,
                _damage_overlay_color.g,
                _damage_overlay_color.b,
                alpha));
        }

        private void ApplyDamageOverlayColor(Color color)
        {
            if (_damage_edge_images != null && _damage_edge_images.Length > 0)
            {
                for (int i = 0; i < _damage_edge_images.Length; i++)
                    if (_damage_edge_images[i] != null)
                        _damage_edge_images[i].color = color;
                return;
            }

            if (_damage_overlay != null)
                _damage_overlay.color = color;
        }

        private void SetDamageEdgesActive(bool active)
        {
            if (_damage_edge_images != null && _damage_edge_images.Length > 0)
            {
                for (int i = 0; i < _damage_edge_images.Length; i++)
                    if (_damage_edge_images[i] != null)
                        _damage_edge_images[i].gameObject.SetActive(active);
                return;
            }

            if (_damage_overlay != null)
                _damage_overlay.gameObject.SetActive(active);
        }

        private WeaponController ResolveLocalWeaponController()
        {
            if (_local_weapon_controller != null &&
                _local_weapon_controller.isActiveAndEnabled &&
                _local_weapon_controller.isOwned)
                return _local_weapon_controller;

            _local_weapon_controller = null;
            WeaponController[] weapon_controllers = FindObjectsByType<WeaponController>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            for (int i = 0; i < weapon_controllers.Length; i++)
            {
                WeaponController weapon_controller = weapon_controllers[i];
                if (weapon_controller == null || !weapon_controller.isOwned)
                    continue;

                _local_weapon_controller = weapon_controller;
                return _local_weapon_controller;
            }

            return null;
        }

        private void CachePrefabReferences()
        {
            _blue_score_background ??= FindChildComponent<Image>("BlueScoreBackground");
            _red_score_background ??= FindChildComponent<Image>("RedScoreBackground");
            _blue_score_text ??= FindChildComponent<TextMeshProUGUI>("BlueScoreText");
            _red_score_text ??= FindChildComponent<TextMeshProUGUI>("RedScoreText");
            _timer_text ??= FindChildComponent<TextMeshProUGUI>("TimerText");
            _capture_progress_fill ??= FindChildComponent<Image>("CaptureProgressFill");
            _health_text ??= FindChildComponent<TextMeshProUGUI>("HealthText");
            _weapon_panel ??= FindChild("WeaponPanel")?.gameObject;
            _weapon_name_text ??= FindChildComponent<TextMeshProUGUI>("WeaponNameText");
            _weapon_ammo_text ??= FindChildComponent<TextMeshProUGUI>("WeaponAmmoText");
            _weapon_reserve_text ??= FindChildComponent<TextMeshProUGUI>("WeaponReserveText");
            _weapon_reload_text ??= FindChildComponent<TextMeshProUGUI>("WeaponReloadText");
            _weapon_reload_progress_fill ??= FindChildComponent<Image>("WeaponReloadProgressFill");
            _crosshair ??= FindChild("Crosshair")?.gameObject;
            _hit_marker ??= FindChild("HitMarker")?.gameObject;
            _damage_overlay ??= FindChildComponent<Image>("DamageOverlay");
            if (_damage_edge_images == null || _damage_edge_images.Length == 0)
                _damage_edge_images = FindDamageEdges();
        }

        private Image[] FindDamageEdges()
        {
            Transform container = FindChild("DamageEdges");
            return container == null ? Array.Empty<Image>() : container.GetComponentsInChildren<Image>(true);
        }

        private void CacheCrosshairLines()
        {
            _crosshair_top = FindCrosshairLine("Top");
            _crosshair_bottom = FindCrosshairLine("Bottom");
            _crosshair_left = FindCrosshairLine("Left");
            _crosshair_right = FindCrosshairLine("Right");
            if (_current_crosshair_gap <= 0f)
                _current_crosshair_gap = _base_crosshair_gap;
        }

        private RectTransform FindCrosshairLine(string name)
        {
            Transform line = _crosshair == null ? null : _crosshair.transform.Find(name);
            return line == null ? null : line.GetComponent<RectTransform>();
        }

        private void CacheHitMarkerLines()
        {
            _hit_marker_lines = _hit_marker == null
                ? Array.Empty<Image>()
                : _hit_marker.GetComponentsInChildren<Image>(true);
            _hit_marker_line_rects = new RectTransform[_hit_marker_lines.Length];

            for (int i = 0; i < _hit_marker_lines.Length; i++)
                _hit_marker_line_rects[i] = _hit_marker_lines[i] == null
                    ? null
                    : _hit_marker_lines[i].rectTransform;
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

        private string FormatTime(float seconds)
        {
            int whole_seconds = Mathf.CeilToInt(seconds);
            int minutes = whole_seconds / 60;
            int seconds_part = whole_seconds % 60;
            return $"{minutes:00}:{seconds_part:00}";
        }

        private Color GetTeamColor(TeamId team_id)
        {
            return team_id switch
            {
                TeamId.Red => _red_color,
                TeamId.Blue => _blue_color,
                _ => _neutral_color,
            };
        }

        private ClientUiLayer GetOrAddLayer()
        {
            ClientUiLayer layer = gameObject.GetComponent<ClientUiLayer>();
            return layer != null ? layer : gameObject.AddComponent<ClientUiLayer>();
        }

#if UNITY_EDITOR
        public void ConfigurePrefabReferences(
            Image blue_score_background,
            Image red_score_background,
            TextMeshProUGUI blue_score_text,
            TextMeshProUGUI red_score_text,
            TextMeshProUGUI timer_text,
            Image capture_progress_fill,
            GameObject weapon_panel,
            TextMeshProUGUI weapon_name_text,
            TextMeshProUGUI weapon_ammo_text,
            TextMeshProUGUI weapon_reserve_text,
            TextMeshProUGUI weapon_reload_text,
            Image weapon_reload_progress_fill,
            TextMeshProUGUI health_text,
            GameObject crosshair,
            GameObject hit_marker,
            Image damage_overlay,
            Image[] damage_edge_images = null)
        {
            _blue_score_background = blue_score_background;
            _red_score_background = red_score_background;
            _blue_score_text = blue_score_text;
            _red_score_text = red_score_text;
            _timer_text = timer_text;
            _capture_progress_fill = capture_progress_fill;
            _weapon_panel = weapon_panel;
            _weapon_name_text = weapon_name_text;
            _weapon_ammo_text = weapon_ammo_text;
            _weapon_reserve_text = weapon_reserve_text;
            _weapon_reload_text = weapon_reload_text;
            _weapon_reload_progress_fill = weapon_reload_progress_fill;
            _health_text = health_text;
            _crosshair = crosshair;
            _hit_marker = hit_marker;
            _damage_overlay = damage_overlay;
            _damage_edge_images = damage_edge_images ?? Array.Empty<Image>();
        }
#endif
    }
}
