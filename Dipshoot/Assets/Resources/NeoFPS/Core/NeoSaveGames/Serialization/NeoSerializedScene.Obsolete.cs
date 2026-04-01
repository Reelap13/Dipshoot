using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeoSaveGames.Serialization
{
    public abstract partial class NeoSerializedScene : MonoBehaviour
    {
        public T InstantiatePrefab<T>(T prototype) where T : Component
        {
            return NeoPrefabFactory.InstantiatePrefab(prototype, this);
        }

        public T InstantiatePrefab<T>(T prototype, Vector3 position, Quaternion rotation) where T : Component
        {
            return NeoPrefabFactory.InstantiatePrefab(prototype, this, position, rotation);
        }
    }
}