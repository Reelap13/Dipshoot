using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeoSaveGames.Serialization
{
    public sealed partial class NeoSerializedGameObject : MonoBehaviour
    {
        [SerializeField, Tooltip("Should the object be moved to the current local scene (not the main scene) if instantiated without a parent.")]
        private bool m_MoveToLocalScene = false;

        public static T InstantiatePrefab<T>(T prototype, Component source) where T : Component
        {
            return InstantiatePrefab(prototype, 0, source, null, Vector3.zero, Quaternion.identity, TranslationSpace.Local);
        }

        public static T InstantiatePrefab<T>(T prototype, Component source, Transform parent) where T : Component
        {
            return InstantiatePrefab(prototype, 0, source, parent, Vector3.zero, Quaternion.identity, TranslationSpace.Local);
        }

        public static T InstantiatePrefab<T>(T prototype, int serializationKey, Component source) where T : Component
        {
            return InstantiatePrefab(prototype, serializationKey, source, null, Vector3.zero, Quaternion.identity, TranslationSpace.Local);
        }

        public static T InstantiatePrefab<T>(T prototype, int serializationKey, Component source, Transform parent) where T : Component
        {
            return InstantiatePrefab(prototype, serializationKey, source, parent, Vector3.zero, Quaternion.identity, TranslationSpace.Local);
        }

        public static T InstantiatePrefab<T>(T prototype, Component source, Transform parent, Vector3 position, Quaternion rotation, TranslationSpace space = TranslationSpace.Local) where T : Component
        {
            return InstantiatePrefab(prototype, 0, source, parent, position, rotation, space);
        }

        public static T InstantiatePrefab<T>(T prototype, int serializationKey, Component source, Transform parent, Vector3 position, Quaternion rotation, TranslationSpace space = TranslationSpace.Local) where T : Component
        {
            T result;
            Transform resultTransform;
            NeoSerializedGameObject resultNSGO;

            // Instantiate the prefab
            if (parent == null)
            {
                result = Instantiate(prototype);
                resultTransform = result.transform;
                resultNSGO = result.GetComponent<NeoSerializedGameObject>();

                bool checkedScene = false;
                if (resultNSGO.m_MoveToLocalScene)
                {
                    var localScene = SaveGameManager.localScene;
                    if (localScene != null)
                    {
                        SceneManager.MoveGameObjectToScene(result.gameObject, localScene.scene);
                        checkedScene = true;
                    }
                }
                
                if (!checkedScene)
                {
                    // Make sure it belongs to the correct scene (instead of the loading screen scene)
                    if (result.gameObject.scene != source.gameObject.scene)
                        SceneManager.MoveGameObjectToScene(result.gameObject, source.gameObject.scene);
                }

                resultTransform.localScale = Vector3.one;
                resultTransform.localPosition = position;
                resultTransform.localRotation = rotation;
            }
            else
            {
                if (space == TranslationSpace.Local)
                {
                    rotation = parent.rotation * rotation;
                    position = parent.position + parent.rotation * position;
                }

                result = Instantiate(prototype, position, rotation, parent);
                resultTransform = result.transform;
                resultTransform.localScale = Vector3.one;
                resultNSGO = result.GetComponent<NeoSerializedGameObject>();
            }

            // Get the target parent container & set transform parent
            INeoSerializedGameObjectContainer container = null;
            if (resultNSGO != null)
            {
                // Initialise the NSGO
                resultNSGO.instantiatedPrefabRoot = true;
                resultNSGO.wasRuntimeInstantiated = true;
                resultNSGO.Initialise();

                // Register with parent container
                resultNSGO.GetParentContainer(out container, true);
                if (container != null)
                    container.RegisterObject(resultNSGO, serializationKey);
            }

            return result;
        }

        #region OBSOLETE

        public T InstantiatePrefab<T>(T prototype) where T : Component
        {
            return InstantiatePrefab(prototype, this, transform);
        }

        public T InstantiatePrefab<T>(T prototype, Vector3 position, Quaternion rotation) where T : Component
        {
            return InstantiatePrefab(prototype, this, transform, position, rotation, TranslationSpace.World);
        }

        public void SetParent(NeoSerializedGameObject target)
        {
            if (target != null)
                transform.SetParent(target.transform);
        }

        #endregion
    }

    public enum TranslationSpace
    {
        World,
        Local
    }
}