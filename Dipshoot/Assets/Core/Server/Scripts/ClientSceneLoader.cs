using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Server.ClientSide
{
    public class ClientSceneLoader : MonoBehaviour
    {
        public IEnumerator LoadMatchScene(string sceneName)
        {
            var async = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            yield return async;

            var scene = SceneManager.GetSceneByName(sceneName);
            SceneManager.SetActiveScene(scene);
            SceneManager.MoveGameObjectToScene(gameObject, scene);

            yield return null; 


            OnLoaded?.Invoke();
        }

        public event Action OnLoaded;
    }
}