using System;
using UnityEngine;

namespace Server.Data
{
    public class Client
    {
        public Guid Id;
        public string Nickname;
        public int ConnectionId;

        public Client(Guid id, string nickname, int connection_id)
        {
            Id = id;
            Nickname = nickname;
            ConnectionId = connection_id;
        }
    }
}