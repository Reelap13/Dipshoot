using Mirror;
using UnityEngine;

public class ConnectorToServer : MonoBehaviour
{
    [SerializeField] private NetworkManager _manager;
    [SerializeField] private string _address = "localhost";

    public void CreateServerConnection()
    {
        _manager.networkAddress = _address;
        _manager.StartServer();
    }
    
    public void CreateHostConnection()
    {
        _manager.networkAddress = _address;
        _manager.StartHost();
    }

    public void CreateClientConnection()
    {
        _manager.networkAddress = _address;
        _manager.StartClient();
    }
}
