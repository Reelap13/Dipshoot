#if !UNITY_EDITOR

using System.Collections.Generic;
using UnityEngine;

namespace NeoSaveGames.Serialization
{
    public sealed partial class NeoSerializedGameObject : MonoBehaviour
    {
        public abstract partial class NeoSerializedGameObjectContainer : INeoSerializedGameObjectContainer
        {
            public void Initialise() { Initialise_(); }
            public void OnDestroy() { OnDestroy_(); }
            public bool Contains(NeoSerializedGameObject nsgo) { return Contains_(nsgo); }
            public int GetSerializationKeyForObject(NeoSerializedGameObject nsgo) { return GetSerializationKeyForObject_(nsgo); }
            public virtual void RegisterObject(NeoSerializedGameObject nsgo, int suggestedKey = 0) { RegisterObject_(nsgo, suggestedKey); }
            public virtual void UnregisterObject(NeoSerializedGameObject nsgo) { UnregisterObject_(nsgo); }
        
            private int GenerateKey() { return GenerateKey_(); }
        }
    }
}

#endif