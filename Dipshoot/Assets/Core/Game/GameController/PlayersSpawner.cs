using Game.Players;
using Mirror;
using Server.Data;
using Server.Match;
using Server.ServerSide;
using UnityEngine;

namespace Game
{
    public class PlayersSpawner : MonoBehaviour
    {
        [field: SerializeField]
        public DipshootGameController GameController { get; private set; }
        [SerializeField] private PlayerCharacter _character_prefab;

        private void Awake()
        {
            GameController.OnStartingGame.AddListener(SpawnPlayersCharacters);
        }

        private void SpawnPlayersCharacters()
        {
            foreach (var lobby_player in GameController.MatchController.MatchData.LobbyData.Players)
            {
                Player player = PlayersController.Instance.GetPlayer(lobby_player.PlayerId);
                PlayerCharacter character = NetworkUtils.NetworkMatchInstantiate(
                    _character_prefab, GameController.Scene, GameController.MatchId, 
                    GameController.LevelCreator.Level.GetRandomSpawnPoint(), transform);
                player.AddNetworkObject(character.netIdentity);
                character.InitializeServerSimulation(GameController.TickManager);
            }
        }
    }
}
