using Game.Players.Input;
using Game.TickSystem;
using UnityEngine;

namespace Game.Players
{
    public class AimController : PlayerCharacterComponent
    {
        [SerializeField] private float _yaw_sensitivity = 0.15f;
        [SerializeField] private float _pitch_sensitivity = 0.15f;
        [SerializeField] private float _min_camera_pitch = -80f;
        [SerializeField] private float _max_camera_pitch = 80f;

        private int _last_server_processed_input_tick = -1;

        public override TickLayer TickLayer => TickLayer.AimSimulation;

        public override bool ShouldTick(GameTickContext context)
        {
            return base.ShouldTick(context) && IsAlive && IsGameplayActive && (IsServer || IsClient && IsOwned);
        }

        protected override void OnTick(GameTickContext context)
        {
            if (IsServer)
            {
                SimulateServerTick(context.Tick);
                return;
            }

            SimulateTick(context.Tick);
        }

        public bool SimulateTick(int tick)
        {
            if (!Character.InputBuffet.TryGet(tick, out PlayerInputData input))
                return false;

            if (!TryGetPreviousStateForTick(tick, out PlayerState previous_state))
                return false;

            PlayerState new_state = Simulate(previous_state, input, tick);
            Character.StateBuffer.Add(new_state);
            return true;
        }

        public bool SimulateServerTick(int server_tick)
        {
            if (!TryGetPreviousStateForTick(server_tick, out PlayerState previous_state))
                return false;

            previous_state = FillMissingStates(previous_state, server_tick - 1);
            PlayerInputData input = GetServerInput();
            PlayerState new_state = Simulate(previous_state, input, server_tick);
            Character.StateBuffer.Add(new_state);
            return true;
        }

        public PlayerState Simulate(PlayerState previous_state, PlayerInputData input, int tick)
        {
            return AimSimulation.Simulate(
                previous_state,
                input,
                _yaw_sensitivity,
                _pitch_sensitivity,
                _min_camera_pitch,
                _max_camera_pitch,
                tick);
        }

        private bool TryGetPreviousStateForTick(int tick, out PlayerState previous_state)
        {
            if (Character.StateBuffer.TryGet(tick - 1, out previous_state))
                return true;

            if (Character.StateBuffer.TryGetLastAtOrBefore(tick - 1, out previous_state))
                return true;

            previous_state = default;
            return false;
        }

        private PlayerState FillMissingStates(PlayerState previous_state, int target_tick)
        {
            while (previous_state.Tick < target_tick)
            {
                int next_tick = previous_state.Tick + 1;
                PlayerState state = Simulate(previous_state, default, next_tick);
                Character.StateBuffer.Add(state);
                previous_state = state;
            }

            return previous_state;
        }

        private PlayerInputData GetServerInput()
        {
            if (Character.InputBuffet.TryGetFirstAfter(_last_server_processed_input_tick, out PlayerInputData input))
            {
                _last_server_processed_input_tick = input.Tick;
                return input;
            }

            return default;
        }

        public override void ResetSimulation()
        {
            _last_server_processed_input_tick = -1;
        }
    }
}
