using UnityEngine;

namespace NeoSaveGames.Serialization
{
    public interface INeoSerializedSceneObject
    {
        bool isDestroyed { get; }

        GameObject gameObject { get; }
        Transform transform { get; }

        INeoSerializedGameObjectContainer nestedObjects { get; }

        void Initialise();
    }
}
