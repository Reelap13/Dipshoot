using Game.Players.Input;
using Game.TickSystem;
using UnityEngine;

namespace Game.Players
{
    public class Movement : PlayerCharacterComponent
    {
        private const string LogPrefix = "[NetTick][Movement]";

        [SerializeField] private float _speed = 5f;
        private int _last_server_processed_input_tick = -1;
        private bool _has_last_server_input;
        private PlayerInputData _last_server_input;

        public int LastServerProcessedInputTick => _last_server_processed_input_tick;

        public override TickLayer TickLayer => TickLayer.Movement;

        public override bool ShouldTick(GameTickContext context)
        {
            return base.ShouldTick(context) && IsAlive && IsGameplayActive && (IsServer || IsClient && IsOwned);
        }

        protected override void OnTick(GameTickContext context)
        {
            if (IsServer)
            {
                SimulateServerTick(context.Tick, context.DeltaTime);
                return;
            }

            SimulateTick(context.Tick, context.DeltaTime);
        }

        public bool SimulateTick(int tick)
        {
            return SimulateTick(tick, TickManager.TickDelta);
        }

        public bool SimulateTick(int tick, float delta_time)
        {
            if (!Character.InputBuffet.TryGet(tick, out PlayerInputData input))
            {
                return false;
            }

            if (!TryGetSimulationStateForTick(tick, out PlayerState previous_state))
            {
                return false;
            }

            PlayerState new_state = Simulate(
                previous_state,
                input,
                delta_time,
                tick);

            Character.StateBuffer.Add(new_state);
            ApplyState(new_state);
            return true;
        }

        public bool SimulateServerTick(int server_tick)
        {
            return SimulateServerTick(server_tick, TickManager.TickDelta);
        }

        public bool SimulateServerTick(int server_tick, float delta_time)
        {
            if (!TryGetSimulationStateForTick(server_tick, out PlayerState previous_state))
            {
                return false;
            }

            previous_state = FillMissingStates(previous_state, server_tick - 1, delta_time);

            PlayerInputData input = GetServerInput();

            PlayerState new_state = Simulate(
                previous_state,
                input,
                delta_time,
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
                tick);
        }

        public void ApplyState(PlayerState state)
        {
            MovementPresentation.ApplyState(transform, state);
        }

        private bool TryGetSimulationStateForTick(int tick, out PlayerState previous_state)
        {
            if (Character.StateBuffer.TryGet(tick, out previous_state))
                return true;

            if (Character.StateBuffer.TryGet(tick - 1, out previous_state))
                return true;

            if (Character.StateBuffer.TryGetLastAtOrBefore(tick - 1, out previous_state))
            {
                return true;
            }

            previous_state = default;
            return false;
        }

        private PlayerState FillMissingStates(PlayerState previous_state, int target_tick, float delta_time)
        {
            while (previous_state.Tick < target_tick)
            {
                int next_tick = previous_state.Tick + 1;
                PlayerInputData input = GetServerInput();
                PlayerState state = Simulate(
                    previous_state,
                    input,
                    delta_time,
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

        public override void ResetSimulation()
        {
            _last_server_processed_input_tick = -1;
            _has_last_server_input = false;
            _last_server_input = default;
        }
    }
}
