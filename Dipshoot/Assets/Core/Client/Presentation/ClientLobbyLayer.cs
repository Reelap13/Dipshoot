using System.Collections.Generic;
using Game.MatchConfig;
using Game.MatchMode;
using Server.Lobby;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Core.ClientPresentation
{
    public class ClientLobbyLayer : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _lobby_code_text;
        [SerializeField] private TextMeshProUGUI _capacity_text;
        [SerializeField] private List<TextMeshProUGUI> _player_rows;
        [SerializeField] private List<TextMeshProUGUI> _red_player_rows;
        [SerializeField] private List<TextMeshProUGUI> _blue_player_rows;
        [SerializeField] private List<TextMeshProUGUI> _spectator_player_rows;
        [SerializeField] private TextMeshProUGUI _selected_preset_text;
        [SerializeField] private TMP_Dropdown _preset_dropdown;
        [SerializeField] private Button _preset_cycle_button;
        [SerializeField] private Toggle _tutorial_mode_toggle;
        [SerializeField] private Button _switch_team_button;
        [SerializeField] private Button _leave_button;
        [SerializeField] private Button _start_game_button;

        private ClientUiLayer _layer;
        private ClientLobbyMatchmakingView _matchmaking_view;
        private readonly List<string> _preset_ids = new();
        private bool _suppress_preset_event;

        private void Awake()
        {
            _layer = GetOrAddLayer();
            _layer.Initialize(ClientUiLayerKind.Lobby);

            ValidatePrefabReferences();
            _matchmaking_view = CreateMatchmakingView();
            HideLegacyMatchSettings();
            _leave_button.onClick.AddListener(Leave);
            _start_game_button.onClick.AddListener(StartGame);
            _switch_team_button.onClick.AddListener(SwitchTeam);
            if (_matchmaking_view != null)
                _matchmaking_view.OpenLobbyButton.onClick.AddListener(OpenLobby);

            ClientAppRoot app_root = ClientAppRoot.Instance;
            app_root.LobbyStore.OnLobbyUpdated += UpdateView;
            app_root.SessionStore.OnUpdated += UpdateView;
            UpdateView(app_root.LobbyStore.CurrentLobby);
        }

        private void OnDestroy()
        {
            _leave_button.onClick.RemoveListener(Leave);
            _start_game_button.onClick.RemoveListener(StartGame);
            if (_switch_team_button != null)
                _switch_team_button.onClick.RemoveListener(SwitchTeam);
            if (_matchmaking_view != null)
                _matchmaking_view.OpenLobbyButton.onClick.RemoveListener(OpenLobby);

            if (!ClientAppRoot.HasInstance)
                return;

            ClientAppRoot app_root = ClientAppRoot.Instance;
            app_root.LobbyStore.OnLobbyUpdated -= UpdateView;
            app_root.SessionStore.OnUpdated -= UpdateView;
        }

        private void UpdateView() => UpdateView(ClientAppRoot.Instance.LobbyStore.CurrentLobby);
        private void UpdateView(LobbyData lobby)
        {
            if (lobby == null)
            {
                ClearView();
                return;
            }

            _lobby_code_text.text = $"Код: {lobby.Code}";
            int players_count = lobby.Players != null ? lobby.Players.Count : 0;
            _capacity_text.text = $"Игроки: {players_count}/{lobby.PlayersCapacity}";

            int player_id = ClientAppRoot.Instance.SessionStore.PlayerId;
            LobbyPlayerData local_player = lobby.GetPlayer(player_id);
            bool is_host = local_player != null && local_player.Type == LobbyPlayerType.HOST;

            ClearRows(_red_player_rows);
            ClearRows(_blue_player_rows);
            ClearRows(_spectator_player_rows);
            FillTeamRows(lobby, TeamId.Red, _red_player_rows, is_host, player_id);
            FillTeamRows(lobby, TeamId.Blue, _blue_player_rows, is_host, player_id);
            FillTeamRows(lobby, TeamId.Spectator, _spectator_player_rows, is_host, player_id);

            _start_game_button.gameObject.SetActive(is_host);
            _matchmaking_view?.SetState(lobby, is_host);
            HideLegacyMatchSettings();
        }

        private void ClearView()
        {
            _lobby_code_text.text = "Code: -";
            _capacity_text.text = "Players: 0/0";
            ClearRows(_red_player_rows);
            ClearRows(_blue_player_rows);
            ClearRows(_spectator_player_rows);
            if (_selected_preset_text != null)
                _selected_preset_text.text = "Preset: -";
            if (_tutorial_mode_toggle != null)
                _tutorial_mode_toggle.SetIsOnWithoutNotify(false);

            _start_game_button.gameObject.SetActive(false);
            _matchmaking_view?.SetState(null, false);
        }

        private void FillTeamRows(LobbyData lobby, TeamId team_id, List<TextMeshProUGUI> rows, bool is_host, int local_player_id)
        {
            if (lobby.Players == null || rows == null)
                return;

            int row_index = 0;
            for (int i = 0; i < lobby.Players.Count && row_index < rows.Count; i++)
            {
                LobbyPlayerData player = lobby.Players[i];
                if (player.Team != team_id)
                    continue;

                ConfigurePlayerRow(rows[row_index], player, is_host && player.PlayerId != local_player_id);
                row_index++;
            }
        }

        private void ClearRows(List<TextMeshProUGUI> rows)
        {
            if (rows == null)
                return;

            for (int i = 0; i < rows.Count; i++)
                ConfigurePlayerRow(rows[i], null, false);
        }

        private void ConfigurePlayerRow(TextMeshProUGUI row, LobbyPlayerData player, bool can_switch_team)
        {
            if (row == null)
                return;

            row.text = player == null
                ? string.Empty
                : $"{player.Nickname} ({FormatPlayerType(player.Type)})";
            row.raycastTarget = can_switch_team;

            Button button = row.GetComponent<Button>();
            if (can_switch_team)
            {
                if (button == null)
                    button = row.gameObject.AddComponent<Button>();

                int player_id = player.PlayerId;
                button.targetGraphic = row;
                button.transition = Selectable.Transition.ColorTint;
                button.interactable = true;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => SwitchPlayerTeam(player_id));
                return;
            }

            if (button == null)
                return;

            button.onClick.RemoveAllListeners();
            button.interactable = false;
        }

        private void UpdatePresetView(LobbyData lobby, bool is_host)
        {
            PopulatePresetOptions();
            string preset_name = GetPresetName(lobby.SelectedPresetId);
            if (_selected_preset_text != null)
                _selected_preset_text.text = $"Preset: {preset_name}";

            if (_preset_dropdown != null)
            {
                _preset_dropdown.gameObject.SetActive(is_host);
                int index = Mathf.Max(0, _preset_ids.IndexOf(lobby.SelectedPresetId));
                _suppress_preset_event = true;
                _preset_dropdown.value = index;
                _preset_dropdown.RefreshShownValue();
                _suppress_preset_event = false;
            }

            if (_preset_cycle_button != null)
                _preset_cycle_button.gameObject.SetActive(is_host);
        }

        private void UpdateTutorialModeView(LobbyData lobby, bool is_host)
        {
            if (_tutorial_mode_toggle == null)
                return;

            _tutorial_mode_toggle.gameObject.SetActive(is_host);
            _tutorial_mode_toggle.interactable = is_host;
            _tutorial_mode_toggle.SetIsOnWithoutNotify(lobby.IsTutorialMode);
        }

        private void PopulatePresetOptions()
        {
            _preset_ids.Clear();
            MatchPresetRegistry registry = MatchPresetRegistry.LoadDefault();
            if (registry == null)
                return;

            List<TMP_Dropdown.OptionData> options = new();
            for (int i = 0; i < registry.Presets.Count; i++)
            {
                MatchPreset preset = registry.Presets[i];
                if (preset == null)
                    continue;

                _preset_ids.Add(preset.Id);
                options.Add(new TMP_Dropdown.OptionData(string.IsNullOrEmpty(preset.DisplayName) ? preset.Id : preset.DisplayName));
            }

            if (_preset_dropdown == null)
                return;

            _preset_dropdown.ClearOptions();
            _preset_dropdown.AddOptions(options);
        }

        private string GetPresetName(string preset_id)
        {
            MatchPreset preset = MatchPresetRegistry.GetPreset(preset_id);
            if (preset == null)
                return string.IsNullOrEmpty(preset_id) ? "-" : preset_id;

            return string.IsNullOrEmpty(preset.DisplayName) ? preset.Id : preset.DisplayName;
        }

        private string FormatPlayerType(LobbyPlayerType type)
        {
            return type == LobbyPlayerType.HOST ? "Host" : "Client";
        }

        private void Leave()
        {
            ClientAppRoot.Instance.LobbyActions.LeaveFromLobby();
        }

        private void StartGame()
        {
            ClientAppRoot.Instance.LobbyActions.StartGame();
        }

        private void OpenLobby()
        {
            ClientAppRoot.Instance.LobbyActions.OpenLobby();
        }

        private void SwitchTeam()
        {
            ClientAppRoot.Instance.LobbyActions.SwitchTeam();
        }

        private void SwitchPlayerTeam(int player_id)
        {
            ClientAppRoot.Instance.LobbyActions.SwitchPlayerTeam(player_id);
        }

        private void SelectPreset(int index)
        {
            if (_suppress_preset_event || index < 0 || index >= _preset_ids.Count)
                return;

            ClientAppRoot.Instance.LobbyActions.SelectPreset(_preset_ids[index]);
        }

        private void SelectNextPreset()
        {
            LobbyData lobby = ClientAppRoot.Instance.LobbyStore.CurrentLobby;
            if (lobby == null)
                return;

            PopulatePresetOptions();
            if (_preset_ids.Count == 0)
                return;

            int current = _preset_ids.IndexOf(lobby.SelectedPresetId);
            int next = (current + 1) % _preset_ids.Count;
            ClientAppRoot.Instance.LobbyActions.SelectPreset(_preset_ids[next]);
        }

        private void SetTutorialMode(bool enabled)
        {
            ClientAppRoot.Instance.LobbyActions.SetTutorialMode(enabled);
        }

        private ClientUiLayer GetOrAddLayer()
        {
            ClientUiLayer layer = gameObject.GetComponent<ClientUiLayer>();
            return layer != null ? layer : gameObject.AddComponent<ClientUiLayer>();
        }

        private void ValidatePrefabReferences()
        {
            if (_player_rows != null)
            {
                for (int i = 0; i < _player_rows.Count; i++)
                {
                    if (_player_rows[i] != null)
                        _player_rows[i].gameObject.SetActive(false);
                }
            }

            _red_player_rows ??= new List<TextMeshProUGUI>();
            _blue_player_rows ??= new List<TextMeshProUGUI>();
            _spectator_player_rows ??= new List<TextMeshProUGUI>();

            if (_red_player_rows.Count == 0)
                _red_player_rows = CreateTeamColumn("Red Team", new Color(0.45f, 0.08f, 0.08f, 0.78f), new Vector2(-180f, -40f));
            if (_blue_player_rows.Count == 0)
                _blue_player_rows = CreateTeamColumn("Blue Team", new Color(0.08f, 0.16f, 0.5f, 0.78f), new Vector2(180f, -40f));
            if (_spectator_player_rows.Count == 0)
                Debug.LogError($"{nameof(ClientLobbyLayer)} spectator rows are not assigned.", this);
            if (_tutorial_mode_toggle == null)
                Debug.LogWarning($"{nameof(ClientLobbyLayer)} tutorial mode toggle is not assigned.", this);
            if (_switch_team_button == null)
                _switch_team_button = CreateRuntimeButton("SwitchTeamButton", new Vector2(0f, -370f), new Vector2(230f, 42f), "Switch Team");

            if (_lobby_code_text == null || _capacity_text == null || _leave_button == null || _start_game_button == null)
                Debug.LogError($"{nameof(ClientLobbyLayer)} prefab is not fully assigned.", this);
        }

        private ClientLobbyMatchmakingView CreateMatchmakingView()
        {
            ClientLobbyMatchmakingView existing =
                GetComponentInChildren<ClientLobbyMatchmakingView>(true);
            if (existing != null)
                return existing;

            GameObject prefab = Resources.Load<GameObject>(
                "ClientUI/Lobby/ClientLobbyMatchmakingView");
            if (prefab == null)
            {
                Debug.LogError("Missing ClientLobbyMatchmakingView prefab.", this);
                return null;
            }

            Transform parent = transform.Find("LobbyPanel");
            GameObject instance = Instantiate(prefab, parent == null ? transform : parent);
            return instance.GetComponent<ClientLobbyMatchmakingView>();
        }

        private void HideLegacyMatchSettings()
        {
            if (_selected_preset_text != null)
                _selected_preset_text.gameObject.SetActive(false);
            if (_preset_dropdown != null)
                _preset_dropdown.gameObject.SetActive(false);
            if (_preset_cycle_button != null)
                _preset_cycle_button.gameObject.SetActive(false);
            if (_tutorial_mode_toggle != null)
                _tutorial_mode_toggle.gameObject.SetActive(false);
        }

        private List<TextMeshProUGUI> CreateTeamColumn(string title, Color color, Vector2 position)
        {
            return CreateTeamColumn(title, color, position, new Vector2(300f, 220f), 8);
        }

        private List<TextMeshProUGUI> CreateTeamColumn(string title, Color color, Vector2 position, Vector2 size, int rows_count)
        {
            GameObject panel = new(title.Replace(" ", "") + "Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(transform, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = panel.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;

            TextMeshProUGUI title_text = CreateRuntimeText(title.Replace(" ", "") + "Title", new Vector2(0f, size.y * 0.5f - 28f), new Vector2(size.x - 40f, 26f), 20, TextAlignmentOptions.Center, panel.transform);
            title_text.text = title;

            List<TextMeshProUGUI> rows = new();
            for (int i = 0; i < rows_count; i++)
                rows.Add(CreateRuntimeText($"{title}Row{i}", new Vector2(0f, size.y * 0.5f - 60f - i * 22f), new Vector2(size.x - 40f, 22f), 16, TextAlignmentOptions.Left, panel.transform));

            return rows;
        }

        private TextMeshProUGUI CreateRuntimeText(string name, Vector2 position, Vector2 size, int fontSize, TextAlignmentOptions alignment, Transform parent = null)
        {
            GameObject obj = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent == null ? transform : parent, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            TextMeshProUGUI text = obj.GetComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private Button CreateRuntimeButton(string name, Vector2 position, Vector2 size, string label)
        {
            GameObject obj = new(name, typeof(RectTransform), typeof(Image), typeof(Button));
            obj.transform.SetParent(transform, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            obj.GetComponent<Image>().color = new Color(0.16f, 0.16f, 0.16f, 0.95f);

            TextMeshProUGUI text = CreateRuntimeText(name + "Text", Vector2.zero, size, 17, TextAlignmentOptions.Center, obj.transform);
            text.text = label;
            return obj.GetComponent<Button>();
        }
    }
}
