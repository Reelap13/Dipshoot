#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeoSaveGames.SceneManagement
{
    [CustomEditor(typeof(NeoSceneManager))]
    public class NeoSceneManagerEditor : Editor
    {
        private const int k_SceneOK = 0;
        private const int k_SceneNotSet = 1;
        private const int k_SceneNotBuilt = 2;

        public bool CheckIsValid()
        {
            var loadingScene = serializedObject.FindProperty("m_DefaultLoadingScreenIndex");
            var menuScene = serializedObject.FindProperty("m_DefaultMainMenuIndex");
            return (loadingScene.intValue != -1 && menuScene.intValue != -1);                
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            EditorGUILayout.HelpBox("Loading screens are scenes which are loaded before loading the target scene asynchronously, and unloaded once the target scene loading is complete.\n\nThe default loading screen and main menu scene are stored by scene build index. If you modify the build settings, make sure to check the default scenes below are still correct.", MessageType.Info);
            EditorGUILayout.Space();

            var menuSceneProperty = serializedObject.FindProperty("m_DefaultMainMenuIndex");
            var loadingSceneProperty = serializedObject.FindProperty("m_DefaultLoadingScreenIndex");
            var buildScenes = EditorBuildSettings.scenes;

            // Get the last enabled scene
            int last = -1;
            for (int i = 0; i < buildScenes.Length; ++i)
                if (buildScenes[i].enabled)
                    ++last;

            SceneAsset menuSceneObject = null;
            SceneAsset loadingSceneObject = null;

            int menuIndex = menuSceneProperty.intValue;
            if (menuIndex > -1)
            {
                if (menuIndex > last)
                {
                    menuSceneProperty.intValue = -1;
                    Debug.LogError("Default loading scene index was out of bounds for build settings, setting to -1.");
                }
                else
                {
                    EditorBuildSettingsScene scene = null;
                    for (int i = 0; i < buildScenes.Length; ++i)
                    {
                        scene = buildScenes[i];
                        if (scene.enabled)
                        {
                            --menuIndex;
                            if (menuIndex < 0)
                                break;
                        }
                    }
                    menuSceneObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path);
                }
            }

            int loadingIndex = loadingSceneProperty.intValue;
            if (loadingIndex > -1)
            {
                if (loadingIndex > last)
                {
                    loadingSceneProperty.intValue = -1;
                    Debug.LogError("Default loading scene index was out of bounds for build settings, setting to -1.");
                }
                else
                {
                    EditorBuildSettingsScene scene = null;
                    for (int i = 0; i < buildScenes.Length; ++i)
                    {
                        scene = buildScenes[i];
                        if (scene.enabled)
                        {
                            --loadingIndex;
                            if (loadingIndex < 0)
                                break;
                        }
                    }
                    loadingSceneObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path);
                }
            }

            // Show the scene fields
            var newMenuSceneObj = EditorGUILayout.ObjectField("Default Main Menu Scene", menuSceneObject, typeof(SceneAsset), false);
            var newLoadingSceneObj = EditorGUILayout.ObjectField("Default Loading Screen", loadingSceneObject, typeof(SceneAsset), false);

            // Get main menu index from new scene
            if (newMenuSceneObj != menuSceneObject)
            {
                if (newMenuSceneObj == null)
                    menuSceneProperty.intValue = -1;
                else
                {
                    bool found = false;
                    var path = AssetDatabase.GetAssetPath(newMenuSceneObj);
                    for (int i = 0; i < buildScenes.Length; ++i)
                    {
                        if (buildScenes[i].path == path)
                        {
                            menuSceneProperty.intValue = i;
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                        Debug.LogError("Main menu scene must be added to build settings");
                }
            }

            // Get loading scene index from new scene
            if (newLoadingSceneObj != loadingSceneObject)
            {
                if (newLoadingSceneObj == null)
                    loadingSceneProperty.intValue = -1;
                else
                {
                    bool found = false;
                    var path = AssetDatabase.GetAssetPath(newLoadingSceneObj);
                    for (int i = 0; i < buildScenes.Length; ++i)
                    {
                        if (buildScenes[i].path == path)
                        {
                            loadingSceneProperty.intValue = i;
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                        Debug.LogError("Loading scene must be added to build settings");
                }
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_MinLoadScreenTime"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_OnSceneLoaded"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_OnSceneLoadFailed"));

            serializedObject.ApplyModifiedProperties();
        }
    }
}

#endif
