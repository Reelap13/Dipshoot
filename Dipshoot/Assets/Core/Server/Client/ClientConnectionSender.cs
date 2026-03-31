using System;
using Mirror;
using Server.ServerSide;
using UnityEngine;

namespace Server.ClientSide
{
    public class ClientConnectionSender : NetworkBehaviour
    {
        [SerializeField] private bool _production_build = false;

        public override void OnStartLocalPlayer()
        {
            Guid player_id = GetPlayerId();
            string nickname = GetNickname();
            CmdRegisterOnServer(player_id, nickname);
        }

        [Command]
        private void CmdRegisterOnServer(Guid player_id, string nickname)
        {
            ConnectionController.Instance.RegisterClient(player_id, nickname, connectionToClient);
        }

        private Guid GetPlayerId()
        {
            return _production_build ? ClientIdentity.ÑlientId : ClientIdentity.GenerateId();
        }

        private string GetNickname()
        {
            return $"Test{UnityEngine.Random.Range(1, 99999):D5}";
            //return PlayerPrefs.GetString("Nickname");
        }
    }
}