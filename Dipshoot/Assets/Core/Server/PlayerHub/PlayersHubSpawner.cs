using System;
using Mirror;
using Server.Data;
using Server.ServerSide;
using UnityEngine;

namespace Server.PlayerHub
{
    public class PlayersHubSpawner : NetworkBehaviour
    {
        [SerializeField] private PlayerHubConnector _hub_prefab;

        private void Awake()
        {
            PlayersController.Instance.OnConnected.AddListener(CreatePlayerHub);
        }

        private void CreatePlayerHub(Player player)
        {
            var hub = NetworkUtils.NetworkInstantiate(_hub_prefab, transform, transform);
            hub.GetComponent<NetworkMatch>().matchId = player.ClientId;
            player.AddNetworkObject(hub.netIdentity);
            hub.GetComponent<PlayerHubController>().Initialize(player);
        }
    }
}