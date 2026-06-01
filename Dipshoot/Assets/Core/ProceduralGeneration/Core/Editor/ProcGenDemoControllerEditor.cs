using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.ProcGen.Editor
{
    [CustomEditor(typeof(ProcGenDemoController))]
    public sealed class ProcGenDemoControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Generate"))
                    Execute(controller => controller.Generate());

                if (GUILayout.Button("Clear"))
                    Execute(controller => controller.ClearGenerated());
            }
        }

        private void Execute(System.Action<ProcGenDemoController> action)
        {
            foreach (Object targetObject in targets)
            {
                if (targetObject is not ProcGenDemoController controller)
                    continue;

                action(controller);
                EditorUtility.SetDirty(controller);
                EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            }
        }
    }
}
