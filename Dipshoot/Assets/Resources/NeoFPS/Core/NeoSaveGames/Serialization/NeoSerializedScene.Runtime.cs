#if !UNITY_EDITOR

using UnityEngine;

namespace NeoSaveGames.Serialization
{
    public abstract partial class NeoSerializedScene : MonoBehaviour
    {
        void Awake() { Awake_(); }
        void OnDestroy() { OnDestroy_(); }
    }
}

#endif