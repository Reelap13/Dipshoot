using System;
using UnityEngine;
using UnityEngine.Serialization;


// NOTE: Partial class is split into multiple files...
// - This script file has the main & runtime functionality for managing NSGO hierarchies
// - NeoSerializedGameObject.Runtime.cs
//   - Hooks into the runtime unity events to trigger the relevant functionality
//   - This script is conditionally compiled for builds only
// - NeoSerializedGameObject.Editor.cs
//   - Set to execute always and hooks into the unity events, branching based on whether the application is playing
//   - This script is conditionally compiled for editor only
// - NeoSerializedGameObject.Override.cs
//   - Contains the override sub-class for changing save behaviour based on save mode
// - NeoSerializedGameObject.ReadWrite.cs
//   - Contains the code for saving and loading the gameobject and its hierarchy while playing
// - NeoSerializedGameObject.Obsolete.cs
//   - Contains methods and properties flagged as obsolete to prevent compilation issues with older projects

namespace NeoSaveGames.Serialization
{
    [DisallowMultipleComponent]
    [HelpURL("https://docs.neofps.com/manual/savegamesref-mb-neoserializedgameobject.html")]
    public sealed partial class NeoSerializedGameObject : MonoBehaviour, INeoSerializedSceneObject
    {
        [SerializeField, FormerlySerializedAs("m_Children")]//, HideInInspector]
        private ChildObjectContainer m_NestedObjects = null;

        private bool m_IgnoreReparenting = false;
        private Transform m_PreviousParent = null;
        private int m_SerializationKey = 0;

        public bool initialised
        {
            get;
            private set;
        }

        public bool registered
        {
            get;
            private set;
        }

        public bool instantiatedPrefabRoot
        {
            get;
            private set;
        }

        public bool wasRuntimeInstantiated
        {
            get;
            private set;
        }

        public bool wasCreatedByScript
        {
            get;
            private set;
        }

        public bool isDestroyed
        {
            get;
            private set;
        }

        public int serializationKey
        {
            get { return m_SerializationKey; }
            private set
            {
                if (m_SerializationKey == 0)
                    m_SerializationKey = value;
                else
                {
                    if (m_SerializationKey != value && value != 0)
                        Debug.LogError($"Cannot change an objects serialization key once it has been set. GameObject: {gameObject.name}, Old: {m_SerializationKey}, New: {value}", gameObject);
                    //if (m_SerializationKey != value && value == 0)
                    //    Debug.Log($"Resetting serialization key from {m_SerializationKey} to 0", gameObject);
                    m_SerializationKey = value;
                }
            }
        }

        public INeoSerializedGameObjectContainer nestedObjects
        {
            get
            {
                CheckNestedObjectsContainer();
                return m_NestedObjects;
            }
        }

        [Serializable]
        class ChildObjectContainer : NeoSerializedGameObjectContainer
        {
            [SerializeField]
            private NeoSerializedGameObject m_GameObject = null;

            public override INeoSerializedSceneObject owner
            {
                get { return m_GameObject; }
            }

            public override Transform rootTransform
            {
                get { return m_GameObject.transform; }
            }

            public override bool isValid
            {
                get { return m_GameObject != null; }
            }

            public ChildObjectContainer(NeoSerializedGameObject nsgo)
            {
                m_GameObject = nsgo;
            }
        }

        public void Initialise()
        {
            if (!initialised)
            {
                // Assign / initialise child container
                CheckNestedObjectsContainer();
                m_NestedObjects.Initialise();

                // Initialise children
                foreach (var child in m_NestedObjects)
                {
                    if (wasRuntimeInstantiated)
                        child.wasRuntimeInstantiated = true;
                    child.Initialise();
                }

                // Record initialised
                initialised = true;
            }
        }

        void OnDestroy_()
        {
            if (!SaveGameManager.enabled)
                return;

            // Skip this if the scene is being unloaded
            if (!gameObject.scene.isLoaded)
                return;

            isDestroyed = true;

            if (GetParentContainer(out var container, wasRuntimeInstantiated))
                container.UnregisterObject(this);
            m_SerializationKey = 0;

            // Destroy child NeoSerializedGameObjectContainer
            // Prevents children from unregistering and bloating the destroyed objects list
            if (m_NestedObjects != null)
                m_NestedObjects.OnDestroy();
        }

        void OnBeforeTransformParentChanged()
        {
            if (!SaveGameManager.enabled)
                return;

            m_PreviousParent = transform.parent;
        }

        void OnTransformParentChanged()
        {
            if (!SaveGameManager.enabled)
                return;

            if (!m_IgnoreReparenting && transform.parent != m_PreviousParent)
            {
                int key = serializationKey;

                if (GetParentContainer(m_PreviousParent, out var previousContainer, wasRuntimeInstantiated))
                    previousContainer.UnregisterObject(this);

                if (GetParentContainer(transform.parent, out var newContainer, wasRuntimeInstantiated))
                    newContainer.RegisterObject(this, key);
            }
            m_PreviousParent = null;
        }

        public NeoSerializedGameObject GetParent()
        {
            var parent = transform.parent;
            if (parent != null)
#if UNITY_2021_2_OR_NEWER
                return parent.GetComponentInParent<NeoSerializedGameObject>(true);
#else
                return NeoSerializationUtilities.GetComponentInParentAllowInactive<NeoSerializedGameObject>(parent);
#endif
            else
                return null;
        }

        private NeoSerializedScene GetScene_()
        {
            return NeoSerializedScene.GetByPath(gameObject.scene.path);
        }

        public bool GetParentContainer(out INeoSerializedGameObjectContainer container, bool directTransformParentOnly)
        {
            return GetParentContainer(transform.parent, out container, directTransformParentOnly);
        }

        public bool GetParentContainer(Transform parentTransform, out INeoSerializedGameObjectContainer container, bool directTransformParentOnly)
        {
            if (parentTransform != null)
            {
                // Get parent NSGO
                NeoSerializedGameObject parentNSGO = (directTransformParentOnly) ?
                    parentTransform.GetComponent<NeoSerializedGameObject>() :
#if UNITY_2021_2_OR_NEWER
                    parentTransform.GetComponentInParent<NeoSerializedGameObject>(true);
#else
                    NeoSerializationUtilities.GetComponentInParentAllowInactive<NeoSerializedGameObject>(parentTransform);
#endif

                if (parentNSGO != null && !parentNSGO.isDestroyed)
                    container = parentNSGO.nestedObjects;
                else
                    container = null;
            }
            else
            {
                var scene = GetScene();
                if (scene != null && !scene.isDestroyed)
                    container = scene.nestedObjects;
                else
                    container = null;
            }

            return container != null;
        }

        public bool CheckWillBeSerialized()
        {
            if (transform.parent == null)
                return true;

            if (instantiatedPrefabRoot)
            {
                var parentNsgo = transform.parent.GetComponent<NeoSerializedGameObject>();
                if (parentNsgo == null || !parentNsgo.CheckWillBeSerialized())
                    return false;
            }

            var parent = GetParent();
            if (parent == null)
                return false;

            if (!parent.CheckWillBeSerialized())
                return false;

            return parent.WillSerializeChildObject(this);
        }

        bool CheckNestedObjectsContainer()
        {
            if (m_NestedObjects == null || !m_NestedObjects.isValid)
            {
#if NEOFPS_SAVE_LOGGING
                if (m_NestedObjects != null && !m_NestedObjects.isValid)
                    Debug.Log("Replacing invalid but non-null nested objects container");
#endif
                m_NestedObjects = new ChildObjectContainer(this);

                return true;
            }
            return false;
        }

        public NeoSerializedGameObject CreateNewChildObject(string name, int key = 0)
        {
            // Create object and add NeoSerializedGameObject
            var go = new GameObject(name);

            // Position, etc
            var t = go.transform;
            t.SetParent(transform);
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;

            var result = go.AddComponent<NeoSerializedGameObject>();
            result.wasCreatedByScript = true;
            result.wasRuntimeInstantiated = true;
            result.Initialise();
            nestedObjects.RegisterObject(result, key);

            return result;
        }
    }
}
