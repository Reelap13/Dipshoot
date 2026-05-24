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
            _scene_loader.OnLoaded -= OnSceneLoaded;
            _scene_loader.OnLoaded += OnSceneLoaded;
            StartCoroutine(_scene_loader.LoadMatchScene(scene_name));
        }

        private void OnSceneLoaded()
        {
            _scene_loader.OnLoaded -= OnSceneLoaded;
            CommandMarkPlayerReadiness();
        }

        [Command]
        private void CommandMarkPlayerReadiness()
        {
            _controller.MarkReadiness(_player);
        }
    }
}
