using Mirror;
using Core.ClientPresentation;
using Game.MatchConfig;
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
        private string _preset_id;
        private int _seed;
        private string _result_url;

        public void Initialize(Player player, MatchPlayersController controller)
        {
            _controller = controller;
            _player = player;
        }

        [TargetRpc]
        public void TargetLoadGameScene(string scene_name, string preset_id, int seed, string result_url)
        {
            _preset_id = preset_id;
            _seed = seed;
            _result_url = result_url;
            _scene_loader.OnLoaded -= OnSceneLoaded;
            _scene_loader.OnLoaded += OnSceneLoaded;
            StartCoroutine(_scene_loader.LoadMatchScene(scene_name));
        }

        private void OnSceneLoaded()
        {
            _scene_loader.OnLoaded -= OnSceneLoaded;
            if (!ClientMatchMapGenerator.GenerateSelectedPreset(_preset_id, _seed, _result_url))
                return;

            CommandMarkPlayerReadiness();
        }

        [TargetRpc]
        public void TargetReturnToMenu()
        {
            ClientAppRoot.Instance.LobbyActions.LeaveMatchView();
            ClientMatchPresetState.Clear();
        }

        [Command]
        private void CommandMarkPlayerReadiness()
        {
            _controller.MarkReadiness(_player);
        }
    }
}
