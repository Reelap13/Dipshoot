using Mirror;
using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConnectorToServer : MonoBehaviour
{
    [SerializeField] private NetworkManager _manager;
    [SerializeField] private string _address = "localhost";
    [SerializeField] private string _config_file_name = "server_config.json";
    [SerializeField] private TMP_InputField _player_id_input;
    [SerializeField] private Button _start_client_button;
    [SerializeField] private bool _create_missing_player_id_input = true;

    private void Awake()
    {
        EnsurePlayerIdUi();
        LoadPlayerIdInput();
        UpdateStartClientButton();
    }

    private void OnDestroy()
    {
        if (_player_id_input != null)
            _player_id_input.onValueChanged.RemoveListener(HandlePlayerIdChanged);
    }

    public void CreateServerConnection()
    {
        ApplyConfiguredAddress();
        _manager.StartServer();
    }
    
    public void CreateHostConnection()
    {
        ApplyConfiguredAddress();
        _manager.StartHost();
    }

    public void CreateClientConnection()
    {
        if (!SavePlayerId())
            return;

        ApplyConfiguredAddress();
        _manager.StartClient();
    }

    private void EnsurePlayerIdUi()
    {
        if (_start_client_button == null)
            _start_client_button = FindButton("StartClient");

        if (_player_id_input == null)
            _player_id_input = GetComponentInChildren<TMP_InputField>(true);

        if (_player_id_input == null && _create_missing_player_id_input)
            _player_id_input = CreatePlayerIdInput();

        if (_player_id_input != null)
            _player_id_input.onValueChanged.AddListener(HandlePlayerIdChanged);
    }

    private void LoadPlayerIdInput()
    {
        if (_player_id_input != null)
            _player_id_input.SetTextWithoutNotify(ClientConnectionSettings.PlayerId);
    }

    private void HandlePlayerIdChanged(string _)
    {
        UpdateStartClientButton();
    }

    private bool SavePlayerId()
    {
        string player_id = _player_id_input == null ? ClientConnectionSettings.PlayerId : _player_id_input.text.Trim();
        if (string.IsNullOrWhiteSpace(player_id))
        {
            UpdateStartClientButton();
            return false;
        }

        ClientConnectionSettings.SavePlayerId(player_id);
        return true;
    }

    private void UpdateStartClientButton()
    {
        if (_start_client_button == null)
            return;

        string player_id = _player_id_input == null ? ClientConnectionSettings.PlayerId : _player_id_input.text;
        _start_client_button.interactable = !string.IsNullOrWhiteSpace(player_id);
    }

    private Button FindButton(string object_name)
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null && buttons[i].name == object_name)
                return buttons[i];
        }

        return null;
    }

    private TMP_InputField CreatePlayerIdInput()
    {
        RectTransform parent = _start_client_button == null
            ? GetComponentInChildren<Canvas>()?.transform as RectTransform
            : _start_client_button.transform.parent as RectTransform;
        if (parent == null)
            return null;

        GameObject root = new("PlayerIdInput", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField));
        root.layer = parent.gameObject.layer;
        root.transform.SetParent(parent, false);

        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -330f);
        rect.sizeDelta = new Vector2(320f, 70f);

        Image image = root.GetComponent<Image>();
        image.color = Color.white;

        TMP_InputField input = root.GetComponent<TMP_InputField>();
        TextMeshProUGUI text = CreateInputText("Text", root.transform, string.Empty, new Color(0.2f, 0.2f, 0.2f, 1f));
        TextMeshProUGUI placeholder = CreateInputText("Placeholder", root.transform, "Player id", new Color(0.2f, 0.2f, 0.2f, 0.5f));
        placeholder.fontStyle = FontStyles.Italic;
        input.textViewport = rect;
        input.textComponent = text;
        input.placeholder = placeholder;
        return input;
    }

    private static TextMeshProUGUI CreateInputText(string name, Transform parent, string text, Color color)
    {
        GameObject obj = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        obj.layer = parent.gameObject.layer;
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(12f, 0f);
        rect.offsetMax = new Vector2(-12f, 0f);

        TextMeshProUGUI label = obj.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = 24f;
        label.color = color;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        return label;
    }

    private void ApplyConfiguredAddress()
    {
        if (_manager == null)
            _manager = NetworkManager.singleton;

        string address = LoadAddressFromConfig();
        if (!string.IsNullOrWhiteSpace(address))
            _address = address.Trim();

        if (_manager != null)
            _manager.networkAddress = _address;
    }

    private string LoadAddressFromConfig()
    {
        string path = GetConfigPath();
        EnsureConfigFile(path);

        try
        {
            ServerConfig config = JsonUtility.FromJson<ServerConfig>(File.ReadAllText(path));
            if (config == null)
                return _address;

            if (!string.IsNullOrWhiteSpace(config.ServerAddress))
                return config.ServerAddress;
            if (!string.IsNullOrWhiteSpace(config.Address))
                return config.Address;
            if (!string.IsNullOrWhiteSpace(config.Ip))
                return config.Ip;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[ConnectorToServer] Failed to read {path}: {exception.Message}");
        }

        return _address;
    }

    private void EnsureConfigFile(string path)
    {
        if (File.Exists(path))
            return;

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonUtility.ToJson(new ServerConfig { ServerAddress = _address }, true));
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[ConnectorToServer] Failed to create {path}: {exception.Message}");
        }
    }

    private string GetConfigPath()
    {
        string root = Application.isEditor
            ? Directory.GetParent(Application.dataPath)?.FullName
            : Directory.GetParent(Application.dataPath)?.FullName;

        return Path.Combine(root ?? Application.persistentDataPath, _config_file_name);
    }

    [Serializable]
    private class ServerConfig
    {
        public string ServerAddress = "localhost";
        public string Address = null;
        public string Ip = null;
    }
}
