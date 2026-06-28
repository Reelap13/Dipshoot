using Server.Scripts.TickSystem;
using UnityEngine;
using Game.Players;

namespace Game.Players.Input
{
    public enum ServerInputSource
    {
        None = 0,
        Received = 1,
        Repeated = 2,
        Decayed = 3,
        Neutral = 4,
        BufferReset = 5,
        Buffering = 6,
        CatchUp = 7,
    }

    public readonly struct ServerInputFrame
    {
        public readonly int ServerTick;
        public readonly int SourceInputTick;
        public readonly PlayerInputData Input;
        public readonly ServerInputSource Source;

        public bool HasCommittedInput => SourceInputTick >= 0;
        public bool IsReceived =>
            Source == ServerInputSource.Received ||
            Source == ServerInputSource.BufferReset;

        public ServerInputFrame(
            int server_tick,
            int source_input_tick,
            PlayerInputData input,
            ServerInputSource source)
        {
            ServerTick = server_tick;
            SourceInputTick = source_input_tick;
            Input = input;
            Source = source;
        }
    }

    public static class ServerInputPlayoutPolicy
    {
        public static PlayerInputData CreatePredictedInput(
            PlayerInputData last_received_input,
            bool has_last_received_input,
            int input_tick,
            int missing_ticks,
            int max_repeat_ticks,
            int decay_ticks,
            out ServerInputSource source)
        {
            PlayerInputData input = has_last_received_input
                ? last_received_input
                : default;
            input.Tick = input_tick;
            input.ShotViewTick = -1;
            input.Look = Vector2.zero;
            input.IsShootPressed = false;
            input.IsShootHeld = false;
            input.ShotSequence = 0;
            input.IsReloadPressed = false;
            input.RequestedWeaponSlot = WeaponSlot.None;
            input.IsJumpPressed = false;
            input.IsJumpHeld = false;

            int repeat_ticks = Mathf.Max(0, max_repeat_ticks);
            if (missing_ticks <= repeat_ticks)
            {
                source = ServerInputSource.Repeated;
                return input;
            }

            int safe_decay_ticks = Mathf.Max(0, decay_ticks);
            int decay_index = missing_ticks - repeat_ticks;
            if (safe_decay_ticks > 0 && decay_index <= safe_decay_ticks)
            {
                float factor = 1f - decay_index / (float)safe_decay_ticks;
                input.Move *= Mathf.Clamp01(factor);
                source = ServerInputSource.Decayed;
                return input;
            }

            source = ServerInputSource.Neutral;
            input.Move = Vector2.zero;
            input.IsSprintHeld = false;
            input.IsCrouchHeld = false;
            return input;
        }

        public static bool CanCoalesce(PlayerInputData input)
        {
            return !input.IsShootPressed &&
                input.ShotSequence == 0 &&
                !input.IsReloadPressed &&
                input.RequestedWeaponSlot == WeaponSlot.None &&
                !input.IsJumpPressed;
        }
    }

    public struct PlayerInputData : ITickable
    {
        public int Tick;
        public int ShotViewTick;

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
