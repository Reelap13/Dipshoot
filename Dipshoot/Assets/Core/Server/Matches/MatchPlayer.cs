using Mirror;
using Server.ClientSide;
using Server.Data;
using UnityEngine;

namespace Server.Match
{
    public class MatchPlayer : NetworkBehaviour
    {
        [SerializeField] private ClientSceneLoader _scene_loader;

        private MatchPlayersController _controller;
        private Player _player;

        public void Initialize(Player player, MatchPlayersController controller)
        {
            _controller = controller;
            _player = player;
        }

        [TargetRpc]
        public void TargetLoadGameScene(string scene_name)
        {
            Debug.Log("Start loading");
            _scene_loader.OnLoaded += () =>
            {
                Debug.Log("Finish loading");
                CommandMarkPlayerReadiness();
            };
            StartCoroutine(_scene_loader.LoadMatchScene(scene_name));
        }

        [Command]
        private void CommandMarkPlayerReadiness()
        {
            _controller.MarkReadiness(_player);
        }
    }
}