using UnityEngine;

namespace Game.Players
{
    public struct ShotResult
    {
        public uint ShooterNetId;
        public uint HitNetId;
        public WeaponSlot WeaponSlot;
        public int ShotSequence;
        public int SpreadSeed;
        public int InputTick;
        public int ServerTick;
        public int ShotViewTick;
        public int ValidatedShotViewTick;
        public int HitboxQueryTick;
        public int HitboxSnapshotTick;
        public int LagCompensationVisualBackTicks;
        public int LagCompensationBiasTicks;
        public int LagCompensationRewindTicks;
        public int ShotServerRewindTicks;
        public bool ShotTimestampClamped;
        public Vector3 Origin;
        public Vector3 Direction;
        public Vector3 Point;
        public Vector3 Normal;
        public int Damage;
        public PlayerHitboxType HitboxType;
        public float DamageMultiplier;
        public bool HasHit;
        public bool DidDamage;
        public bool DidKill;
    }
}
