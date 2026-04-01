#if UNITY_EDITOR

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace NeoSaveGames.Serialization
{
    public class NeoSerializationScenePreprocessor : IProcessSceneWithReport
    {
        public int callbackOrder { get { return 0; } }

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            // Check the scene hierarchy, etc
            NeoSerializationEditorUtilities.CheckOpenScene(scene);
        }
    }
}

#endif