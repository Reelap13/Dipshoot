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
            PreferredHeight =
                _header_height +
                _player_info_height * Mathf.Max(count, 0.5f);
            if (_layout_element != null)
            {
                _layout_element.minHeight = PreferredHeight;
                _layout_element.preferredHeight = PreferredHeight;
            }
            EnsureRows(count);

            for (int i = 0; i < _rows.Count; i++)
            {
                ClientScoreboardPlayerRowView row = _rows[i];
                bool is_active = i < count;
                row.gameObject.SetActive(is_active);
                if (is_active)
                    row.Bind(players[i], players[i].PlayerId == local_player_id);
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
