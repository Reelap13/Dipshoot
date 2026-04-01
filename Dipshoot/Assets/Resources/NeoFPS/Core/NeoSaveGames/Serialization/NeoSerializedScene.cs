using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

// NOTE: Partial class is split into multiple files...
// - This script file has the main & runtime functionality for managing the scene and NSGO hierarchies
// - NeoSerializedScene.Runtime.cs
//   - Hooks into the runtime unity events to trigger the relevant functionality
//   - This script is conditionally compiled for builds only
// - NeoSerializedScene.Editor.cs
//   - Set to execute always and hooks into the unity events, branching based on whether the application is playing
//   - This script is conditionally compiled for editor only
// - NeoSerializedGameObject.RegisteredObject.cs
//   - Contains the code for registering prefabs and assets with the scene for runtime instantiation
// - NeoSerializedGameObject.Obsolete.cs
//   - Contains methods and properties flagged as obsolete to prevent compilation issues with older projects

namespace NeoSaveGames.Serialization
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public abstract partial class NeoSerializedScene : MonoBehaviour, INeoSerializedSceneObject
    {
        [SerializeField, FormerlySerializedAs("m_SceneObjects"), HideInInspector]
        private SceneObjectContainer m_NestedObjects = null;

        private static List<NeoSerializedScene> s_ActiveScenes = new List<NeoSerializedScene>();

        private Scene m_Scene = new Scene();
        private int m_HashedPath = 0;
        private bool m_Initialised = false;

        public static bool initialisingSceneHierarchy
        {
            get;
            private set;
        }

        public bool isDestroyed
        {
            get;
            private set;
        } = false;

        public Scene scene
        {
            get
            {
                if (!m_Scene.IsValid())
                    m_Scene = gameObject.scene;
                return m_Scene;
            }
        }

        public int hashedPath
        {
            get
            {
                if (m_HashedPath == 0)
                    m_HashedPath = NeoSerializationUtilities.StringToHash(scene.path);
                return m_HashedPath;
            }
        }

        public abstract bool isMainScene
        {
            get;
        }

        public INeoSerializedGameObjectContainer nestedObjects
        {
            get
            {
                CheckNestedObjectsContainer();
                return m_NestedObjects;
            }
        }

        bool CheckNestedObjectsContainer()
        {
            if (m_NestedObjects == null || !m_NestedObjects.isValid)
            {
                if (m_NestedObjects != null && !m_NestedObjects.isValid)
                    Debug.Log("Replacing scene nested objects container");

                m_NestedObjects = new SceneObjectContainer(this);

                return true;
            }
            return false;
        }

        [Serializable]
        class SceneObjectContainer : NeoSerializedGameObject.NeoSerializedGameObjectContainer
        {
            [SerializeField]
            private NeoSerializedScene m_Scene = null;

            public override INeoSerializedSceneObject owner
            {
                get { return m_Scene; }
            }

            public override Transform rootTransform
            {
                get { return null; }
            }

            public override bool isValid
            {
                get { return m_Scene != null; }
            }

            public NeoSerializedScene serializedScene
            {
                get { return m_Scene; }
            }

            public SceneObjectContainer(NeoSerializedScene scene)
            {
                m_Scene = scene;
            }

            protected override void RegisterObject_(NeoSerializedGameObject nsgo, int suggestedKey)
            {
                base.RegisterObject_(nsgo, suggestedKey);

                if (isBuildingHierarchy)
                    SceneManager.MoveGameObjectToScene(nsgo.gameObject, m_Scene.scene);
                // Set current scene in ReadGameObjectHierarchy???
                // Then can stop these from being virtual
            }
        }

        protected virtual void Awake_()
        {
            if (!SaveGameManager.enabled)
                return;

            if (s_ActiveScenes == null)
                s_ActiveScenes = new List<NeoSerializedScene>();
            s_ActiveScenes.Add(this);

            // Initialise hierarchy
            Initialise();

            // Notify the save manager that the scene is loaded
            SaveGameManager.NotifySceneLoaded(this);
        }

        public void Initialise()
        {
            if (!m_Initialised)
            {
                initialisingSceneHierarchy = true;

                // Assign child container
                CheckNestedObjectsContainer();
                m_NestedObjects.Initialise();

                // Initialise hierarchy
                foreach (var nsgo in m_NestedObjects)
                    nsgo.Initialise();

                m_Initialised = true;

                initialisingSceneHierarchy = false;
            }
        }

        protected virtual void OnDestroy_()
        {
            if (!SaveGameManager.enabled)
                return;

            if (s_ActiveScenes != null)
                s_ActiveScenes.Remove(this);

            if (m_NestedObjects != null)
                m_NestedObjects.OnDestroy();

            SaveGameManager.UnregisterScene(this);
        }

        public static NeoSerializedScene GetByScene(Scene scene)
        {
            return GetByPath(scene.path);
        }

        public static NeoSerializedScene GetByBuildIndex(int buildIndex)
        {
            for (int i = 0; i < s_ActiveScenes.Count; ++i)
            {
                if (s_ActiveScenes[i].scene.buildIndex == buildIndex)
                    return s_ActiveScenes[i];
            }
            return null;
        }

        public static NeoSerializedScene GetByName(string name)
        {
            for (int i = 0; i < s_ActiveScenes.Count; ++i)
            {
                if (s_ActiveScenes[i].scene.name == name)
                    return s_ActiveScenes[i];
            }
            return null;
        }

        public static NeoSerializedScene GetByPath(string path)
        {
            for (int i = 0; i < s_ActiveScenes.Count; ++i)
            {
                if (s_ActiveScenes[i].scene.path == path)
                    return s_ActiveScenes[i];
            }
            return null;
        }

        public static NeoSerializedScene GetByPathHash(int hash)
        {
            for (int i = 0; i < s_ActiveScenes.Count; ++i)
            {
                if (s_ActiveScenes[i].hashedPath == hash)
                    return s_ActiveScenes[i];
            }
            return null;
        }

        #region SERIALIZATION

        public void WriteData(INeoSerializer writer)
        {
            WriteData(writer, SaveMode.Default);
        }

        public void WriteData(INeoSerializer writer, SaveMode saveMode)
        {
            if (!scene.IsValid())
                return;

            PreSaveScene();

            m_NestedObjects.WriteGameObjects(writer, saveMode);
            WriteProperties(writer);

            PostSaveScene();
        }

        public void ReadData(INeoDeserializer reader)
        {
            Initialise();

            PreLoadScene();
            
            m_NestedObjects.ReadGameObjectHierarchy(reader);
            m_NestedObjects.ReadGameObjectProperties(reader);

            ReadProperties(reader);

            PostLoadScene();
        }

        protected virtual void PreSaveScene() { }
        protected virtual void PostSaveScene() { }
        protected virtual void PreLoadScene() { }
        protected virtual void PostLoadScene() { }

        protected virtual void WriteProperties(INeoSerializer writer) { }
        protected virtual void ReadProperties(INeoDeserializer reader) { }

        #endregion
    }
}
