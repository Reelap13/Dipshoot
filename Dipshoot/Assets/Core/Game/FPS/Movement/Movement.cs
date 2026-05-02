using Game.Players.Input;
using Game.TickSystem;
using UnityEngine;

namespace Game.Players
{
    public class Movement : PlayerCharacterComponent
    {
        private const string LogPrefix = "[NetTick][Movement]";

        [SerializeField] private float _speed = 5f;
        [SerializeField] private float _yaw_sensitivity = 0.15f;
        [SerializeField] private float _pitch_sensitivity = 0.15f;
        [SerializeField] private float _min_camera_pitch = -80f;
        [SerializeField] private float _max_camera_pitch = 80f;
        private int _last_server_processed_input_tick = -1;
        private bool _has_last_server_input;
        private PlayerInputData _last_server_input;

        public int LastServerProcessedInputTick => _last_server_processed_input_tick;

        protected override void OnTick()
        {
            if (IsServer)
            {
                SimulateServerTick(TickManager.CurrentTick);
                return;
            }

            if (!IsOwned)
                return;

            SimulateTick(TickManager.CurrentTick);
        }

        public bool SimulateTick(int tick)
        {
            if (!Character.InputBuffet.TryGet(tick, out PlayerInputData input))
            {
                return false;
            }

            if (!TryGetPreviousStateForTick(tick, out PlayerState previous_state))
            {
                return false;
            }

            PlayerState new_state = Simulate(
                previous_state,
                input,
                TickManager.TickDelta,
                tick);

            Character.StateBuffer.Add(new_state);
            ApplyState(new_state);
            return true;
        }

        public bool SimulateServerTick(int server_tick)
        {
            if (!TryGetPreviousStateForTick(server_tick, out PlayerState previous_state))
            {
                return false;
            }

            previous_state = FillMissingStates(previous_state, server_tick - 1);

            PlayerInputData input = GetServerInput();

            PlayerState new_state = Simulate(
                previous_state,
                input,
                TickManager.TickDelta,
                server_tick);

            Character.StateBuffer.Add(new_state);
            ApplyState(new_state);
            return true;
        }

        public void ReplayFromTick(int from_tick, int to_tick)
        {
            if (to_tick < from_tick)
                return;

            for (int tick = from_tick; tick <= to_tick; tick++)
                SimulateTick(tick);
        }

        public PlayerState Simulate(
            PlayerState previous_state,
            PlayerInputData input,
            float delta_time,
            int tick)
        {
            return MovementSimulation.Simulate(
                previous_state,
                input,
                delta_time,
                _speed,
                _yaw_sensitivity,
                _pitch_sensitivity,
                _min_camera_pitch,
                _max_camera_pitch,
                tick);
        }

        public void ApplyState(PlayerState state)
        {
            MovementPresentation.ApplyState(transform, state);
        }

        private bool TryGetPreviousStateForTick(int tick, out PlayerState previous_state)
        {
            if (Character.StateBuffer.TryGet(tick - 1, out previous_state))
                return true;

            if (Character.StateBuffer.TryGetLastAtOrBefore(tick - 1, out previous_state))
            {
                return true;
            }

            previous_state = default;
            return false;
        }

        private PlayerState FillMissingStates(PlayerState previous_state, int target_tick)
        {
            while (previous_state.Tick < target_tick)
            {
                int next_tick = previous_state.Tick + 1;
                PlayerInputData input = GetServerInput();
                PlayerState state = Simulate(
                    previous_state,
                    input,
                    TickManager.TickDelta,
                    next_tick);

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
                _last_server_input = input;
                _has_last_server_input = true;
                return input;
            }

            if (_has_last_server_input)
            {
                return _last_server_input;
            }

            return default;
        }
    }
}
