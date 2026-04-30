using UnityEngine;

namespace Game.Players.State
{
    public struct PlayerStateSnapshot
    {
        public int ServerTick;
        public int LastProcessedInputTick;

        public Vector3 Position;
        public Vector3 Velocity;
        public Quaternion Rotation;
        public bool IsGrounded;

        public static PlayerStateSnapshot Create(
            PlayerState state,
            int server_tick,
            int last_processed_input_tick)
        {
            return new PlayerStateSnapshot
            {
                ServerTick = server_tick,
                LastProcessedInputTick = last_processed_input_tick,
                Position = state.Position,
                Velocity = state.Velocity,
                Rotation = state.Rotation,
                IsGrounded = state.IsGrounded,
            };
        }

        public PlayerState ToState(int tick)
        {
            return new PlayerState
            {
                Tick = tick,
                Position = Position,
                Velocity = Velocity,
                Rotation = Rotation,
                IsGrounded = IsGrounded,
            };
        }
    }
}
