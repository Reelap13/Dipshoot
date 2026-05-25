using UnityEngine;

namespace Game.Players
{
    public struct ShotResult
    {
        public uint ShooterNetId;
        public uint HitNetId;
        public WeaponSlot WeaponSlot;
        public int InputTick;
        public int ServerTick;
        public Vector3 Origin;
        public Vector3 Direction;
        public Vector3 Point;
        public int Damage;
        public PlayerHitboxType HitboxType;
        public float DamageMultiplier;
        public bool HasHit;
        public bool DidDamage;
    }
}
