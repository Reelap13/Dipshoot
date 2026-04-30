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
                Debug.Log(
                    $"{LogPrefix} Local simulate skipped. netId={Character.netId} tick={tick} " +
                    "reason=missing_input");
                return false;
            }

            if (!TryGetPreviousStateForTick(tick, out PlayerState previous_state))
            {
                Debug.Log(
                    $"{LogPrefix} Local simulate skipped. netId={Character.netId} tick={tick} " +
                    $"reason=missing_previous_state previousTick={tick - 1}");
                return false;
            }

            PlayerState new_state = Simulate(
                previous_state,
                input,
                TickManager.TickDelta,
                tick);

            Character.StateBuffer.Add(new_state);
            ApplyState(new_state);
            Debug.Log(
                $"{LogPrefix} Local simulate success. netId={Character.netId} tick={tick} " +
                $"inputTick={input.Tick} position={new_state.Position}");
            return true;
        }

        public bool SimulateServerTick(int server_tick)
        {
            if (!TryGetPreviousStateForTick(server_tick, out PlayerState previous_state))
            {
                Debug.Log(
                    $"{LogPrefix} Server simulate skipped. netId={Character.netId} serverTick={server_tick} " +
                    $"reason=missing_previous_state previousTick={server_tick - 1}");
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
            Debug.Log(
                $"{LogPrefix} Server simulate success. netId={Character.netId} serverTick={server_tick} " +
                $"inputTick={input.Tick} position={new_state.Position}");
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

        private bool TryGetPreviousStateForTick(int tick, out PlayerState previous_state)
        {
            if (Character.StateBuffer.TryGet(tick - 1, out previous_state))
                return true;

            if (Character.StateBuffer.TryGetLastAtOrBefore(tick - 1, out previous_state))
            {
                Debug.Log(
                    $"{LogPrefix} Resolved previous state by fallback. netId={Character.netId} " +
                    $"requestedPreviousTick={tick - 1} resolvedTick={previous_state.Tick}");
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
                Debug.Log(
                    $"{LogPrefix} Filled missing state. netId={Character.netId} tick={next_tick} " +
                    $"inputTick={input.Tick} position={state.Position}");
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
                Debug.Log(
                    $"{LogPrefix} Server consumed new input. netId={Character.netId} " +
                    $"inputTick={input.Tick} move={input.Move}");
                return input;
            }

            if (_has_last_server_input)
            {
                Debug.Log(
                    $"{LogPrefix} Server reusing last input. netId={Character.netId} " +
                    $"inputTick={_last_server_input.Tick} move={_last_server_input.Move}");
                return _last_server_input;
            }

            Debug.Log(
                $"{LogPrefix} Server using neutral input. netId={Character.netId} " +
                $"lastProcessedInputTick={_last_server_processed_input_tick}");
            return default;
        }
    }
}
