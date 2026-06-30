using System;
using Mirror;
using Scripts.UI.SceneUI;
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
            string nickname = GetNickname(player_id);
            CmdRegisterOnServer(player_id, nickname);
            StartFadeIn();
        }

        private void StartFadeIn()
        {
            SceneUI scene_ui = SceneUI.Instance;
            if (scene_ui != null && scene_ui.Fader != null)
                StartCoroutine(scene_ui.Fader.FadeIn());
        }

        [Command]
        private void CmdRegisterOnServer(Guid player_id, string nickname)
        {
            ConnectionController.Instance.RegisterClient(player_id, nickname, connectionToClient);
            GetComponent<NetworkMatch>().matchId = player_id;
        }

        private Guid GetPlayerId()
        {
            return _production_build ? ClientIdentity.СlientId : ClientIdentity.GenerateId();
        }

        private string GetNickname(Guid player_id)
        {
            if (_production_build)
                return $"P-{player_id.ToString("N")[..6].ToUpperInvariant()}";

            string test_player_id = ClientConnectionSettings.PlayerId;
            return $"Test{test_player_id}";
        }
    }
}
