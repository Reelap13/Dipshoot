using UnityEngine;
using Game.Players.Input;

namespace Game.Players.State
{
    public struct PlayerStateSnapshot
    {
        public int ServerTick;
        public int StateTick;
        public int AckInputTick;
        public int LastProcessedInputTick;
        public int InputQueueDepth;
        public int InputTargetDepth;
        public int MissingInputTicks;
        public int BufferResetCount;
        public ServerInputSource InputSource;
        public float InputJitterTicks;

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

        public static PlayerStateSnapshot Create(
            PlayerState state,
            int server_tick,
            int last_processed_input_tick,
            int input_queue_depth = 0,
            int input_target_depth = 0,
            int missing_input_ticks = 0,
            int buffer_reset_count = 0,
            ServerInputSource input_source = ServerInputSource.None,
            float input_jitter_ticks = 0f)
        {
            return new PlayerStateSnapshot
            {
                ServerTick = server_tick,
                StateTick = state.Tick,
                AckInputTick = last_processed_input_tick,
                LastProcessedInputTick = last_processed_input_tick,
                InputQueueDepth = input_queue_depth,
                InputTargetDepth = input_target_depth,
                MissingInputTicks = missing_input_ticks,
                BufferResetCount = buffer_reset_count,
                InputSource = input_source,
                InputJitterTicks = input_jitter_ticks,
                Position = state.Position,
                Velocity = state.Velocity,
                Rotation = state.Rotation,
                CameraPitch = state.CameraPitch,
                RecoilPitch = state.RecoilPitch,
                RecoilYaw = state.RecoilYaw,
                IsGrounded = state.IsGrounded,
                Stance = state.Stance,
                TimeSinceGrounded = state.TimeSinceGrounded,
                TimeSinceJumpPressed = state.TimeSinceJumpPressed,
            };
        }

        public PlayerState ToState()
        {
            return ToState(StateTick);
        }

        public PlayerState ToState(int tick)
        {
            return new PlayerState
            {
                Tick = tick,
                Position = Position,
                Velocity = Velocity,
                Rotation = Rotation,
                CameraPitch = CameraPitch,
                RecoilPitch = RecoilPitch,
                RecoilYaw = RecoilYaw,
                IsGrounded = IsGrounded,
                Stance = Stance,
                TimeSinceGrounded = TimeSinceGrounded,
                TimeSinceJumpPressed = TimeSinceJumpPressed,
            };
        }
    }
}
