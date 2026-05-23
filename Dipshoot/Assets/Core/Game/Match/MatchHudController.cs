using Mirror;
using UnityEngine;
using UnityEngine.UI;

namespace Game.MatchMode
{
    public class MatchHudController : NetworkBehaviour
    {
        [SerializeField] private TeamControlModeController _mode_controller;
        [SerializeField] private Color _red_color = new(0.95f, 0.18f, 0.14f, 1f);
        [SerializeField] private Color _blue_color = new(0.16f, 0.45f, 1f, 1f);
        [SerializeField] private Color _neutral_color = new(0.7f, 0.7f, 0.7f, 1f);
        [SerializeField] private Color _contested_color = new(1f, 0.78f, 0.18f, 1f);

        private GameObject _hud_root;
        private GameObject _phase_banner;
        private GameObject _result_panel;
        private Text _score_text;
        private Text _round_text;
        private Text _phase_text;
        private Text _result_text;
        private Text _point_text;
        private Text _inside_text;
        private Image _point_owner_strip;
        private Image _point_progress_fill;
        private Font _font;

        public override void OnStartClient()
        {
            base.OnStartClient();
            CacheReferences();
            CreateHud();
        }

        private void Update()
        {
            if (_mode_controller == null)
                CacheReferences();

            if (_mode_controller == null || _score_text == null)
                return;

            UpdateScorePanel();
            UpdatePhaseBanner();
            UpdateResultPanel();
            UpdatePointPanel();
        }

        private void OnDestroy()
        {
            if (_hud_root != null)
                Destroy(_hud_root);
        }

        private void CacheReferences()
        {
            if (_mode_controller == null)
                _mode_controller = GetComponent<TeamControlModeController>();
        }

        private void CreateHud()
        {
            if (_hud_root != null)
                return;

            _font = GetDefaultFont();
            _hud_root = new GameObject("MatchHud");

            Canvas canvas = _hud_root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;

            CanvasScaler scaler = _hud_root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            CreateScorePanel();
            CreatePhaseBanner();
            CreateResultPanel();
            CreatePointPanel();
        }

        private void CreateScorePanel()
        {
            GameObject panel = CreatePanel(
                "ScorePanel",
                _hud_root.transform,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -24f),
                new Vector2(620f, 92f),
                new Color(0f, 0f, 0f, 0.58f));

            _score_text = CreateText(
                "ScoreText",
                panel.transform,
                new Vector2(0f, 0.42f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                34,
                TextAnchor.MiddleCenter);

            _round_text = CreateText(
                "RoundText",
                panel.transform,
                new Vector2(0f, 0f),
                new Vector2(1f, 0.42f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                20,
                TextAnchor.MiddleCenter);
        }

        private void CreatePhaseBanner()
        {
            _phase_banner = CreatePanel(
                "PhaseBanner",
                _hud_root.transform,
                new Vector2(0.5f, 0.72f),
                new Vector2(0.5f, 0.72f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(640f, 84f),
                new Color(0f, 0f, 0f, 0.62f));

            _phase_text = CreateText(
                "PhaseText",
                _phase_banner.transform,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                30,
                TextAnchor.MiddleCenter);
        }

        private void CreateResultPanel()
        {
            _result_panel = CreatePanel(
                "ResultPanel",
                _hud_root.transform,
                new Vector2(0.5f, 0.58f),
                new Vector2(0.5f, 0.58f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(560f, 88f),
                new Color(0f, 0f, 0f, 0.5f));

            _result_text = CreateText(
                "ResultText",
                _result_panel.transform,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                22,
                TextAnchor.MiddleCenter);
        }

        private void CreatePointPanel()
        {
            GameObject panel = CreatePanel(
                "PointPanel",
                _hud_root.transform,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 34f),
                new Vector2(640f, 96f),
                new Color(0f, 0f, 0f, 0.58f));

            _point_owner_strip = CreateImage(
                "PointOwner",
                panel.transform,
                new Vector2(0f, 0f),
                new Vector2(0.018f, 1f),
                Vector2.zero,
                Vector2.zero,
                Vector2.zero,
                _neutral_color);

            _point_text = CreateText(
                "PointText",
                panel.transform,
                new Vector2(0.05f, 0.48f),
                new Vector2(0.95f, 1f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                22,
                TextAnchor.MiddleLeft);

            _inside_text = CreateText(
                "InsideText",
                panel.transform,
                new Vector2(0.05f, 0f),
                new Vector2(0.95f, 0.42f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                18,
                TextAnchor.MiddleLeft);

            GameObject progress_background = CreatePanel(
                "PointProgressBackground",
                panel.transform,
                new Vector2(0.05f, 0.42f),
                new Vector2(0.95f, 0.48f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                new Color(1f, 1f, 1f, 0.15f));

            _point_progress_fill = CreateImage(
                "PointProgressFill",
                progress_background.transform,
                Vector2.zero,
                new Vector2(0f, 1f),
                Vector2.zero,
                Vector2.zero,
                Vector2.zero,
                _neutral_color);
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

        private GameObject CreatePanel(
            string name,
            Transform parent,
            Vector2 anchor_min,
            Vector2 anchor_max,
            Vector2 pivot,
            Vector2 anchored_position,
            Vector2 size_delta,
            Color color)
        {
            GameObject target = new(name);
            target.transform.SetParent(parent, false);
            RectTransform rect_transform = target.AddComponent<RectTransform>();
            rect_transform.anchorMin = anchor_min;
            rect_transform.anchorMax = anchor_max;
            rect_transform.pivot = pivot;
            rect_transform.anchoredPosition = anchored_position;
            rect_transform.sizeDelta = size_delta;

            Image image = target.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return target;
        }

        private Image CreateImage(
            string name,
            Transform parent,
            Vector2 anchor_min,
            Vector2 anchor_max,
            Vector2 pivot,
            Vector2 anchored_position,
            Vector2 size_delta,
            Color color)
        {
            GameObject target = CreatePanel(
                name,
                parent,
                anchor_min,
                anchor_max,
                pivot,
                anchored_position,
                size_delta,
                color);

            return target.GetComponent<Image>();
        }

        private Text CreateText(
            string name,
            Transform parent,
            Vector2 anchor_min,
            Vector2 anchor_max,
            Vector2 pivot,
            Vector2 anchored_position,
            Vector2 size_delta,
            int font_size,
            TextAnchor alignment)
        {
            GameObject target = new(name);
            target.transform.SetParent(parent, false);
            RectTransform rect_transform = target.AddComponent<RectTransform>();
            rect_transform.anchorMin = anchor_min;
            rect_transform.anchorMax = anchor_max;
            rect_transform.pivot = pivot;
            rect_transform.anchoredPosition = anchored_position;
            rect_transform.sizeDelta = size_delta;

            Text text = target.AddComponent<Text>();
            text.font = _font;
            text.fontSize = font_size;
            text.color = Color.white;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.supportRichText = true;
            return text;
        }

        private Font GetDefaultFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}
