using System;
using UnityEngine;

namespace Game.Players
{
    [CreateAssetMenu(fileName = "PlayerHitboxRigProfile", menuName = "Game/Players/PlayerHitboxRigProfile")]
    public class PlayerHitboxRigProfile : ScriptableObject
    {
        [SerializeField] private HitboxBinding[] _bindings;

        public HitboxBinding[] Bindings => _bindings;

        [Serializable]
        public struct HitboxBinding
        {
            public string Name;
            public string StartBoneName;
            public string EndBoneName;
            public PlayerHitboxType Type;
            public float DamageMultiplier;
            public HitboxShape Shape;
            public Vector3 LocalPosition;
            public Vector3 Size;
            public float Radius;
            public float LengthPadding;
        }

        public enum HitboxShape
        {
            Box,
            Sphere,
            Capsule,
        }
    }
}
