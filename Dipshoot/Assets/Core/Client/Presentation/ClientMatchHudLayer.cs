using Game.MatchMode;
using UnityEngine;
using UnityEngine.UI;

namespace Core.ClientPresentation
{
    public class ClientMatchHudLayer : MonoBehaviour
    {
        [SerializeField] private Color _red_color = new(0.95f, 0.18f, 0.14f, 1f);
        [SerializeField] private Color _blue_color = new(0.16f, 0.45f, 1f, 1f);
        [SerializeField] private Color _neutral_color = new(0.7f, 0.7f, 0.7f, 1f);
        [SerializeField] private Color _contested_color = new(1f, 0.78f, 0.18f, 1f);

        [SerializeField] private GameObject _phase_banner;
        [SerializeField] private GameObject _result_panel;
        [SerializeField] private Text _score_text;
        [SerializeField] private Text _round_text;
        [SerializeField] private Text _phase_text;
        [SerializeField] private Text _result_text;
        [SerializeField] private Text _point_text;
        [SerializeField] private Text _inside_text;
        [SerializeField] private Image _point_owner_strip;
        [SerializeField] private Image _point_progress_fill;

        private TeamControlModeController _mode_controller;
        private ClientUiLayer _layer;

        private void Awake()
        {
            _layer = GetOrAddLayer();
            _layer.Initialize(ClientUiLayerKind.MatchHud);
        }

        private void Update()
        {
            _mode_controller = ClientAppRoot.Instance.MatchStore.ModeController;
            if (_mode_controller == null || _score_text == null)
                return;

            UpdateScorePanel();
            UpdatePhaseBanner();
            UpdateResultPanel();
            UpdatePointPanel();
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

        private ClientUiLayer GetOrAddLayer()
        {
            ClientUiLayer layer = gameObject.GetComponent<ClientUiLayer>();
            return layer != null ? layer : gameObject.AddComponent<ClientUiLayer>();
        }
    }
}
