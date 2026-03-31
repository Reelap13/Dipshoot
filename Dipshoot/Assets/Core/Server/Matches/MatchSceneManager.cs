using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Server.Match
{
    public class MatchSceneManager : MonoBehaviour
    {
        [field: SerializeField]
        public MatchController MatchController;

        public Scene Scene { get; private set; }

        public void CreateMatchScene()
        {
            IEnumerator DealyLoadPlayers()
            {
                yield return null;
                MatchController.ConnectPlayers();
            }

            Scene = SceneManager.CreateScene("Match" + MatchController.MatchData.MatchId, new CreateSceneParameters(LocalPhysicsMode.Physics3D));
            StartCoroutine(DealyLoadPlayers());
        }

        public void AddObjectToScene(GameObject obj)
        {
            SceneManager.MoveGameObjectToScene(obj, Scene);
        }

        private void OnDestroy()
        {
            SceneManager.UnloadSceneAsync(Scene);
        }
    }
}