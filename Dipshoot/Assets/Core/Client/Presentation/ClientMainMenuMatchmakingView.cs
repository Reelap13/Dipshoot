using Server.ServerSide;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Core.ClientPresentation
{
    public class ClientMainMenuMatchmakingView : MonoBehaviour
    {
        [SerializeField] private Button _search_button;
        [SerializeField] private TextMeshProUGUI _online_text;
        [SerializeField] private TextMeshProUGUI _matches_text;
        [SerializeField] private TextMeshProUGUI _search_text;

        public Button SearchButton => _search_button;

        private void Awake()
        {
            _search_button ??= transform.Find("FindMatchButton")?.GetComponent<Button>();
            _online_text ??= transform.Find("OnlineText")?.GetComponent<TextMeshProUGUI>();
            _matches_text ??= transform.Find("MatchesText")?.GetComponent<TextMeshProUGUI>();
            _search_text ??= transform.Find("SearchText")?.GetComponent<TextMeshProUGUI>();
        }

        public void SetSnapshot(ServerPopulationSnapshot snapshot)
        {
            _online_text.text = $"Онлайн: {snapshot.TotalOnlinePlayers}";
            _matches_text.text =
                $"В матчах: {snapshot.PlayersInMatches} · Матчей: {snapshot.ActiveMatches}";
            _search_text.text =
                $"В поиске: {snapshot.PlayersInPublicLobbies} · Лобби: {snapshot.PublicLobbies}";
        }

        public void SetSearchButtonVisible(bool visible)
        {
            _search_button.gameObject.SetActive(visible);
        }
    }
}
