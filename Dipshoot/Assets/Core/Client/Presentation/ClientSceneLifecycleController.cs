using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Core.ClientPresentation
{
    public class ClientSceneLifecycleController : MonoBehaviour
    {
        [SerializeField] private ClientSceneRetentionPolicy _retention_policy =
            ClientSceneRetentionPolicy.UnloadPreviousScenes;

        private readonly HashSet<string> _processed_scene_names = new();
        private Transform _preserved_objects_root;

        public void SuspendPresentationInPreviousScenes(Scene active_scene)
        {
            List<Scene> scenes = GetProcessableScenes(active_scene);
            foreach (Scene scene in scenes)
            {
                DisablePresentationObjects(scene);
            }
        }

        public IEnumerator CleanupPreviousScenes(Scene active_scene)
        {
            List<Scene> scenes = GetProcessableScenes(active_scene);
            foreach (Scene scene in scenes)
            {
                PreserveCriticalObjects(scene);

                if (_retention_policy == ClientSceneRetentionPolicy.HidePreviousScenes)
                {
                    HideScene(scene);
                    continue;
                }

                ClearEditorSelection();
                AsyncOperation unload_operation = SceneManager.UnloadSceneAsync(scene);
                if (unload_operation != null)
                    yield return unload_operation;
            }
        }

        private List<Scene> GetProcessableScenes(Scene active_scene)
        {
            List<Scene> scenes = new();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (CanProcessScene(scene, active_scene))
                    scenes.Add(scene);
            }

            return scenes;
        }

        private bool CanProcessScene(Scene scene, Scene active_scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return false;

            if (scene == active_scene)
                return false;

            if (_processed_scene_names.Contains(scene.name))
                return false;

            return true;
        }

        private void PreserveCriticalObjects(Scene scene)
        {
            GameObject[] root_objects = scene.GetRootGameObjects();
            foreach (GameObject root_object in root_objects)
            {
                if (root_object == null || !IsCriticalRoot(root_object))
                    continue;

                DisablePresentationComponents(root_object);
                DontDestroyOnLoad(root_object);
                root_object.transform.SetParent(GetPreservedObjectsRoot(), true);
            }

            _processed_scene_names.Add(scene.name);
        }

        private void DisablePresentationObjects(Scene scene)
        {
            GameObject[] root_objects = scene.GetRootGameObjects();
            foreach (GameObject root_object in root_objects)
            {
                if (root_object == null)
                    continue;

                DisablePresentationComponents(root_object);
            }
        }

        private void HideScene(Scene scene)
        {
            GameObject[] root_objects = scene.GetRootGameObjects();
            foreach (GameObject root_object in root_objects)
            {
                if (root_object == null || IsCriticalRoot(root_object))
                    continue;

                root_object.SetActive(false);
            }

            _processed_scene_names.Add(scene.name);
        }

        private bool IsCriticalRoot(GameObject root_object)
        {
            return root_object.GetComponent<ClientScenePersistentObject>() != null;
        }

        private void DisablePresentationComponents(GameObject root_object)
        {
            DisableComponents(root_object.GetComponentsInChildren<Canvas>(true));
            DisableComponents(root_object.GetComponentsInChildren<GraphicRaycaster>(true));
            DisableComponents(root_object.GetComponentsInChildren<EventSystem>(true));
            DisableComponents(root_object.GetComponentsInChildren<BaseInputModule>(true));
            DisableComponents(root_object.GetComponentsInChildren<Camera>(true));
            DisableComponents(root_object.GetComponentsInChildren<AudioListener>(true));
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

        private Transform GetPreservedObjectsRoot()
        {
            if (_preserved_objects_root != null)
                return _preserved_objects_root;

            GameObject target = new("ClientPreservedSceneObjects");
            target.transform.SetParent(ClientAppRoot.Instance.transform, false);
            _preserved_objects_root = target.transform;
            return _preserved_objects_root;
        }

        private static void ClearEditorSelection()
        {
#if UNITY_EDITOR
            Selection.activeObject = null;
#endif
        }
    }
}
