using System.Collections.Generic;
using Game.MatchConfig;
using Game.MatchMode;
using Server.Lobby;
using UnityEngine;
using UnityEngine.UI;

namespace Core.ClientPresentation
{
    public class ClientLobbyLayer : MonoBehaviour
    {
        [SerializeField] private Text _lobby_code_text;
        [SerializeField] private Text _capacity_text;
        [SerializeField] private List<Text> _player_rows;
        [SerializeField] private List<Text> _red_player_rows;
        [SerializeField] private List<Text> _blue_player_rows;
        [SerializeField] private Text _selected_preset_text;
        [SerializeField] private Dropdown _preset_dropdown;
        [SerializeField] private Button _preset_cycle_button;
        [SerializeField] private Button _switch_team_button;
        [SerializeField] private Button _leave_button;
        [SerializeField] private Button _start_game_button;

        private ClientUiLayer _layer;
        private readonly List<string> _preset_ids = new();
        private bool _suppress_preset_event;

        private void Awake()
        {
            _layer = GetOrAddLayer();
            _layer.Initialize(ClientUiLayerKind.Lobby);

            EnsureRuntimeLobbyUi();
            _leave_button.onClick.AddListener(Leave);
            _start_game_button.onClick.AddListener(StartGame);
            _switch_team_button.onClick.AddListener(SwitchTeam);
            if (_preset_dropdown != null)
                _preset_dropdown.onValueChanged.AddListener(SelectPreset);
            if (_preset_cycle_button != null)
                _preset_cycle_button.onClick.AddListener(SelectNextPreset);

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
            if (_preset_dropdown != null)
                _preset_dropdown.onValueChanged.RemoveListener(SelectPreset);
            if (_preset_cycle_button != null)
                _preset_cycle_button.onClick.RemoveListener(SelectNextPreset);

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

            _lobby_code_text.text = $"Code: {lobby.Code}";
            int players_count = lobby.Players != null ? lobby.Players.Count : 0;
            _capacity_text.text = $"Players: {players_count}/{lobby.PlayersCapacity}";

            ClearRows(_red_player_rows);
            ClearRows(_blue_player_rows);
            FillTeamRows(lobby, TeamId.Red, _red_player_rows);
            FillTeamRows(lobby, TeamId.Blue, _blue_player_rows);

            int player_id = ClientAppRoot.Instance.SessionStore.PlayerId;
            LobbyPlayerData local_player = lobby.GetPlayer(player_id);
            bool is_host = local_player != null && local_player.Type == LobbyPlayerType.HOST;
            _start_game_button.gameObject.SetActive(is_host);
            UpdatePresetView(lobby, is_host);
        }

        private void ClearView()
        {
            _lobby_code_text.text = "Code: -";
            _capacity_text.text = "Players: 0/0";
            ClearRows(_red_player_rows);
            ClearRows(_blue_player_rows);
            if (_selected_preset_text != null)
                _selected_preset_text.text = "Preset: -";

            _start_game_button.gameObject.SetActive(false);
        }

        private void FillTeamRows(LobbyData lobby, TeamId team_id, List<Text> rows)
        {
            if (lobby.Players == null || rows == null)
                return;

            int row_index = 0;
            for (int i = 0; i < lobby.Players.Count && row_index < rows.Count; i++)
            {
                LobbyPlayerData player = lobby.Players[i];
                if (player.Team != team_id)
                    continue;

                rows[row_index].text = $"{player.Nickname} ({FormatPlayerType(player.Type)})";
                row_index++;
            }
        }

        private static void ClearRows(List<Text> rows)
        {
            if (rows == null)
                return;

            for (int i = 0; i < rows.Count; i++)
                rows[i].text = string.Empty;
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

        private void PopulatePresetOptions()
        {
            _preset_ids.Clear();
            MatchPresetRegistry registry = MatchPresetRegistry.LoadDefault();
            if (registry == null)
                return;

            List<Dropdown.OptionData> options = new();
            for (int i = 0; i < registry.Presets.Count; i++)
            {
                MatchPreset preset = registry.Presets[i];
                if (preset == null)
                    continue;

                _preset_ids.Add(preset.Id);
                options.Add(new Dropdown.OptionData(string.IsNullOrEmpty(preset.DisplayName) ? preset.Id : preset.DisplayName));
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

        private void SwitchTeam()
        {
            ClientAppRoot.Instance.LobbyActions.SwitchTeam();
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

        private ClientUiLayer GetOrAddLayer()
        {
            ClientUiLayer layer = gameObject.GetComponent<ClientUiLayer>();
            return layer != null ? layer : gameObject.AddComponent<ClientUiLayer>();
        }

        private void EnsureRuntimeLobbyUi()
        {
            if (_player_rows != null)
            {
                for (int i = 0; i < _player_rows.Count; i++)
                {
                    if (_player_rows[i] != null)
                        _player_rows[i].gameObject.SetActive(false);
                }
            }

            _red_player_rows ??= new List<Text>();
            _blue_player_rows ??= new List<Text>();
            if (_red_player_rows.Count == 0)
                _red_player_rows = CreateTeamColumn("Red Team", new Color(0.45f, 0.08f, 0.08f, 0.78f), new Vector2(-180f, -40f));
            if (_blue_player_rows.Count == 0)
                _blue_player_rows = CreateTeamColumn("Blue Team", new Color(0.08f, 0.16f, 0.5f, 0.78f), new Vector2(180f, -40f));

            if (_selected_preset_text == null)
                _selected_preset_text = CreateRuntimeText("SelectedPresetText", new Vector2(0f, -92f), new Vector2(420f, 30f), 18, TextAnchor.MiddleCenter);

            if (_preset_dropdown == null && _preset_cycle_button == null)
                _preset_cycle_button = CreateRuntimeButton("PresetCycleButton", new Vector2(0f, -130f), new Vector2(230f, 34f), "Change Preset");

            if (_switch_team_button == null)
                _switch_team_button = CreateRuntimeButton("SwitchTeamButton", new Vector2(0f, -272f), new Vector2(230f, 42f), "Switch Team");
        }

        private List<Text> CreateTeamColumn(string title, Color color, Vector2 position)
        {
            GameObject panel = new(title.Replace(" ", "") + "Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(transform, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(300f, 220f);
            panel.GetComponent<Image>().color = color;

            Text title_text = CreateRuntimeText(title.Replace(" ", "") + "Title", new Vector2(0f, 90f), new Vector2(260f, 26f), 20, TextAnchor.MiddleCenter, panel.transform);
            title_text.text = title;

            List<Text> rows = new();
            for (int i = 0; i < 8; i++)
            {
                Text row = CreateRuntimeText($"{title}Row{i}", new Vector2(0f, 58f - i * 22f), new Vector2(260f, 22f), 16, TextAnchor.MiddleLeft, panel.transform);
                rows.Add(row);
            }

            return rows;
        }

        private Text CreateRuntimeText(string name, Vector2 position, Vector2 size, int fontSize, TextAnchor anchor, Transform parent = null)
        {
            GameObject obj = new(name, typeof(RectTransform), typeof(Text));
            obj.transform.SetParent(parent == null ? transform : parent, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Text text = obj.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = Color.white;
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

            Text text = CreateRuntimeText(name + "Text", Vector2.zero, size, 17, TextAnchor.MiddleCenter, obj.transform);
            text.text = label;
            return obj.GetComponent<Button>();
        }
    }
}
