using System;
using Game.MatchMode;
using Game.Players;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Core.ClientPresentation
{
    public class ClientMatchHudLayer : MonoBehaviour
    {
        private const float HitMarkerDuration = 0.42f;
        private const float HitMarkerFadeInDuration = 0.055f;
        private const float HitMarkerHoldDuration = 0.12f;
        private const float HitMarkerStartOffset = 8f;
        private const float HitMarkerEndOffset = 22f;

        private static ClientMatchHudLayer _active_instance;

        [SerializeField] private Color _red_color = new(0.95f, 0.18f, 0.14f, 1f);
        [SerializeField] private Color _blue_color = new(0.16f, 0.45f, 1f, 1f);
        [SerializeField] private Color _neutral_color = new(0.7f, 0.7f, 0.7f, 1f);
        [SerializeField] private Color _contested_color = new(1f, 0.78f, 0.18f, 1f);

        [SerializeField] private GameObject _phase_banner;
        [SerializeField] private GameObject _result_panel;
        [SerializeField] private TextMeshProUGUI _score_text;
        [SerializeField] private TextMeshProUGUI _round_text;
        [SerializeField] private TextMeshProUGUI _phase_text;
        [SerializeField] private TextMeshProUGUI _result_text;
        [SerializeField] private TextMeshProUGUI _point_text;
        [SerializeField] private TextMeshProUGUI _inside_text;
        [SerializeField] private Image _point_owner_strip;
        [SerializeField] private Image _point_progress_fill;
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

        public static void PlayLocalHitMarker(bool is_kill = false)
        {
            if (_active_instance != null)
                _active_instance.PlayHitMarker(is_kill);
        }

        private void Awake()
        {
            _layer = GetOrAddLayer();
            _layer.Initialize(ClientUiLayerKind.MatchHud);
            EnsureCrosshair();
            EnsureHitMarker();
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
            UpdateWeaponPanel();
            UpdateHealth();

            _mode_controller = ClientAppRoot.Instance.MatchStore.ModeController;
            if (_mode_controller == null || _score_text == null)
                return;

            UpdateScorePanel();
            UpdatePhaseBanner();
            UpdateResultPanel();
            UpdatePointPanel();
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

            if (_weapon_reload_progress_fill == null)
                return;

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
        }

        private void UpdateScorePanel()
        {
            _score_text.text =
                $"<color=#{ColorUtility.ToHtmlStringRGB(_red_color)}>RED</color> {_mode_controller.RedScore}  -  " +
                $"{_mode_controller.BlueScore} <color=#{ColorUtility.ToHtmlStringRGB(_blue_color)}>BLUE</color>";

            _round_text.text =
                $"Round {_mode_controller.CurrentRound}/{_mode_controller.RoundsCount}   " +
                $"Rounds: {_mode_controller.RedRoundWins} - {_mode_controller.BlueRoundWins}";
        }

        private void UpdatePhaseBanner()
        {
            bool show_banner = _mode_controller.Phase != RoundPhase.Playing;
            _phase_banner.SetActive(show_banner);

            if (!show_banner)
                return;

            _phase_text.text = GetPhaseText();
        }

        private void UpdateResultPanel()
        {
            bool show_results =
                _mode_controller.Phase == RoundPhase.Ending ||
                _mode_controller.Phase == RoundPhase.Finished;
            _result_panel.SetActive(show_results);

            if (!show_results)
                return;

            if (_mode_controller.Phase == RoundPhase.Finished)
            {
                _result_text.text = $"Match winner: {FormatTeam(_mode_controller.MatchWinner)}";
                return;
            }

            _result_text.text =
                $"Round winner: {FormatTeam(_mode_controller.LastRoundWinner)}\n" +
                $"Score: Red {_mode_controller.RedScore} - Blue {_mode_controller.BlueScore}";
        }

        private void UpdatePointPanel()
        {
            Color point_color = _mode_controller.IsCaptureContested
                ? _contested_color
                : GetTeamColor(GetPointDisplayTeam());

            _point_owner_strip.color = GetTeamColor(_mode_controller.CaptureOwner);
            _point_progress_fill.color = point_color;
            _point_progress_fill.rectTransform.anchorMax =
                new Vector2(Mathf.Clamp01(_mode_controller.CaptureProgress), 1f);

            string status = GetPointStatusText();
            _point_text.text =
                $"Point: {status}   Owner: {FormatTeam(_mode_controller.CaptureOwner)}";
            _inside_text.text =
                $"Inside: Red {_mode_controller.RedPlayersInside} / Blue {_mode_controller.BluePlayersInside}";
        }

        private string GetPhaseText()
        {
            return _mode_controller.Phase switch
            {
                RoundPhase.GeneratingMap => "Preparing map",
                RoundPhase.SpawningPlayers => "Spawning players",
                RoundPhase.Intro => $"Round starts in {FormatTime(_mode_controller.PhaseTimeRemaining)}",
                RoundPhase.Ending => $"Next round in {FormatTime(_mode_controller.PhaseTimeRemaining)}",
                RoundPhase.Finished => "Match finished",
                _ => _mode_controller.Phase.ToString(),
            };
        }

        private string GetPointStatusText()
        {
            if (_mode_controller.IsCaptureContested)
                return "Contested";

            if (_mode_controller.CapturingTeam != TeamId.None)
                return $"Capturing by {FormatTeam(_mode_controller.CapturingTeam)}";

            return _mode_controller.CaptureOwner == TeamId.None
                ? "Neutral"
                : $"Held by {FormatTeam(_mode_controller.CaptureOwner)}";
        }

        private TeamId GetPointDisplayTeam()
        {
            return _mode_controller.CapturingTeam != TeamId.None
                ? _mode_controller.CapturingTeam
                : _mode_controller.CaptureOwner;
        }

        private string FormatTeam(TeamId team_id)
        {
            return team_id switch
            {
                TeamId.Red => "Red",
                TeamId.Blue => "Blue",
                _ => "None",
            };
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

        private WeaponController ResolveLocalWeaponController()
        {
            if (_local_weapon_controller != null &&
                _local_weapon_controller.isActiveAndEnabled &&
                _local_weapon_controller.isOwned)
            {
                return _local_weapon_controller;
            }

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

        private ClientUiLayer GetOrAddLayer()
        {
            ClientUiLayer layer = gameObject.GetComponent<ClientUiLayer>();
            return layer != null ? layer : gameObject.AddComponent<ClientUiLayer>();
        }

        private void EnsureCrosshair()
        {
            if (_crosshair != null)
            {
                CacheCrosshairLines();
                return;
            }

            _crosshair = new GameObject("Crosshair", typeof(RectTransform));
            _crosshair.transform.SetParent(transform, false);

            RectTransform rect_transform = _crosshair.GetComponent<RectTransform>();
            rect_transform.anchorMin = new Vector2(0.5f, 0.5f);
            rect_transform.anchorMax = new Vector2(0.5f, 0.5f);
            rect_transform.pivot = new Vector2(0.5f, 0.5f);
            rect_transform.anchoredPosition = Vector2.zero;
            rect_transform.sizeDelta = new Vector2(40f, 40f);

            _crosshair_top = CreateCrosshairLine("Top", _crosshair.transform, new Vector2(0f, _base_crosshair_gap), new Vector2(2f, 8f));
            _crosshair_bottom = CreateCrosshairLine("Bottom", _crosshair.transform, new Vector2(0f, -_base_crosshair_gap), new Vector2(2f, 8f));
            _crosshair_left = CreateCrosshairLine("Left", _crosshair.transform, new Vector2(-_base_crosshair_gap, 0f), new Vector2(8f, 2f));
            _crosshair_right = CreateCrosshairLine("Right", _crosshair.transform, new Vector2(_base_crosshair_gap, 0f), new Vector2(8f, 2f));
            _current_crosshair_gap = _base_crosshair_gap;
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

        private RectTransform CreateCrosshairLine(
            string name,
            Transform parent,
            Vector2 anchored_position,
            Vector2 size_delta)
        {
            GameObject line = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            line.transform.SetParent(parent, false);

            RectTransform rect_transform = line.GetComponent<RectTransform>();
            rect_transform.anchorMin = new Vector2(0.5f, 0.5f);
            rect_transform.anchorMax = new Vector2(0.5f, 0.5f);
            rect_transform.pivot = new Vector2(0.5f, 0.5f);
            rect_transform.anchoredPosition = anchored_position;
            rect_transform.sizeDelta = size_delta;

            Image image = line.GetComponent<Image>();
            image.color = _crosshair_color;
            image.raycastTarget = false;

            return rect_transform;
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

        private void EnsureHitMarker()
        {
            if (_hit_marker == null)
            {
                _hit_marker = new GameObject("HitMarker", typeof(RectTransform));
                _hit_marker.transform.SetParent(transform, false);

                RectTransform rect_transform = _hit_marker.GetComponent<RectTransform>();
                rect_transform.anchorMin = new Vector2(0.5f, 0.5f);
                rect_transform.anchorMax = new Vector2(0.5f, 0.5f);
                rect_transform.pivot = new Vector2(0.5f, 0.5f);
                rect_transform.anchoredPosition = Vector2.zero;
                rect_transform.sizeDelta = new Vector2(96f, 96f);
            }

            if (_hit_marker.GetComponentsInChildren<Image>(true).Length < 4)
            {
                CreateHitMarkerLine("TopRight", _hit_marker.transform, -45f);
                CreateHitMarkerLine("TopLeft", _hit_marker.transform, 45f);
                CreateHitMarkerLine("BottomLeft", _hit_marker.transform, -45f);
                CreateHitMarkerLine("BottomRight", _hit_marker.transform, 45f);
            }

            CacheHitMarkerLines();
            _hit_marker.SetActive(false);
        }

        private void CreateHitMarkerLine(string name, Transform parent, float z_rotation)
        {
            GameObject line = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            line.transform.SetParent(parent, false);

            RectTransform rect_transform = line.GetComponent<RectTransform>();
            rect_transform.anchorMin = new Vector2(0.5f, 0.5f);
            rect_transform.anchorMax = new Vector2(0.5f, 0.5f);
            rect_transform.pivot = new Vector2(0.5f, 0.5f);
            rect_transform.anchoredPosition = Vector2.zero;
            rect_transform.sizeDelta = new Vector2(2f, 14f);
            rect_transform.localRotation = Quaternion.Euler(0f, 0f, z_rotation);

            Image image = line.GetComponent<Image>();
            image.color = new Color(_hit_marker_color.r, _hit_marker_color.g, _hit_marker_color.b, 0f);
            image.raycastTarget = false;
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

        private void PlayHitMarker(bool is_kill)
        {
            EnsureHitMarker();
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
    }
}
