using UnityEngine;

namespace NeoSaveGames.Serialization
{
    public static class NeoPrefabFactory
    {
        private static CustomPrefabFactoryBase s_Custom = null;

        public static T InstantiatePrefab<T>(T prototype, Component source) where T : Component
        {
            return InstantiatePrefab(prototype, 0, source, null);
        }

        public static T InstantiatePrefab<T>(T prototype, Component source, Transform parent) where T : Component
        {
            return InstantiatePrefab(prototype, 0, source, parent);
        }

        public static T InstantiatePrefab<T>(T prototype, int serializationKey, Component source) where T : Component
        {
            return InstantiatePrefab(prototype, serializationKey, source, null);
        }

        public static T InstantiatePrefab<T>(T prototype, int serializationKey, Component source, Transform parent) where T : Component
        {
            if (s_Custom != null)
                return s_Custom.InstantiatePrefab(prototype, serializationKey, source, parent);
            else
                return NeoSerializedGameObject.InstantiatePrefab(prototype, serializationKey, source, parent);
        }

        public static T InstantiatePrefab<T>(T prototype, Component source, Vector3 position, Quaternion rotation, TranslationSpace space = TranslationSpace.Local) where T : Component
        {
            return InstantiatePrefab(prototype, 0, source, null, position, rotation, space);
        }

        public static T InstantiatePrefab<T>(T prototype, Component source, Transform parent, Vector3 position, Quaternion rotation, TranslationSpace space = TranslationSpace.Local) where T : Component
        {
            return InstantiatePrefab(prototype, 0, source, parent, position, rotation, space);
        }

        public static T InstantiatePrefab<T>(T prototype, int serializationKey, Component source, Vector3 position, Quaternion rotation, TranslationSpace space = TranslationSpace.Local) where T : Component
        {
            return InstantiatePrefab(prototype, serializationKey, source, null, position, rotation, space);
        }

        public static T InstantiatePrefab<T>(T prototype, int serializationKey, Component source, Transform parent, Vector3 position, Quaternion rotation, TranslationSpace space = TranslationSpace.Local) where T : Component
        {
            if (s_Custom != null)
                return s_Custom.InstantiatePrefab(prototype, serializationKey, source, parent, position, rotation, space);
            else
                return NeoSerializedGameObject.InstantiatePrefab(prototype, serializationKey, source, parent, position, rotation, space);
        }

        public static NeoSerializedGameObject InstantiateFromID(int prefabID, int serializationKey, Component source)
        {
            return InstantiateFromID(prefabID, serializationKey, source, null);
        }

        public static NeoSerializedGameObject InstantiateFromID(int prefabID, int serializationKey, Component source, Transform parent)
        {
            if (SaveGameManager.enabled)
            {
                var prototype = NeoSerializedObjectRegistry.GetPrefab(prefabID);
                if (prototype != null)
                {
                    if (s_Custom != null)
                        return s_Custom.InstantiatePrefab(prototype, serializationKey, source, parent);
                    else
                        return NeoSerializedGameObject.InstantiatePrefab(prototype, serializationKey, source, parent);
                }
            }
            return null;
        }

        public static T InstantiateFromID<T>(int prefabID, int serializationKey, Component source)
        {
            var nsgo = InstantiateFromID(prefabID, serializationKey, source);
            if (nsgo != null)
                return nsgo.GetComponent<T>();
            else
                return default;
        }

        public static T InstantiateFromID<T>(int prefabID, int serializationKey, Component source, Transform parent)
        {
            var nsgo = InstantiateFromID(prefabID, serializationKey, source, parent);
            if (nsgo != null)
                return nsgo.GetComponent<T>();
            else
                return default;
        }

        public abstract class CustomPrefabFactoryBase : MonoBehaviour
        {
            protected virtual void Awake()
            {
                if (s_Custom == null)
                    s_Custom = this;
                else
                    Debug.LogError("Attempting to activate multiple custom NeoPrefabFactory components at the same time", gameObject);
            }

            protected virtual void OnDestroy()
            {
                if (s_Custom == this)
                    s_Custom = null;
            }

            public abstract T InstantiatePrefab<T>(T prototype, int serializationKey, Component source, Transform parent) where T : Component;
            public abstract T InstantiatePrefab<T>(T prototype, int serializationKey, Component source, Transform parent, Vector3 position, Quaternion rotation, TranslationSpace space) where T : Component;
        }
    }
}