using Server.Scripts.TickSystem;
using UnityEngine;

namespace Game.Players.Input
{
    public struct PlayerInputData : ITickable
    {
        public int Tick;

        public Vector2 Move;
        public Vector2 Look;
        public bool IsShootPressed;
        public bool IsShootHeld;

        public readonly int GetTick() => Tick;
    }
}
