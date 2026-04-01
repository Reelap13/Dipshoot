#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeoSaveGames.Serialization
{
    public abstract partial class NeoSerializedScene : MonoBehaviour
    {
        [HideInInspector] public bool expandNestedObjectList = false;

        private static List<GameObject> s_GatheredGameObjects = new List<GameObject>();
        private static List<NeoSerializedGameObject> s_GatheredNsgos = new List<NeoSerializedGameObject>();

        bool runtime
        {
            get { return Application.isPlaying; }
        }

        public void RegisterPrefab(NeoSerializedGameObject prefab)
        {
            if (Array.IndexOf(m_Prefabs, prefab) == -1)
            {
                var so = new UnityEditor.SerializedObject(this);
                var prop = so.FindProperty("m_Prefabs");
                ++prop.arraySize;
                prop = prop.GetArrayElementAtIndex(prop.arraySize - 1);
                prop.objectReferenceValue = prefab;
                so.ApplyModifiedProperties();
            }
        }

        void Awake()
        {
            if (runtime)
                Awake_();
        }

        void Start()
        {
            if (!runtime)
                EditorCheckOnAwake();
        }

        void OnDestroy()
        {
            if (runtime)
                OnDestroy_();
        }

        void EditorCheckOnAwake()
        {
            // Assign child container
            CheckNestedObjectsContainer();
            m_NestedObjects.Initialise();

            // Check object arrays
            if (s_GatheredGameObjects == null)
                s_GatheredGameObjects = new List<GameObject>();
            if (s_GatheredNsgos == null)
                s_GatheredNsgos = new List<NeoSerializedGameObject>();

            // Gather scene objects
            scene.GetRootGameObjects(s_GatheredGameObjects);
            for (int i = 0; i < s_GatheredGameObjects.Count; ++i)
            {
                var nsgo = s_GatheredGameObjects[i].GetComponent<NeoSerializedGameObject>();
                if (nsgo != null)
                    s_GatheredNsgos.Add(nsgo);
            }

            // Validate container
            if (m_NestedObjects.EditorCheckHierarchy(s_GatheredNsgos))
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);

            s_GatheredGameObjects.Clear();
            s_GatheredNsgos.Clear();
        }

        #region VALIDATION

        void OnValidate()
        {
            if (runtime)
                return;

            CheckNestedObjectsContainer();
            m_NestedObjects.OnValidate();

            //ValidatePrefabs();
            //ValidateAssets();
        }

        public void EditorCheckScene()
        {
            CheckNestedObjectsContainer();
            ValidatePrefabs();
            ValidateAssets();
        }

        void ValidatePrefabs()
        {
            int valid = 0;
            for (int i = 0; i < m_Prefabs.Length; ++i)
            {
                if (m_Prefabs[i] != null)
                    ++valid;
            }
            if (valid != m_Prefabs.Length)
            {
                var rebuilt = new NeoSerializedGameObject[valid];
                int itr = 0;
                for (int i = 0; i < m_Prefabs.Length; ++i)
                {
                    if (m_Prefabs[i] != null)
                    {
                        rebuilt[itr] = m_Prefabs[i];
                        ++itr;
                    }
                }
                m_Prefabs = rebuilt;
            }
        }

        void ValidateAssets()
        {
            int valid = 0;
            for (int i = 0; i < m_Assets.Length; ++i)
            {
                if (m_Assets[i] != null)
                    ++valid;
            }
            if (valid != m_Assets.Length)
            {
                var rebuilt = new ScriptableObject[valid];
                int itr = 0;
                for (int i = 0; i < m_Assets.Length; ++i)
                {
                    if (m_Assets[i] != null)
                    {
                        rebuilt[itr] = m_Assets[i];
                        ++itr;
                    }
                }
                m_Assets = rebuilt;
            }
        }

        #endregion
    }
}

#endif