#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
#if UNITY_2021_2_OR_NEWER
using UnityEditor.SceneManagement;
#else
using UnityEditor.Experimental.SceneManagement;
#endif
using UnityEngine;

namespace NeoSaveGames.Serialization
{
    [ExecuteAlways]
    public sealed partial class NeoSerializedGameObject : MonoBehaviour
    {
        // Inspector foldout expansion tracking
        [HideInInspector] public bool expandChildObjects = false;
        [HideInInspector] public bool expandNeoComponents = false;
        [HideInInspector] public bool expandOtherComponents = false;
        [HideInInspector] public bool expandNestedObjectList = false;

        // Guid tracking (if it doesn't match the asset file's then update and use to generate a new prefabID)
        [HideInInspector] public string prefabGuid = string.Empty;

        private static List<NeoSerializedGameObject> s_GatheredNsgos = new List<NeoSerializedGameObject>();
        private static List<INeoSerializedGameObjectLimiter> s_Limiters = new List<INeoSerializedGameObjectLimiter>();
        private static string s_CurrentScenePath = string.Empty;
        private static NeoSerializedScene s_CurrentScene = null;

        bool runtime
        {
            get { return Application.isPlaying; }
        }

        void Start()
        {
            if (!runtime)
            {
                if (CheckNestedObjectsContainer())
                    Undo.RecordObject(this, "Reset nested objects container");

                bool checkChildren = true;

                // Register with parent on placing prefab in scene or adding NSGO behaviour to object
                if (GetParentContainer(out var container, false))
                {
                    if (container.Contains(this))
                    {
                        // Check the serialization key has been correctly assigned
                        if (m_SerializationKey == 0)
                            m_SerializationKey = container.GetSerializationKeyForObject(this);
                    }
                    else
                    {
                        // Check if this is a new NSGO inserted between 2 others
                        if (nestedObjects.count == 0 && container.owner is NeoSerializedGameObject && container.count > 0)
                        {
                            checkChildren = false;

                            // Get children
                            GetComponentsInChildren(true, s_GatheredNsgos);
                            if (s_GatheredNsgos.Count > 0)
                            {
                                // Create a hash set of children for fast checking
                                var nestedSet = new HashSet<NeoSerializedGameObject>(s_GatheredNsgos);

                                s_GatheredNsgos.Clear();

                                // Gather any nested from parent that are chidren of this
                                foreach (var nested in container)
                                {
                                    if (nestedSet.Contains(nested))
                                        s_GatheredNsgos.Add(nested);
                                }

                                // Transfer gathered object from parent to this
                                foreach (var nested in s_GatheredNsgos)
                                {
                                    Debug.Log($"Transferred object {nested.gameObject.name} from {container.owner.gameObject.name} to {gameObject.name}");
                                    container.UnregisterObject(nested);
                                    nestedObjects.RegisterObject(nested);
                                }

                                s_GatheredNsgos.Clear();
                            }
                        }

                        // Register with parent
                        container.RegisterObject(this);
                    }
                }

                if (checkChildren)
                {
                    // Get children
                    GetComponentsInChildren(true, s_GatheredNsgos);
                    foreach (var gathered in s_GatheredNsgos)
                    {
                        // Register direct children
                        if (gathered.GetParent() == this)
                            nestedObjects.RegisterObject(gathered);
                    }
                    s_GatheredNsgos.Clear();
                }
            }
        }

        void OnDestroy()
        {
            if (runtime)
                OnDestroy_();
            else
            {
                // Skip this if the scene is being unloaded
                if (!gameObject.scene.isLoaded)
                    return;

                isDestroyed = true;

                // Need to do extra funkiness compared to runtime to check if OnDestroy() is called as a result
                // of the behaviour being removed (children should be assigned to parent) or the object it's on destroyed
                if (GetParentContainer(out var container, false))
                {
                    container.UnregisterObject(this);
                    m_SerializationKey = 0;

                    if (container.owner is NeoSerializedGameObject nsgo)
                    {
                        // Transfer children to parent, in case it's a situation where the NSGO
                        // behaviour was removed instead of object destroyed
                        foreach (var nested in nestedObjects)
                            container.RegisterObject(nested);
                    }
                }
                else
                {
                    // Check if there's a parent container that's been destroyed
                    var parent = GetParent();
                    if (parent != null)
                    {
                        // Now check the grandparent doesn't contain this, etc
                        if (parent.GetParentContainer(out var parentContainer, false) && parentContainer.Contains(this))
                            parentContainer.UnregisterObject(this);
                    }
                }
            }
        }

        public NeoSerializedScene GetScene()
        {
            if (runtime)
                return GetScene_();
            else
            {
                var scene = gameObject.scene;
                if (scene.path == s_CurrentScenePath)
                    return s_CurrentScene;
                else
                {
                    if (scene.isLoaded)
                    {
                        var rootObjects = scene.GetRootGameObjects();
                        foreach (var obj in rootObjects)
                        {
                            if (obj.TryGetComponent(out s_CurrentScene))
                            {
                                s_CurrentScenePath = scene.path;
                                return s_CurrentScene;
                            }
                        }
                    }

                    s_CurrentScenePath = string.Empty;
                    s_CurrentScene = null;

                    return null;
                }
            }
        }

        #region VALIDATION

        public void OnValidate()
        {
            if (runtime)
                return;

            // Assign child container
            if (CheckNestedObjectsContainer())
                EditorUtility.SetDirty(this);
            // We can't validate the container here or else it will break undo/redo by clearing
            // the null value before the object has been restored and then treating it as new

            // Check for restrictions
            if (s_Limiters == null)
                s_Limiters = new List<INeoSerializedGameObjectLimiter>();
            GetComponents(s_Limiters);

            for (int i = 0; i < s_Limiters.Count; ++i)
            {
                // Restrict child objects
                if (s_Limiters[i].restrictChildObjects)
                {
                    m_FilterChildObjects = NeoSerializationFilter.Include;
                    if (m_ChildObjects.Length > 0)
                        m_ChildObjects = new NeoSerializedGameObject[0];
                }

                // Restrict neo components
                if (s_Limiters[i].restrictNeoComponents)
                {
                    m_FilterNeoComponents = NeoSerializationFilter.Include;
                    if (m_NeoComponents.Length > 0)
                        m_NeoComponents = new MonoBehaviour[0];
                }

                // Restrict non-neo components
                if (s_Limiters[i].restrictOtherComponents)
                {
                    if (m_OtherComponents.Length > 0)
                        m_OtherComponents = new Component[0];
                }

                for (int j = 0; j < m_Overrides.Length; ++j)
                    m_Overrides[j].ApplyLimiter(s_Limiters[i]);
            }

            s_Limiters.Clear();
        }

        public void EditorCheckPrefabID()
        {
            string path = null;
            bool found = false;

            // Check if this NSGO is part of a nested prefab, and skip if so.
            // Nested prefabs need to use PrefabUtility to get the prefab assed path,
            // and all utility methods will skip (and therefore break) variant prefabs.
            // There might be a way to do this, but I couldn't find it and I wasted a lot of time looking
            var root = transform.root;
            var itr = transform;
            while (itr != root)
            {
                if (PrefabUtility.IsAnyPrefabInstanceRoot(gameObject))
                {
                    found = true;
                    break;
                }

                itr = itr.parent;
            }

            // If not a nested prefab. Get the prefab path
            if (!found)
            {
                var p = PrefabStageUtility.GetCurrentPrefabStage();
                if (p != null && p.IsPartOfPrefabContents(gameObject))
#if UNITY_2021_2_OR_NEWER
                    path = p.assetPath;
#else
                    path = p.prefabAssetPath;
#endif
                else
                    path = AssetDatabase.GetAssetPath(gameObject);
            }

            // Check if is prefab root
            if (transform == root && !m_IsPrefabRoot)
            {
                m_IsPrefabRoot = true;
                EditorUtility.SetDirty(this);
            }

            // If a valid prefab path was found
            if (!string.IsNullOrEmpty(path))
            {
                // Get the GUID from path and check against recorded
                string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(gameObject));// prefabGameobject));
                if (!string.IsNullOrEmpty(guid) && prefabGuid != guid)
                {
                    // Update the prefab GUID and ID
                    prefabGuid = guid;
                    m_PrefabStrongID = NeoSerializationUtilities.StringToHash(guid);
                    EditorUtility.SetDirty(this);
                }
            }
        }

        public bool EditorCheckHierarchy()
        {
            bool result = CheckNestedObjectsContainer();

            // Get all child NSGOs
            GetComponentsInChildren(true, s_GatheredNsgos);

            // Filter for invalid objects
            for (int i = s_GatheredNsgos.Count - 1; i >= 0; --i)
            {
                if (s_GatheredNsgos[i].GetParent() != this)
                    s_GatheredNsgos.RemoveAt(i);
            }

            // Assign child container if required and validate
            result |= m_NestedObjects.EditorCheckHierarchy(s_GatheredNsgos);
            if (result)
                EditorUtility.SetDirty(this);

            s_GatheredNsgos.Clear();

            return result;
        }

#endregion
    }
}

#endif