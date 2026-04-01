#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

#if !UNITY_2021_2_OR_NEWER
using UnityEditor.Experimental.SceneManagement;
#endif

namespace NeoSaveGames.Serialization
{
    public static class NeoSerializationEditorUtilities
    {
        public static bool IsSceneValid(string sceneName)
        {
            if (Application.isPlaying)
                return Application.CanStreamedLevelBeLoaded(sceneName);
            else
            {
                var scenes = EditorBuildSettings.scenes;
                foreach (var s in scenes)
                {
                    if (s.path == sceneName || Path.GetFileNameWithoutExtension(s.path) == sceneName)
                        return true;
                }
                return false;
            }
        }

        public static bool IsSceneValid(int sceneIndex)
        {
            return (sceneIndex >= 0 && sceneIndex < SceneManager.sceneCountInBuildSettings);
        }

        public static void LayoutSceneNameField(SerializedProperty property)
        {
            LayoutSceneNameField(property, new GUIContent(property.displayName, property.tooltip));
        }

        public static void LayoutSceneIndexField(SerializedProperty property)
        {
            LayoutSceneIndexField(property, new GUIContent(property.displayName, property.tooltip));
        }

        public static void LayoutSceneNameField(SerializedProperty property, string label)
        {
            LayoutSceneNameField(property, new GUIContent(label, property.tooltip));
        }

        public static void LayoutSceneIndexField(SerializedProperty property, string label)
        {
            LayoutSceneIndexField(property, new GUIContent(label, property.tooltip));
        }

        public static void LayoutSceneNameField(SerializedProperty property, GUIContent label)
        {
            EditorGUILayout.PropertyField(property, label);

            bool isValid = IsSceneValid(property.stringValue);

            // Sort layout for small help box
            var helpRect = EditorGUILayout.BeginVertical(GUILayout.Height(EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing));
            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();

            // Draw help box
            if (isValid)
                EditorGUI.HelpBox(helpRect, "Scene is valid", MessageType.Info);
            else
            {
                var color = GUI.color;
                GUI.color = Color.red;
                EditorGUI.HelpBox(helpRect, "Scene not in build settings", MessageType.Error);
                GUI.color = color;
            }
        }

        public static void LayoutSceneIndexField(SerializedProperty property, GUIContent label)
        {
            EditorGUILayout.PropertyField(property, label);

            bool isValid = IsSceneValid(property.intValue);

            // Sort layout for small help box
            var helpRect = EditorGUILayout.BeginVertical(GUILayout.Height(EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing));
            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();

            // Draw help box
            if (isValid)
                EditorGUI.HelpBox(helpRect, "Scene is valid", MessageType.Info);
            else
            {
                var color = GUI.color;
                GUI.color = Color.red;
                EditorGUI.HelpBox(helpRect, "Scene not in build settings", MessageType.Error);
                GUI.color = color;
            }
        }

        public static void CheckProjectPrefabIDs()
        {
            // Scan prefabs
            var guids = AssetDatabase.FindAssets("t:GameObject");
            if (guids != null)
            {
                foreach (var guid in guids)
                {
                    // Load prefab object & get NeoSerializedGameObject
                    var obj = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                    if (obj != null)
                    {
                        if (obj.TryGetComponent(out NeoSerializedGameObject nsgo))
                        {
                            // Check & validate
                            nsgo.EditorCheckPrefabID();
                            nsgo.OnValidate();
                        }
                    }
                }

                // SAVE GAMES LOGGING - REPORT RESULTS
            }
        }

        public static void CheckOpenScene(Scene scene)
        {
            // Skip invalid scenes
            if (!scene.IsValid() || !scene.isLoaded)
                return;

            int found = 0;
            var objects = scene.GetRootGameObjects();
            foreach (var obj in objects)
            {
                if (obj.TryGetComponent(out NeoSerializedScene nss))
                {
                    // Check for multiple scene save objects
                    if (++found == 2)
                        Debug.LogError("Multiple NeoSerializedScene objects found in scene. This will create duplicate data in your saves.");

                    // Validate the scene (fills all serialized object containers and generates keys)
                    nss.EditorCheckScene();
                }
            }

            // SAVE GAMES LOGGING - REPORT RESULTS
        }

        public static void OnShowNestedObjects(SerializedObject so)
        {
            var cast = so.targetObject as INeoSerializedSceneObject;
            int childCount = cast.nestedObjects.count;

            var nested = so.FindProperty("m_NestedObjects");
            var rect = EditorGUILayout.GetControlRect(false);

            EditorGUI.BeginProperty(rect, GUIContent.none, nested);

            var foldoutProp = so.FindProperty("expandNestedObjectList");
            foldoutProp.boolValue = EditorGUI.Foldout(rect, foldoutProp.boolValue, $"Nested Objects ({childCount})", true);

            EditorGUI.EndProperty();

            if (foldoutProp.boolValue)
            {
                if (!Application.isPlaying)
                {
                    // Get serialized properties
                    var keys = nested.FindPropertyRelative("m_Keys");
                    var values = nested.FindPropertyRelative("m_Values");

                    // Iterate throught
                    for (int i = 0; i < keys.arraySize; ++i)
                    {
                        var valueProp = values.GetArrayElementAtIndex(i);
                        var k = keys.GetArrayElementAtIndex(i).intValue;
                        var v = valueProp.objectReferenceValue as NeoSerializedGameObject;

                        rect = EditorGUILayout.GetControlRect(true);
                        var label = EditorGUI.BeginProperty(rect, new GUIContent(k.ToString()), valueProp);

                        rect = EditorGUI.PrefixLabel(rect, label);

                        if (v != null)
                        {
#if UNITY_2021_1_OR_NEWER
                            if (EditorGUI.LinkButton(rect, v.name))
#else
                            if (GUI.Button(rect, v.name, EditorStyles.miniButton))
#endif
                                EditorGUIUtility.PingObject(v.gameObject); //Selection.activeObject = v;
                        }
                        else
                            EditorGUI.LabelField(rect, "<null>");


                        EditorGUI.EndProperty();
                    }
                }
                else
                {
                    foreach (var nsgo in cast.nestedObjects)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(nsgo.serializationKey.ToString(), GUILayout.Width(EditorGUIUtility.labelWidth));
#if UNITY_2021_1_OR_NEWER
                        if (EditorGUILayout.LinkButton(nsgo.gameObject.name))
#else
                        if (GUILayout.Button(nsgo.gameObject.name, EditorStyles.miniButton))
#endif
                            EditorGUIUtility.PingObject(nsgo.gameObject); //Selection.activeObject = v;
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }

            EditorGUILayout.Space();
        }

        [InitializeOnLoadMethod]
        static void NeoSerializationStartup()
        {
            CheckProjectPrefabIDs();

            // Check already open scenes if valid
            int sceneCount = SceneManager.sceneCount;
            for (int i = 0; i < sceneCount; ++i)
            {
                var scene = SceneManager.GetSceneAt(i);
                CheckOpenScene(scene);
            }

            // Add event handler to check scene contents
            EditorSceneManager.sceneOpened += (scene, mode) =>
            {
                CheckOpenScene(scene);
            };

            // Add event handler to check prefab stage
            PrefabStage.prefabStageOpened += (stage) =>
            {
                var root = stage.prefabContentsRoot;
                var nsgo = root.GetComponent<NeoSerializedGameObject>();
                if (nsgo != null)
                {
                    nsgo.EditorCheckHierarchy();
                    nsgo.EditorCheckPrefabID();
                }
            };
        }

        [MenuItem("Tools/NeoFPS/Save System/Check Project Prefabs", priority = 20)]
        static void CheckProjectPrefabsMenuEntry()
        {
            CheckProjectPrefabIDs();
        }
    }
}

#endif
