using Game.MatchMode;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Core.ClientPresentation
{
    public class ClientScoreboardPlayerRowView : MonoBehaviour
    {
        [SerializeField] private Image _local_player_highlight;
        [SerializeField] private TextMeshProUGUI _nickname_text;
        [SerializeField] private TextMeshProUGUI _kills_text;
        [SerializeField] private TextMeshProUGUI _deaths_text;
        [SerializeField] private TextMeshProUGUI _point_time_text;

        public void Bind(ScoreboardPlayerState state, bool is_local_player)
        {
            if (_local_player_highlight != null)
                _local_player_highlight.gameObject.SetActive(is_local_player);
            if (_nickname_text != null)
                _nickname_text.text = state.Nickname ?? string.Empty;
            if (_kills_text != null)
                _kills_text.text = state.Kills.ToString();
            if (_deaths_text != null)
                _deaths_text.text = state.Deaths.ToString();
            if (_point_time_text != null)
                _point_time_text.text = FormatTime(state.CapturePresenceSeconds);
        }

        private static string FormatTime(int seconds)
        {
            int safe_seconds = Mathf.Max(0, seconds);
            return $"{safe_seconds / 60}:{safe_seconds % 60:00}";
        }
    }
}
