using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Server.ClientSide
{
    public class ClientSceneLoader : MonoBehaviour
    {
        [SerializeField] private bool _disable_other_scenes_ui = true;

        public IEnumerator LoadMatchScene(string sceneName)
        {
            var async = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            yield return async;

            var scene = SceneManager.GetSceneByName(sceneName);
            SceneManager.SetActiveScene(scene);
            if (_disable_other_scenes_ui)
                DisableOtherScenesUI(scene);

            //SceneManager.MoveGameObjectToScene(gameObject, scene); host mode break

            yield return null; 


            OnLoaded?.Invoke();
        }

        private void DisableOtherScenesUI(Scene active_scene)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene == active_scene || !scene.IsValid() || !scene.isLoaded)
                    continue;

                DisableSceneUI(scene);
            }
        }

        private void DisableSceneUI(Scene scene)
        {
            GameObject[] root_objects = scene.GetRootGameObjects();
            foreach (GameObject root_object in root_objects)
            {
                DisableComponents(root_object.GetComponentsInChildren<Canvas>(true));
                DisableComponents(root_object.GetComponentsInChildren<GraphicRaycaster>(true));
                DisableComponents(root_object.GetComponentsInChildren<EventSystem>(true));
                DisableComponents(root_object.GetComponentsInChildren<BaseInputModule>(true));
            }
        }

        private void DisableComponents<T>(T[] components) where T : Behaviour
        {
            foreach (T component in components)
            {
                if (component == null)
                    continue;

                component.enabled = false;
            }
        }

        public event Action OnLoaded;
    }
}
