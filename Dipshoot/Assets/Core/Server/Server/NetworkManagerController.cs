using Mirror;
using UnityEngine;

namespace Server.ServerSide
{
    public class NetworkManagerController : NetworkManager
    {
        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            ConnectionController.Instance.UnregisterClient(conn);

            base.OnServerDisconnect(conn);
        }
    }
}