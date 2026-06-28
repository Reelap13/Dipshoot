using System.Collections.Generic;
using Game.MatchMode;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Core.ClientPresentation
{
    public class ClientScoreboardTeamPanel : MonoBehaviour
    {
        [SerializeField] private Image _team_background;
        [SerializeField] private Image _team_logo;
        [SerializeField] private TextMeshProUGUI _team_name_text;
        [SerializeField] private TextMeshProUGUI _score_text;
        [SerializeField] private TextMeshProUGUI _round_wins_text;
        [SerializeField] private Transform _rows_container;
        [SerializeField] private ClientScoreboardPlayerRowView _player_row_prefab;
        [SerializeField] private LayoutElement _layout_element;
        [SerializeField] private float _header_height = 42f;
        [SerializeField] private float _player_info_height = 38f;

        private readonly List<ClientScoreboardPlayerRowView> _rows = new();

        public float PreferredHeight { get; private set; }

        public void Configure(string team_name, Color team_color)
        {
            if (_team_name_text != null)
                _team_name_text.text = team_name;
            if (_team_background != null)
                _team_background.color = new Color(
                    team_color.r,
                    team_color.g,
                    team_color.b,
                    0.78f);
            if (_team_logo != null)
                _team_logo.color = team_color;
        }

        public void SetData(
            IReadOnlyList<ScoreboardPlayerState> players,
            int score,
            int round_wins,
            int local_player_id)
        {
            if (_score_text != null)
                _score_text.text = score.ToString();
            if (_round_wins_text != null)
                _round_wins_text.text = $"РАУНДЫ: {round_wins}";

            int count = players == null ? 0 : players.Count;
            PreferredHeight = CalculatePreferredHeight(
                count,
                out float players_height);
            if (_layout_element != null)
            {
                _layout_element.minHeight = PreferredHeight;
                _layout_element.preferredHeight = PreferredHeight;
            }
            ApplyRuntimeHeight(PreferredHeight, players_height);
            EnsureRows(count);

            for (int i = 0; i < _rows.Count; i++)
            {
                ClientScoreboardPlayerRowView row = _rows[i];
                bool is_active = i < count;
                row.gameObject.SetActive(is_active);
                if (is_active)
                    row.Bind(players[i], players[i].PlayerId == local_player_id);
            }

            if (_rows_container is RectTransform rows_rect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rows_rect);
        }

        private float CalculatePreferredHeight(
            int player_count,
            out float players_height)
        {
            float header_height = _header_height;
            if (_team_background != null)
                header_height = _team_background.rectTransform.rect.height;

            float row_height = _player_info_height;
            if (_player_row_prefab != null &&
                _player_row_prefab.transform is RectTransform row_rect)
            {
                float preferred_height = LayoutUtility.GetPreferredHeight(row_rect);
                row_height = preferred_height > 0f
                    ? preferred_height
                    : row_rect.rect.height;
            }

            float spacing = 0f;
            if (_rows_container != null &&
                _rows_container.TryGetComponent(out VerticalLayoutGroup rows_layout))
            {
                spacing = rows_layout.spacing * Mathf.Max(0, player_count - 1);
            }

            players_height =
                row_height * Mathf.Max(player_count, 0.5f) +
                spacing;
            return header_height + players_height;
        }

        private void ApplyRuntimeHeight(
            float team_height,
            float players_height)
        {
            if (transform is RectTransform team_rect)
            {
                team_rect.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Vertical,
                    team_height);
            }

            if (_rows_container != null &&
                _rows_container.parent is RectTransform players_panel)
            {
                players_panel.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Vertical,
                    players_height);
            }
        }

        private void EnsureRows(int count)
        {
            if (_player_row_prefab == null || _rows_container == null)
                return;

            while (_rows.Count < count)
            {
                ClientScoreboardPlayerRowView row =
                    Instantiate(_player_row_prefab, _rows_container, false);
                row.gameObject.SetActive(false);
                _rows.Add(row);
            }
        }
    }
}
