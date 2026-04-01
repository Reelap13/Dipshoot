#define NEOFPS_SAVE_LOGGING

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

// NOTE: Partial class is split into multiple files...
// - This script file has the runtime functionality for managing nested NSGO hierarchies
//   - This class is nested inside NeoSerializedGameObject so that it can access its private members without needing to expose them publically
// - NeoSerializedGameObjectContainer.Runtime.cs
//   - Exposes public methods to trigger the relevant functionality
//   - This script is conditionally compiled for builds only
// - NeoSerializedGameObjectContainer.Editor.cs
//   - Exposes public methods to trigger the relevant functionality, branching based on whether the application is playing
//   - This script is conditionally compiled for editor only

namespace NeoSaveGames.Serialization
{
    public sealed partial class NeoSerializedGameObject : MonoBehaviour
    {
        [Serializable]
        public abstract partial class NeoSerializedGameObjectContainer : INeoSerializedGameObjectContainer
        {
            [SerializeField]
            private List<int> m_Keys = new List<int>();
            [SerializeField]
            private List<NeoSerializedGameObject> m_Values = new List<NeoSerializedGameObject>();

            private static readonly NeoSerializationKey k_DestroyedKey = new NeoSerializationKey("destroyedObjects");
            private static readonly NeoSerializationKey k_RuntimeKey = new NeoSerializationKey("runtimeObjects");

            private Dictionary<int, NeoSerializedGameObject> m_TrackedObjects = null;// new Dictionary<int, NeoSerializedGameObject>();
            private List<int> m_DestroyedObjects = new List<int>();

            public abstract INeoSerializedSceneObject owner
            {
                get;
            }

            public abstract Transform rootTransform
            {
                get;
            }

            public abstract bool isValid
            {
                get;
            }

            public int count
            {
                get
                {
                    if (m_TrackedObjects != null)
                        return m_TrackedObjects.Count;
                    if (m_Keys != null)
                        return m_Keys.Count;
                    return 0;
                }
            }

            protected bool isBuildingHierarchy
            {
                get;
                private set;
            }

            public IEnumerator<NeoSerializedGameObject> GetEnumerator()
            {
                if (m_TrackedObjects != null)
                    return m_TrackedObjects.Values.GetEnumerator();

                if (m_Values != null)
                    return m_Values.GetEnumerator();

                return null;
            }

            private IEnumerator GetEnumerator_()
            {
                return this.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator_();
            }

            public NeoSerializedGameObject this[int key]
            {
                get
                {
                    NeoSerializedGameObject result;
                    m_TrackedObjects.TryGetValue(key, out result);
                    return result;
                }
            }

            protected NeoSerializedGameObjectContainer()
            {
                if (m_Keys == null)
                    m_Keys = new List<int>();
                if (m_Values == null)
                    m_Values = new List<NeoSerializedGameObject>();
            }

            protected void Initialise_()
            {
                if (m_TrackedObjects == null && isValid)
                    RebuildDictionary();
            }

            protected void OnDestroy_()
            {
                m_TrackedObjects = null;
                m_DestroyedObjects = null;
            }

            protected bool Contains_(NeoSerializedGameObject nsgo)
            {
                return (m_TrackedObjects.TryGetValue(nsgo.serializationKey, out var found) && found == nsgo);
            }

            void RebuildDictionary()
            {
                // Create and populate tracked objects dictionary
                int count = Math.Min(m_Keys.Count, m_Values.Count);
                m_TrackedObjects = new Dictionary<int, NeoSerializedGameObject>(count);
                for (int i = 0; i != count; i++)
                {
                    var nsgo = m_Values[i];
                    if (nsgo != null)
                    {
                        m_TrackedObjects.Add(m_Keys[i], nsgo);
                        nsgo.serializationKey = m_Keys[i];
                        nsgo.registered = true;
                    }
                }

                // Clear the old key & values lists as no longer needed
                m_Keys = null;
                m_Values = null;
            }

            protected int GenerateKey_()
            {
                int key = 0;
                while (key == 0 || m_TrackedObjects.ContainsKey(key))
                    key = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
                return key;
            }

            protected virtual int GetSerializationKeyForObject_(NeoSerializedGameObject nsgo)
            {
                foreach (var pair in m_TrackedObjects)
                {
                    if (pair.Value == nsgo)
                        return pair.Key;
                }

                return 0;
            }

            protected virtual void RegisterObject_(NeoSerializedGameObject nsgo, int suggestedKey)
            {
                Initialise_();

                // Check if child object
                if (nsgo.transform.parent != rootTransform)
                {
                    Debug.LogError("Attempting to register runtime object that is not a direct child of the container object.", nsgo.gameObject);
                    return;
                }

                if (nsgo.gameObject.name == "LocalWeapon")
                    Debug.LogWarning("Registering offset", nsgo.gameObject);

                // Use existing serialization key (if instantiated on load) or generate a new one
                int key = suggestedKey;
                if (key == 0)
                    key = GenerateKey();
                else
                {
                    if (m_TrackedObjects.TryGetValue(key, out var existing))
                    {
                        if (existing == nsgo)
                            Debug.LogError($"Attempting to register object with its parent NSGO twice: {nsgo.name}. key: {key}", nsgo.gameObject);
                        else
                            Debug.LogError($"Attempting to register object with key [{key}] that is already registered. This object will not be tracked: {nsgo.name}. Existing object: {existing.name}", nsgo.gameObject);
                        return;
                    }
                }

                m_TrackedObjects.Add(key, nsgo);
                nsgo.serializationKey = key;
                nsgo.registered = true;
            }

            protected virtual void UnregisterObject_(NeoSerializedGameObject nsgo)
            {
                if (m_TrackedObjects == null)
                    return;

                NeoSerializedGameObject found = null;
                if (m_TrackedObjects.TryGetValue(nsgo.serializationKey, out found) && found == nsgo)
                {
                    m_TrackedObjects.Remove(nsgo.serializationKey);
                    nsgo.registered = false;

                    // Add to destroyed list
                    if (!nsgo.wasRuntimeInstantiated)
                    {
                        if (m_DestroyedObjects == null)
                            m_DestroyedObjects = new List<int>();
                        m_DestroyedObjects.Add(nsgo.serializationKey);
                    }
                }
            }

            public NeoSerializedGameObject GetChildObject(int serializationKey)
            {
                NeoSerializedGameObject result;
                if (m_TrackedObjects.TryGetValue(serializationKey, out result))
                    return result;
                else
                    return null;
            }

            public void WriteGameObjects(INeoSerializer writer, SaveMode saveMode)
            {
                WriteGameObjects(writer, NeoSerializationFilter.Exclude, null, saveMode);
            }

            public void WriteGameObjects(INeoSerializer writer, NeoSerializationFilter filter, NeoSerializedGameObject[] objects, SaveMode saveMode)
            {
                // Check if awake
                Initialise_();

                // Write list of destroyed objects (non-runtime initialised)
                if (m_DestroyedObjects != null && m_DestroyedObjects.Count > 0)
                    writer.WriteValues(k_DestroyedKey, m_DestroyedObjects);

                if (m_TrackedObjects.Count > 0)
                {
                    // Runtime initialised prefabs & objects
                    var runtime = new List<Vector2Int>(m_TrackedObjects.Count);
                    var nsgos = new List<NeoSerializedGameObject>(m_TrackedObjects.Count);

                    // Populate
                    foreach (var nsgo in m_TrackedObjects.Values)
                    {
                        bool serializeObject = false;
                        if (filter == NeoSerializationFilter.Include)
                        {
                            // Add if in objects list
                            if (objects != null && Array.IndexOf(objects, nsgo) != -1)
                                serializeObject = true;
                        }
                        else
                        {
                            // Add if not in objects list
                            if (objects == null || Array.IndexOf(objects, nsgo) == -1)
                                serializeObject = true;
                        }

                        if (serializeObject)
                        {
                            nsgos.Add(nsgo);
                            if (nsgo.instantiatedPrefabRoot || nsgo.wasCreatedByScript)
                                runtime.Add(new Vector2Int(nsgo.prefabStrongID, nsgo.serializationKey));
                        }
                    }

                    // write list of runtime initialised prefabs
                    if (runtime.Count > 0)
                        writer.WriteValues(k_RuntimeKey, runtime);

                    // Write all tracked gameobjects
                    for (int i = 0; i < nsgos.Count; ++i)
                    {
                        if (nsgos[i] != null)
                        {
                            writer.PushContext(SerializationContext.GameObject, nsgos[i].serializationKey);
                            nsgos[i].WriteGameObject(writer, saveMode);
                            writer.PopContext(SerializationContext.GameObject);
                        }
                    }
                }
            }

            public void ReadGameObjectHierarchy(INeoDeserializer reader)
            {
                // Check if awake
                Initialise_();

                isBuildingHierarchy = true;

                // Should check non-dynamic children are already registered??

                // Destroy any non-runtime instantiated objects that were removed
                int[] destroyed;
                if (reader.TryReadValues(k_DestroyedKey, out destroyed, null) && destroyed.Length > 0)
                {
                    // Track the destroyed objects for next save/load
                    m_DestroyedObjects = new List<int>();
                    m_DestroyedObjects.AddRange(destroyed);

                    // Actually destroy the objects
                    for (int i = 0; i < destroyed.Length; ++i)
                    {
                        NeoSerializedGameObject destroy;
                        if (m_TrackedObjects.TryGetValue(destroyed[i], out destroy))
                            UnityEngine.Object.Destroy(destroy.gameObject);
                    }
                }

                // Instantiate runtime objects
                Vector2Int[] runtime;
                if (reader.TryReadValues(k_RuntimeKey, out runtime, null))
                {
                    for (int i = 0; i < runtime.Length; ++i)
                    {
                        if (runtime[i].x == 0)
                        {
                            // Create object and add NeoSerializedGameObject
                            var go = new GameObject("Name Pending");

                            // Position, etc
                            var t = go.transform;
                            t.SetParent(rootTransform);
                            t.localPosition = Vector3.zero;
                            t.localRotation = Quaternion.identity;
                            t.localScale = Vector3.one;

                            var result = go.AddComponent<NeoSerializedGameObject>();
                            //result.serializationKey = runtime[i].y;
                            result.wasCreatedByScript = true;
                            result.wasRuntimeInstantiated = true;
                            result.Initialise();
                            RegisterObject_(result, runtime[i].y);
                        }
                        else
                        {
#if NEOFPS_SAVE_LOGGING
                            try
                            {
                                var result = NeoPrefabFactory.InstantiateFromID(runtime[i].x, runtime[i].y, owner as MonoBehaviour, rootTransform);
                            }
                            catch (Exception e)
                            {
                                Debug.LogError($"Failed to instantiate runtime object with prefab ID {runtime[i].x} and serialization key {runtime[i].y} due to error: {e.Message}");
                            }
#else
                            var result = NeoPrefabFactory.InstantiateFromID(runtime[i].x, runtime[i].y, owner as MonoBehaviour);
#endif
                        }
                    }
                }

                // Read all tracked objects
                foreach (var nsgo in m_TrackedObjects.Values)
                {
                    if (reader.PushContext(SerializationContext.GameObject, nsgo.serializationKey))
                    {
                        nsgo.ReadGameObjectHierarchy(reader);
                        reader.PopContext(SerializationContext.GameObject, nsgo.serializationKey);
                    }
                }

                isBuildingHierarchy = false;
            }

            public void ReadGameObjectProperties(INeoDeserializer reader)
            {
                // Read all tracked objects
                foreach (var nsgo in m_TrackedObjects.Values)
                {
                    if (reader.PushContext(SerializationContext.GameObject, nsgo.serializationKey))
                    {
                        nsgo.ReadGameObjectProperties(reader);
                        reader.PopContext(SerializationContext.GameObject, nsgo.serializationKey);
                    }
                }
            }
        }
    }
}