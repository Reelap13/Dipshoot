using System.Collections;
using Scripts.UI.SceneUI;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Core.ClientPresentation
{
    public class ClientSceneFlowController : MonoBehaviour
    {
        public IEnumerator LoadMatchScene(string scene_name)
        {
            ClientAppRoot app_root = ClientAppRoot.Instance;
            app_root.MatchStore.BeginLoading(scene_name);
            app_root.PresentationRoot.SetState(ClientPresentationState.MatchLoading);

            yield return FadeOut();

            AsyncOperation async = SceneManager.LoadSceneAsync(scene_name, LoadSceneMode.Additive);
            if (async != null)
                yield return async;

            Scene scene = SceneManager.GetSceneByName(scene_name);
            if (scene.IsValid() && scene.isLoaded)
            {
                SceneManager.SetActiveScene(scene);
                app_root.SceneLifecycle.SuspendPresentationInPreviousScenes(scene);
            }

            app_root.MatchStore.MarkSceneLoaded();
            app_root.PresentationRoot.SetState(ClientPresentationState.Match);

            yield return FadeIn();
        }

        public IEnumerator CleanupPreviousScenesAfterMatchLoaded()
        {
            Scene active_scene = SceneManager.GetActiveScene();
            yield return ClientAppRoot.Instance.SceneLifecycle.CleanupPreviousScenes(active_scene);
        }

        public IEnumerator ReturnToMenuFromMatch()
        {
            ClientAppRoot app_root = ClientAppRoot.Instance;
            app_root.PresentationRoot.SetState(ClientPresentationState.MatchLoading);

            yield return FadeOut();

            string match_scene_name = app_root.MatchStore.MatchSceneName;
            if (!string.IsNullOrEmpty(match_scene_name))
            {
                Scene match_scene = SceneManager.GetSceneByName(match_scene_name);
                if (match_scene.IsValid() && match_scene.isLoaded)
                {
                    ClearEditorSelection();
                    AsyncOperation unload_operation = SceneManager.UnloadSceneAsync(match_scene);
                    if (unload_operation != null)
                        yield return unload_operation;
                }
            }

            app_root.MatchStore.FinishMatch();
            app_root.PresentationRoot.SetState(ClientPresentationState.MainMenu);

            yield return FadeIn();
        }

        private IEnumerator FadeOut()
        {
            SceneUI scene_ui = SceneUI.Instance;
            if (scene_ui == null || scene_ui.Fader == null)
                yield break;

            yield return scene_ui.Fader.FadeOut();
        }

        private IEnumerator FadeIn()
        {
            SceneUI scene_ui = SceneUI.Instance;
            if (scene_ui == null || scene_ui.Fader == null)
                yield break;

            yield return scene_ui.Fader.FadeIn();
        }

        private static void ClearEditorSelection()
        {
#if UNITY_EDITOR
            Selection.activeObject = null;
#endif
        }
    }
}
