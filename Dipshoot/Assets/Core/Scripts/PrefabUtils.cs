using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
public static class PrefabUtils
{
    public static void SaveInProject(ScriptableObject scriptable_object)
    {
#if UNITY_EDITOR
        EditorUtility.SetDirty(scriptable_object);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
#endif
    }
}
