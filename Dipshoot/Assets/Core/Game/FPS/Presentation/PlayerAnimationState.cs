using UnityEngine;

namespace Game.Players
{
    public struct PlayerAnimationState
    {
        public Vector2 Move;
        public float Speed01;
        public bool IsGrounded;
        public bool IsCrouching;
        public bool IsSprinting;
        public bool IsFalling;
        public int WeaponSlot;
        public int FireSequence;
        public float AimPitch;
    }
}
