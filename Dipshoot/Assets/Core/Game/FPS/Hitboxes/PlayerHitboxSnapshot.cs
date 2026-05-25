using UnityEngine;

namespace Game.Players
{
    public enum PlayerHitboxSnapshotShape
    {
        Box = 0,
        Sphere = 1,
        Capsule = 2,
    }

    public struct PlayerHitboxSnapshot
    {
        public int Tick;
        public PlayerHitboxType Type;
        public PlayerHitboxSnapshotShape Shape;
        public Vector3 Center;
        public Quaternion Rotation;
        public Vector3 Size;
        public float Radius;
        public float Height;
        public int Direction;
        public float DamageMultiplier;
    }

    public struct PlayerHitboxSnapshotFrame
    {
        public int Tick;
        public int Count;
        public PlayerHitboxSnapshot[] Snapshots;

        public bool IsValid => Snapshots != null && Count > 0;

        public void EnsureCapacity(int capacity)
        {
            if (capacity <= 0)
                return;

            if (Snapshots != null && Snapshots.Length >= capacity)
                return;

            Snapshots = new PlayerHitboxSnapshot[capacity];
        }
    }

    public struct PlayerHitboxSnapshotHit
    {
        public PlayerHealth Health;
        public uint HitNetId;
        public PlayerHitboxType HitboxType;
        public float DamageMultiplier;
        public Vector3 Point;
        public float Distance;
        public int SnapshotTick;
    }
}
