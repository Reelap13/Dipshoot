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

        public static MatchPlayer Local { get; private set; }
        public int PlayerId => _player == null ? -1 : _player.PlayerId;

        public void Initialize(Player player, MatchPlayersController controller)
        {
            _controller = controller;
            _player = player;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (isOwned)
                Local = this;
        }

        private void OnDestroy()
        {
            if (Local == this)
                Local = null;
        }

        [TargetRpc]
        public void TargetLoadGameScene(string scene_name, string preset_id, int seed, string result_url)
        {
            Debug.Log($"[MatchLoad][Client] Load scene={scene_name} preset={preset_id} seed={seed}");
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

            Debug.Log($"[MatchLoad][Client] Ready preset={_preset_id} seed={_seed}");
            CommandMarkPlayerReadiness();
        }

        [TargetRpc]
        public void TargetReturnToMenu()
        {
            ClientAppRoot.Instance.LobbyActions.LeaveMatchView();
            ClientMatchPresetState.Clear();
        }

        public void RequestLeaveMatch()
        {
            if (!isOwned)
                return;

            CommandLeaveMatch();
        }

        [Command]
        private void CommandMarkPlayerReadiness()
        {
            Debug.Log($"[MatchLoad][Server] Ready player={_player?.PlayerId ?? -1}");
            _controller.MarkReadiness(_player);
        }

        [Command]
        private void CommandLeaveMatch()
        {
            _controller.ProcessPlayerLeave(_player);
        }
    }
}
