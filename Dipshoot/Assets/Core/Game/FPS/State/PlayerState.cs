using Server.Scripts.TickSystem;
using UnityEngine;

namespace Game.Players
{
    public enum MovementStance
    {
        Standing = 0,
        Crouching = 1
    }

    public struct PlayerState : ITickable
    {
        public int Tick;

        public Vector3 Position;
        public Vector3 Velocity;
        public Quaternion Rotation;
        public float CameraPitch;
        public float RecoilPitch;
        public float RecoilYaw;
        public bool IsGrounded;
        public MovementStance Stance;
        public float TimeSinceGrounded;
        public float TimeSinceJumpPressed;

        public readonly int GetTick() => Tick;
    }
}
