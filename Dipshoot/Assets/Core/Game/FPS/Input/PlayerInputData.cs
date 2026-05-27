using Server.Scripts.TickSystem;
using UnityEngine;
using Game.Players;

namespace Game.Players.Input
{
    public struct PlayerInputData : ITickable
    {
        public int Tick;

        public Vector2 Move;
        public Vector2 Look;
        public bool IsShootPressed;
        public bool IsShootHeld;
        public int ShotSequence;
        public bool IsReloadPressed;
        public WeaponSlot RequestedWeaponSlot;
        public bool IsJumpPressed;
        public bool IsJumpHeld;
        public bool IsSprintHeld;
        public bool IsCrouchHeld;

        public readonly int GetTick() => Tick;
    }
}
