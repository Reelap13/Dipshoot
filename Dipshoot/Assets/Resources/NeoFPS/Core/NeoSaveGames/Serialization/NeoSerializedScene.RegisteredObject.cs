using UnityEngine;

namespace NeoSaveGames.Serialization
{
    public abstract partial class NeoSerializedScene : MonoBehaviour
    {
        [SerializeField, Tooltip("Predefined collections of prefabs and assets that should be available for this scene")]
        private SaveGamePrefabCollection[] m_Collections = { };

        [SerializeField, Tooltip("Prefabs available for serialization in this scene.")]
        private NeoSerializedGameObject[] m_Prefabs = new NeoSerializedGameObject[0];

        [SerializeField, Tooltip("Assets available for serialization in this scene.")]
        private ScriptableObject[] m_Assets = new ScriptableObject[0];

        public SaveGamePrefabCollection[] registeredCollections
        {
            get { return m_Collections; }
        }

        public NeoSerializedGameObject[] registeredPrefabs
        {
            get { return m_Prefabs; }
        }

        public ScriptableObject[] registeredAssets
        {
            get { return m_Assets; }
        }
    }
}
