using Mirror;
using System;
using System.IO;
using UnityEngine;

public class ConnectorToServer : MonoBehaviour
{
    [SerializeField] private NetworkManager _manager;
    [SerializeField] private string _address = "localhost";
    [SerializeField] private string _config_file_name = "server_config.json";

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
        ApplyConfiguredAddress();
        _manager.StartClient();
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
