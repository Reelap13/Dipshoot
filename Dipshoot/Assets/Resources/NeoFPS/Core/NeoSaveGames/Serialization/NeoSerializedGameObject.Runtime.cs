#if !UNITY_EDITOR

using UnityEngine;

namespace NeoSaveGames.Serialization
{
    public sealed partial class NeoSerializedGameObject : MonoBehaviour
    {
        void OnDestroy() { OnDestroy_(); }

        public NeoSerializedScene GetScene() { return GetScene_(); }
    }
}

#endif