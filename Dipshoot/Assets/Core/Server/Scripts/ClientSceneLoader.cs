using System;
using System.Collections;
using Core.ClientPresentation;
using UnityEngine;

namespace Server.ClientSide
{
    public class ClientSceneLoader : MonoBehaviour
    {
        public IEnumerator LoadMatchScene(string sceneName)
        {
            yield return ClientAppRoot.Instance.SceneFlow.LoadMatchScene(sceneName);
            OnLoaded?.Invoke();
            yield return null;
            yield return ClientAppRoot.Instance.SceneFlow.CleanupPreviousScenesAfterMatchLoaded();
        }

        public event Action OnLoaded;
    }
}
