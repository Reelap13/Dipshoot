using Server.Scripts.TickSystem;
using UnityEngine;

namespace Game.Players
{
    public struct PlayerState : ITickable
    {
        public int Tick;

        public Vector3 Position;
        public Vector3 Velocity;
        public Quaternion Rotation;
        public bool IsGrounded;

        public readonly int GetTick() => Tick;
    }
}