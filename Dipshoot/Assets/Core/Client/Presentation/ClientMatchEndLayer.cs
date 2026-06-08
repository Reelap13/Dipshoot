using Game.MatchConfig;
using Game.MatchMode;
using Game.Players;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Core.ClientPresentation
{
    public class ClientMatchEndLayer : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _title_text;
        [SerializeField] private TextMeshProUGUI _winner_text;
        [SerializeField] private TextMeshProUGUI _score_text;
        [SerializeField] private Button _exit_to_menu_button;
        [SerializeField] private Button _survey_button;

        private ClientUiLayer _layer;

        private void Awake()
        {
            _layer = GetOrAddLayer();
            _layer.Initialize(ClientUiLayerKind.MatchEnd);
            DisablePassiveRaycasts();

            if (_exit_to_menu_button != null)
                _exit_to_menu_button.onClick.AddListener(ExitToMenu);
            if (_survey_button != null)
                _survey_button.onClick.AddListener(OpenSurvey);
        }

        private void OnDestroy()
        {
            if (_exit_to_menu_button != null)
                _exit_to_menu_button.onClick.RemoveListener(ExitToMenu);
            if (_survey_button != null)
                _survey_button.onClick.RemoveListener(OpenSurvey);
        }

        private void Update()
        {
            ClientAppRoot app_root = ClientAppRoot.Instance;
            TeamControlModeController mode_controller = app_root.MatchStore.ModeController;
            if (mode_controller == null)
                return;

            if (mode_controller.Phase == RoundPhase.Finished &&
                app_root.PresentationRoot.State is ClientPresentationState.Match or ClientPresentationState.MatchMenu)
                app_root.PresentationRoot.SetState(ClientPresentationState.MatchEnded);

            if (app_root.PresentationRoot.State == ClientPresentationState.MatchEnded)
                UpdateView(mode_controller);
        }

        private void UpdateView(TeamControlModeController mode_controller)
        {
            TeamId winner = mode_controller.MatchWinner;
            TeamId local_team = GetLocalTeam();

            if (_title_text != null)
            {
                _title_text.text = FormatResultTitle(winner, local_team);
                _title_text.color = winner == TeamId.None ? Color.gray : Color.white;
            }

            if (_winner_text != null)
            {
                _winner_text.gameObject.SetActive(true);
                _winner_text.richText = true;
                _winner_text.text = FormatWinnerText(winner);
            }

            if (_score_text != null)
            {
                string blue = ColorUtility.ToHtmlStringRGB(GetTeamColor(TeamId.Blue));
                string red = ColorUtility.ToHtmlStringRGB(GetTeamColor(TeamId.Red));
                _score_text.text = $"<color=#{blue}>{mode_controller.BlueScore}</color> - <color=#{red}>{mode_controller.RedScore}</color>";
            }

            if (_survey_button != null)
                _survey_button.gameObject.SetActive(!string.IsNullOrWhiteSpace(ClientMatchPresetState.ResultUrl));
        }

        private void ExitToMenu()
        {
            ClientAppRoot.Instance.LobbyActions.RequestLeaveMatch();
        }

        private void OpenSurvey()
        {
            if (!string.IsNullOrWhiteSpace(ClientMatchPresetState.ResultUrl))
                Application.OpenURL(ClientMatchPresetState.ResultUrl);
        }

        private static Color GetTeamColor(TeamId team_id)
        {
            return team_id switch
            {
                TeamId.Red => new Color(0.95f, 0.18f, 0.14f, 1f),
                TeamId.Blue => new Color(0.16f, 0.45f, 1f, 1f),
                _ => Color.gray,
            };
        }

        private static string FormatResultTitle(TeamId winner, TeamId local_team)
        {
            if (winner == TeamId.None)
                return "Draw!";

            if (local_team == TeamId.Spectator || local_team == TeamId.None)
                return "Match Ended!";

            return winner == local_team ? "Victory!" : "Defeat!";
        }

        private static string FormatWinnerText(TeamId winner)
        {
            if (winner == TeamId.None)
                return "No Team Wins!";

            string color = ColorUtility.ToHtmlStringRGB(GetTeamColor(winner));
            return $"<color=#{color}>{FormatTeamName(winner)}</color> Wins!";
        }

        private static string FormatTeamName(TeamId team_id)
        {
            return team_id switch
            {
                TeamId.Red => "Red Team",
                TeamId.Blue => "Blue Team",
                TeamId.Spectator => "Spectators",
                _ => "No Team",
            };
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

        private void DisablePassiveRaycasts()
        {
            Image exit_target = _exit_to_menu_button != null ? _exit_to_menu_button.targetGraphic as Image : null;
            Image survey_target = _survey_button != null ? _survey_button.targetGraphic as Image : null;
            Image[] images = GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image == exit_target || image == survey_target)
                    continue;

                image.raycastTarget = false;
            }
        }

        private ClientUiLayer GetOrAddLayer()
        {
            ClientUiLayer layer = gameObject.GetComponent<ClientUiLayer>();
            return layer != null ? layer : gameObject.AddComponent<ClientUiLayer>();
        }
    }
}
