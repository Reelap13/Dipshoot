#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NeoSaveGames.Serialization
{
    public sealed partial class NeoSerializedGameObject : MonoBehaviour
    {
        public abstract partial class NeoSerializedGameObjectContainer : INeoSerializedGameObjectContainer
        {
            private static List<NeoSerializedGameObject> s_CachedObjects = new List<NeoSerializedGameObject>();

            bool runtime
            {
                get { return Application.isPlaying; }
            }

            public void OnValidate()
            {
                ValidateKeyAndValueArrays();
            }

            public void Initialise()
            {
                if (runtime)
                    Initialise_();
                else
                    EditorCheckOnAwake();
            }

            public void OnDestroy()
            {
                if (runtime)
                    OnDestroy_();
            }

            public bool Contains(NeoSerializedGameObject nsgo)
            {
                if (runtime)
                    return Contains_(nsgo);

                if (m_Values != null)
                    return m_Values.Contains(nsgo);

                return false;
            }

            public void RegisterObject(NeoSerializedGameObject nsgo, int suggestedKey = 0)
            {
                if (runtime)
                    RegisterObject_(nsgo, suggestedKey);
                else
                {
                    if (m_Keys != null && m_Values != null)
                    {
                        int index = m_Values.IndexOf(nsgo);
                        if (index == -1)
                        {
                            Undo.RecordObject(owner as MonoBehaviour, "Registering nested NSGO");

                            var key = suggestedKey;
                            if (key == 0)
                            {
                                key = GenerateKey();
                                nsgo.serializationKey = key;
                            }

                            m_Keys.Add(key);
                            m_Values.Add(nsgo);

                            if (owner != null)
                                EditorUtility.SetDirty(owner as MonoBehaviour);
                        }
                        else
                            nsgo.serializationKey = m_Keys[index];
                    }
                }
            }

            public void UnregisterObject(NeoSerializedGameObject nsgo)
            {
                if (runtime)
                    UnregisterObject_(nsgo);
                else
                {
                    if (m_Keys != null && m_Values != null)
                    {
                        int index = m_Values.IndexOf(nsgo);
                        if (index != -1)
                        {
                            Undo.RecordObject(owner as MonoBehaviour, "Unregistering nested NSGO");

                            m_Keys.RemoveAt(index);
                            m_Values.RemoveAt(index);
                            if (owner != null)
                                EditorUtility.SetDirty(owner as MonoBehaviour);

                            nsgo.serializationKey = 0;
                        }
                    }
                }
            }

            public int GetSerializationKeyForObject(NeoSerializedGameObject nsgo)
            {
                if (runtime)
                    return GetSerializationKeyForObject_(nsgo);

                if (m_Keys == null || m_Values == null)
                {
                    m_Keys = new List<int>();
                    m_Values = new List<NeoSerializedGameObject>();
                    return 0;
                }
                else
                {
                    // Search serialized m_Values
                    int index = m_Values.IndexOf(nsgo);
                    if (index != -1)
                        return m_Keys[index];
                    else
                        return 0;
                }
            }

            private int GenerateKey()
            {
                if (runtime)
                    return GenerateKey_();
                else
                {
                    int result = 0;
                    while (result == 0 || m_Keys.Contains(result))
                        result = Random.Range(int.MinValue, int.MaxValue);
                    return result;
                }
            }

            #region VALIDATION

            void EditorCheckOnAwake()
            {
                bool edited = false;

                // Check for invalid references
                for (int i = m_Values.Count - 1; i >= 0; --i)
                {
                    if (m_Values[i] == null || !m_Values[i].GetParentContainer(out var parent, false) || parent != this)
                    {
                        m_Values.RemoveAt(i);
                        m_Keys.RemoveAt(i);
                    }
                }

                if (edited)
                    EditorUtility.SetDirty(owner as MonoBehaviour);
            }

            bool ValidateKeyAndValueArrays()
            {
                bool result = false;

                // Check m_Keys lengths
                if (m_Keys.Count != m_Values.Count)
                {
#if NEOFPS_SAVE_LOGGING
                Debug.LogError("[NeoSerializedGameObjectContainerBase.OnValidate(nsgo)] - Key and value count doesn't match. Trimming...");
#endif

                    // Trim m_Keys array
                    if (m_Keys.Count > m_Values.Count)
                        m_Keys.RemoveRange(m_Values.Count, m_Keys.Count - m_Values.Count);
                    else
                    {
                        // Trim m_Values array
                        m_Values.RemoveRange(m_Keys.Count, m_Values.Count - m_Keys.Count);
                        result = true;
                    }
                }

                // Check m_Values validity
                for (int i = m_Values.Count - 1; i >= 0; --i)
                {
                    if (m_Values[i] == null || m_Keys[i] == 0)
                    {
#if NEOFPS_SAVE_LOGGING
                    Debug.LogError("[NeoSerializedGameObjectContainerBase.OnValidate(nsgo)] - Removing invalid entry");
#endif
                        m_Values.RemoveAt(i);
                        m_Keys.RemoveAt(i);
                        result = true;
                    }
                }

                return result;
            }

            public bool EditorCheckHierarchy(List<NeoSerializedGameObject> gathered)
            {
                if (!isValid || Application.isPlaying)
                    return false;

                bool result = ValidateKeyAndValueArrays();

                // Record m_Values to check which are still valid
                if (s_CachedObjects == null)
                    s_CachedObjects = new List<NeoSerializedGameObject>(m_Values.Count);
                for (int i = 0; i < m_Values.Count; ++i)
                    s_CachedObjects.Add(m_Values[i]);

                // Gather scene objects
                for (int i = 0; i < gathered.Count; ++i)
                {
                    // If already known, remove from remaining list
                    if (m_Values.Contains(gathered[i]))
                        s_CachedObjects.Remove(gathered[i]);
                    else
                    {
#if NEOFPS_SAVE_LOGGING
                    Debug.LogError("[NeoSerializedGameObjectContainerBase.OnValidate(nsgo)] - Adding missing entry: " + gathered[i].gameObject.name);
#endif
                        // Else add to container
                        m_Values.Add(gathered[i]);
                        m_Keys.Add(GenerateKey());
                        result = true;
                    }
                }

                // Remove remaining objects that weren't found
                // They must have moved in the hierarchy
                for (int i = 0; i < s_CachedObjects.Count; ++i)
                {
#if NEOFPS_SAVE_LOGGING
                Debug.LogError("[NeoSerializedGameObjectContainerBase.OnValidate(nsgo)] - A NeoSerializedGameObject was registered with the wrong parent: " + s_StoredObjects[i].gameObject.name);
#endif
                    int index = m_Values.IndexOf(s_CachedObjects[i]);
                    if (index != -1)
                    {
                        m_Values.RemoveAt(index);
                        m_Keys.RemoveAt(index);
                        result = true;
                    }
                }

                // Check child object hierarchy
                foreach (var nsgo in m_Values)
                {
                    var childCheck = nsgo.EditorCheckHierarchy();
#if NEOFPS_SAVE_LOGGING
                Debug.LogError("[NeoSerializedGameObjectContainerBase.OnValidate(nsgo)] - Error found in child object: " + nsgo.gameObject.name);
#endif
                    result |= childCheck;
                }

                if (result)
                    EditorUtility.SetDirty(owner as MonoBehaviour);

                return result;
            }

            #endregion
        }
    }
}

#endif