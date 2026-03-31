using System.Collections.Generic;
using Mirror;
using Server.Data;
using Unity.VisualScripting;
using UnityEngine;

namespace Server
{
    public class ConnectorsCreator : NetworkBehaviour
    {
        [SerializeField] private NetworkIdentity _connector_prefab;

        private List<NetworkIdentity> _connectors = new();

        public void CreateConnectors(List<Player> players)
        {
            _connectors = new();
            foreach (var player in players)
            {
                NetworkIdentity connector = NetworkUtils.NetworkInstantiate(_connector_prefab, transform, transform);
                player.AddNetworkObject(connector);
                _connectors.Add(connector);
            }
        }

        public List<T> GetConnectors<T>()
        {
            List<T> connectors = new();
            foreach (var connector in _connectors)
                connectors.Add(connector.GetComponent<T>());
            return connectors;
        }
    }
}