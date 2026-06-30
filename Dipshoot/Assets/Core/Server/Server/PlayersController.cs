using System;
using System.Collections.Generic;
using Mirror;
using Server.Data;
using UnityEngine;
using UnityEngine.Events;

namespace Server.ServerSide
{
    public class PlayersController : Singleton<PlayersController>
    {
        [NonSerialized] public UnityEvent<Player> OnConnected = new();
        [NonSerialized] public UnityEvent<Player> OnReconnected = new();
        [NonSerialized] public UnityEvent<Player> OnDisconnected = new();
        [NonSerialized] public UnityEvent<Player> OnDeleted = new();

        private List<Player> _players = new();
        private int _player_id = 0;

        public int ConnectedPlayersCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _players.Count; i++)
                {
                    if (_players[i].IsHasClient())
                        count++;
                }

                return count;
            }
        }

        private void Awake()
        {
            ConnectionController.Instance.OnConnected.AddListener(ConnectPlayer);
            ConnectionController.Instance.OnDisconnected.AddListener(DisconnectPlayer);
        }

        private void ConnectPlayer(Client client)
        {
            if (GetPlayer(client.Id) != null)
            {
                ReconnectPlayer(client);
                return;
            }

            Player player = new Player(_player_id++, client);
            _players.Add(player);
            OnConnected.Invoke(player);
        }

        private void ReconnectPlayer(Client client)
        {
            Player player = GetPlayer(client.Id);
            if (player == null)
            {
                Debug.LogError($"Try to reconnect client '{client.Id}' without player object");
                return;
            }

            player.Client = client;
            EnableAllPlayerObjects(player);
            OnReconnected.Invoke(player);
        }

        private void DisconnectPlayer(Client client)
        {
            Player player = GetPlayer(client.Id);
            if (player == null)
            {
                Debug.LogError($"Try to disconnect client '{client.Id}' without player object");
                return;
            }

            player.Client = null;
            DisableAllPlayerObjects(player); 
            OnDisconnected.Invoke(player);
            DeletePlayer(player);
        }

        public void DeletePlayer(Player player)
        {
            if (player == null || player.Client != null || !_players.Contains(player))
                return;

            _players.Remove(player);
            OnDeleted.Invoke(player);
        }

        public Player GetPlayer(Guid clientId)
        {
            foreach (Player player in _players)
                if (player.ClientId == clientId)
                    return player;
            return null;
        }

        public Player GetPlayer(int player_id)
        {
            foreach (Player player in _players)
                if (player.PlayerId == player_id)
                    return player;
            return null;
        }

        private void DisableAllPlayerObjects(Player player)
        {
            foreach (var obj in player.OwnObjects)
            {
                if (obj == null || obj.connectionToClient == null)
                    continue;

                obj.RemoveClientAuthority();
            }
        }

        private void EnableAllPlayerObjects(Player player)
        {
            NetworkConnectionToClient conn = player.GetConnection();
            if (conn == null)
                return;

            foreach (var obj in player.OwnObjects)
            {
                if (obj == null)
                    continue;

                if (obj.connectionToClient == conn)
                    continue;

                if (obj.connectionToClient != null)
                    obj.RemoveClientAuthority();

                obj.AssignClientAuthority(conn);
            }
        }
    }
}
