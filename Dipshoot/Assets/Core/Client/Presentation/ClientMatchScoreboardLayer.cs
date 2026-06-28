using System.Collections.Generic;
using Game.MatchMode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Core.ClientPresentation
{
    public class ClientMatchScoreboardLayer : MonoBehaviour
    {
        [SerializeField] private GameObject _content_root;
        [SerializeField] private RectTransform _scoreboard_panel;
        [SerializeField] private Transform _teams_container;
        [SerializeField] private ClientScoreboardTeamPanel _team_panel_prefab;
        [SerializeField] private float _team_spacing = 16f;
        [SerializeField] private float _panel_vertical_padding = 48f;
        [SerializeField] private Color _blue_color = new(0.16f, 0.45f, 1f, 1f);
        [SerializeField] private Color _red_color = new(0.95f, 0.18f, 0.14f, 1f);

        private readonly List<ScoreboardPlayerState> _blue_players = new();
        private readonly List<ScoreboardPlayerState> _red_players = new();
        private ClientUiLayer _layer;
        private ClientScoreboardTeamPanel _blue_team_panel;
        private ClientScoreboardTeamPanel _red_team_panel;
        private MatchHudController _hud_controller;
        private RectTransform _content_rect;
        private float _content_vertical_padding;
        private int _last_scoreboard_revision = -1;
        private int _last_blue_score = -1;
        private int _last_red_score = -1;
        private int _last_blue_round_wins = -1;
        private int _last_red_round_wins = -1;
        private bool _is_visible;

        private void Awake()
        {
            _content_rect = _content_root != null
                ? _content_root.transform as RectTransform
                : null;
            if (_content_rect != null && _scoreboard_panel != null)
            {
                _content_vertical_padding = Mathf.Max(
                    0f,
                    _content_rect.rect.height -
                    _scoreboard_panel.rect.height);
            }

            _layer = GetComponent<ClientUiLayer>();
            if (_layer == null)
            {
                Debug.LogError($"{nameof(ClientMatchScoreboardLayer)} requires {nameof(ClientUiLayer)}.", this);
                enabled = false;
                return;
            }

            _layer.Initialize(ClientUiLayerKind.MatchHud);
            EnsureTeamPanels();
            SetScoreboardVisible(false);
        }

        private void Update()
        {
            bool should_show =
                ClientAppRoot.Instance.PresentationRoot.State == ClientPresentationState.Match &&
                Keyboard.current != null &&
                Keyboard.current.tabKey.isPressed;

            if (should_show != _is_visible)
                SetScoreboardVisible(should_show);

            if (_is_visible)
                RefreshIfRequired();
        }

        private void OnDisable()
        {
            SetScoreboardVisible(false);
        }

        private void OnApplicationFocus(bool has_focus)
        {
            if (!has_focus)
                SetScoreboardVisible(false);
        }

        private void EnsureTeamPanels()
        {
            if (_team_panel_prefab == null || _teams_container == null)
                return;

            if (_blue_team_panel == null)
                _blue_team_panel = Instantiate(_team_panel_prefab, _teams_container, false);
            if (_red_team_panel == null)
                _red_team_panel = Instantiate(_team_panel_prefab, _teams_container, false);

            _blue_team_panel.name = "BlueTeamPanel";
            _red_team_panel.name = "RedTeamPanel";
            _blue_team_panel.Configure("СИНЯЯ КОМАНДА", _blue_color);
            _red_team_panel.Configure("КРАСНАЯ КОМАНДА", _red_color);
        }

        private void SetScoreboardVisible(bool is_visible)
        {
            _is_visible = is_visible;
            if (_content_root != null)
                _content_root.SetActive(is_visible);

            if (!is_visible)
                return;

            _last_scoreboard_revision = -1;
            RefreshIfRequired();
        }

        private void RefreshIfRequired()
        {
            ClientMatchStore match_store = ClientAppRoot.Instance.MatchStore;
            MatchHudController hud_controller = match_store.HudController;
            TeamControlModeController mode_controller = match_store.ModeController;
            if (hud_controller == null || mode_controller == null)
                return;

            bool changed =
                _hud_controller != hud_controller ||
                _last_scoreboard_revision != hud_controller.ScoreboardRevision ||
                _last_blue_score != mode_controller.BlueScore ||
                _last_red_score != mode_controller.RedScore ||
                _last_blue_round_wins != mode_controller.BlueRoundWins ||
                _last_red_round_wins != mode_controller.RedRoundWins;
            if (!changed)
                return;

            _hud_controller = hud_controller;
            _last_scoreboard_revision = hud_controller.ScoreboardRevision;
            _last_blue_score = mode_controller.BlueScore;
            _last_red_score = mode_controller.RedScore;
            _last_blue_round_wins = mode_controller.BlueRoundWins;
            _last_red_round_wins = mode_controller.RedRoundWins;

            SplitAndSortPlayers(hud_controller.ScoreboardPlayers);
            int local_player_id = ClientAppRoot.Instance.SessionStore.PlayerId;
            _blue_team_panel?.SetData(
                _blue_players,
                mode_controller.BlueScore,
                mode_controller.BlueRoundWins,
                local_player_id);
            _red_team_panel?.SetData(
                _red_players,
                mode_controller.RedScore,
                mode_controller.RedRoundWins,
                local_player_id);

            if (_scoreboard_panel != null &&
                _blue_team_panel != null &&
                _red_team_panel != null)
            {
                float height =
                    GetPanelVerticalPadding() +
                    GetTeamSpacing() +
                    _blue_team_panel.PreferredHeight +
                    _red_team_panel.PreferredHeight;
                SetPanelHeightKeepingTop(height);
            }
        }

        private float GetTeamSpacing()
        {
            if (_teams_container != null &&
                _teams_container.TryGetComponent(out VerticalLayoutGroup layout))
            {
                return layout.spacing;
            }

            return _team_spacing;
        }

        private float GetPanelVerticalPadding()
        {
            if (_teams_container is not RectTransform teams_rect)
                return _panel_vertical_padding;

            return Mathf.Max(
                0f,
                teams_rect.offsetMin.y - teams_rect.offsetMax.y);
        }

        private void SetPanelHeightKeepingTop(float height)
        {
            if (_content_rect != null)
            {
                SetHeightKeepingTop(
                    _content_rect,
                    height + _content_vertical_padding);
                _scoreboard_panel.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Vertical,
                    height);
            }
            else
            {
                SetHeightKeepingTop(_scoreboard_panel, height);
            }

            if (_teams_container is RectTransform teams_rect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(teams_rect);
        }

        private static void SetHeightKeepingTop(
            RectTransform rect,
            float height)
        {
            float previous_height = rect.rect.height;
            float top_position =
                rect.anchoredPosition.y +
                (1f - rect.pivot.y) * previous_height;

            rect.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                height);

            Vector2 position = rect.anchoredPosition;
            position.y =
                top_position -
                (1f - rect.pivot.y) * height;
            rect.anchoredPosition = position;
        }

        private void SplitAndSortPlayers(
            IReadOnlyDictionary<int, ScoreboardPlayerState> players)
        {
            _blue_players.Clear();
            _red_players.Clear();

            if (players != null)
            {
                foreach (ScoreboardPlayerState player in players.Values)
                {
                    if (player.TeamId == TeamId.Blue)
                        _blue_players.Add(player);
                    else if (player.TeamId == TeamId.Red)
                        _red_players.Add(player);
                }
            }

            _blue_players.Sort(ComparePlayers);
            _red_players.Sort(ComparePlayers);
        }

        private static int ComparePlayers(
            ScoreboardPlayerState left,
            ScoreboardPlayerState right)
        {
            int result = right.Kills.CompareTo(left.Kills);
            if (result != 0)
                return result;

            result = right.CapturePresenceSeconds.CompareTo(left.CapturePresenceSeconds);
            if (result != 0)
                return result;

            result = left.Deaths.CompareTo(right.Deaths);
            if (result != 0)
                return result;

            return string.Compare(
                left.Nickname,
                right.Nickname,
                System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
