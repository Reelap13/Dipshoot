using Game.MatchConfig;
using Game.MatchMode;
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
            if (_title_text != null)
                _title_text.text = "Match Finished";

            if (_winner_text != null)
                _winner_text.text = $"Winner: {FormatTeam(mode_controller.MatchWinner)}";

            if (_score_text != null)
                _score_text.text =
                    $"Rounds {mode_controller.RedRoundWins} - {mode_controller.BlueRoundWins}\n" +
                    $"Score {mode_controller.RedScore} - {mode_controller.BlueScore}";

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

        private static string FormatTeam(TeamId team_id)
        {
            return team_id switch
            {
                TeamId.Red => "Red",
                TeamId.Blue => "Blue",
                _ => "Draw",
            };
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
