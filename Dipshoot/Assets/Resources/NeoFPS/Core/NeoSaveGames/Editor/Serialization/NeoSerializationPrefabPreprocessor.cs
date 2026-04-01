#if UNITY_EDITOR

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace NeoSaveGames.Serialization
{
    public class NeoSerializationPrefabPreprocessor : IPreprocessBuildWithReport
    { 
        public void OnPreprocessBuild(BuildReport report)
        {
            // Check all the project prefabs have correct prefab IDs
            NeoSerializationEditorUtilities.CheckProjectPrefabIDs();
        }

        public int callbackOrder { get { return 0; } }
    }
}

#endif
