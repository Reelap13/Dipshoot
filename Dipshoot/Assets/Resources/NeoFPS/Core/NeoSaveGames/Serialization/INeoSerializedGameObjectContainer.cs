using System.Collections.Generic;
using UnityEngine;

namespace NeoSaveGames.Serialization
{
    public interface INeoSerializedGameObjectContainer : IEnumerable<NeoSerializedGameObject>
    {
        INeoSerializedSceneObject owner { get; }
        Transform rootTransform { get; }
        bool isValid { get; }
        int count { get; }

        NeoSerializedGameObject this[int key] { get; }

        void Initialise();
        void OnDestroy();

        bool Contains(NeoSerializedGameObject nsgo);
        int GetSerializationKeyForObject(NeoSerializedGameObject nsgo);
        void RegisterObject(NeoSerializedGameObject nsgo, int suggestedKey = 0);
        void UnregisterObject(NeoSerializedGameObject nsgo);

        void WriteGameObjects(INeoSerializer writer, SaveMode saveMode);
        void WriteGameObjects(INeoSerializer writer, NeoSerializationFilter filter, NeoSerializedGameObject[] objects, SaveMode saveMode);
        void ReadGameObjectHierarchy(INeoDeserializer reader);
        void ReadGameObjectProperties(INeoDeserializer reader);
    }
}
