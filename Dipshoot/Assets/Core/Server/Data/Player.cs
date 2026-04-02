using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace Server.Data
{
    public class Player
    {
        public int PlayerId;
        public Guid ClientId;
        public string Nickname;
        public Client Client;
        public List<NetworkIdentity> OwnObjects;

        public Player(int player_id, Client client)
        {
            PlayerId = player_id;
            ClientId = client.Id;
            Nickname = client.Nickname;
            Client = client;
            OwnObjects = new List<NetworkIdentity>();
        }

        public void AddNetworkObject(NetworkIdentity obj)
        {
            if (obj == null || OwnObjects.Contains(obj))
                return;

            if (!IsHasClient())
                return;

            OwnObjects.Add(obj);

            NetworkConnectionToClient conn = GetConnection();
            if (conn != null)
            {
                obj.AssignClientAuthority(conn);
                NetworkServer.RebuildObservers(obj, true);
            }
        }

        public void RemoveNetworkObject(NetworkIdentity obj)
        {
            if (obj == null)
                return;

            OwnObjects.Remove(obj);

            if (obj.connectionToClient != null)
                obj.RemoveClientAuthority();
        }

        public bool IsHasClient() => Client != null;
        public NetworkConnectionToClient GetConnection()
        {
            if (!IsHasClient())
            {
                Debug.LogError("Try to get connection of player without client");
                return null;
            }

            if (NetworkServer.connections.TryGetValue(Client.ConnectionId, out var conn))
                return conn;

            Debug.LogError($"Try to get connection of player '{PlayerId}' with missing connection '{Client.ConnectionId}'");
            return null;
        }
    }
}
