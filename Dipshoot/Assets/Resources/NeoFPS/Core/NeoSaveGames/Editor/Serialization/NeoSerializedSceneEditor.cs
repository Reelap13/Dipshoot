#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using NeoSaveGames.Serialization;

namespace NeoSaveGames.Serialization
{
    [CustomEditor(typeof(NeoSerializedScene), true)]
    public class NeoSerializedSceneEditor : NeoSerializedSceneEditorBase
    {
        public sealed override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            GUI.enabled = false;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
            GUI.enabled = true;

            if (PrefabUtility.IsPartOfAnyPrefab(target))
                EditorGUILayout.HelpBox("It looks like this object is a prefab. Be aware that SceneSaveInfo and AdditiveSceneSaveInfo behaviours are scene specific and should not be shared across multiple scenes.", MessageType.Warning);

            OnInspectorGUITop();
            InspectRecreatableItems(true, true);
            OnInspectorGUIBottom();

            EditorGUILayout.Space();

            NeoSerializationEditorUtilities.OnShowNestedObjects(serializedObject);

            serializedObject.ApplyModifiedProperties();
        }

        public virtual void OnInspectorGUITop()
        {
        }

        public virtual void OnInspectorGUIBottom()
        {
            var prop = serializedObject.FindProperty("m_Assets");
            while (prop.NextVisible(false))
                EditorGUILayout.PropertyField(prop, true);
        }
    }
}

#endif