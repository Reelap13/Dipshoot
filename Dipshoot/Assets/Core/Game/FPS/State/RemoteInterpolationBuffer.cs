using System.Collections.Generic;
using UnityEngine;

namespace Game.Players.State
{
    public class RemoteInterpolationBuffer
    {
        private readonly List<PlayerState> _states = new();

        public int Count => _states.Count;
        public int OldestTick => _states.Count == 0 ? -1 : _states[0].Tick;
        public int NewestTick => _states.Count == 0 ? -1 : _states[^1].Tick;

        public void Clear()
        {
            _states.Clear();
        }

        public void Add(PlayerState state)
        {
            for (int i = 0; i < _states.Count; i++)
            {
                if (_states[i].Tick == state.Tick)
                {
                    _states[i] = state;
                    return;
                }

                if (_states[i].Tick > state.Tick)
                {
                    _states.Insert(i, state);
                    return;
                }
            }

            _states.Add(state);
        }

        public void RemoveUpTo(int tick)
        {
            int remove_count = 0;
            while (remove_count < _states.Count - 2 && _states[remove_count].Tick <= tick)
                remove_count++;

            if (remove_count > 0)
                _states.RemoveRange(0, remove_count);
        }

        public bool TryGetInterpolatedState(float render_tick, out PlayerState state)
        {
            state = default;

            if (_states.Count == 0)
                return false;

            if (_states.Count == 1)
            {
                state = _states[0];
                return true;
            }

            PlayerState from_state = _states[0];
            PlayerState to_state = _states[_states.Count - 1];

            for (int i = 0; i < _states.Count - 1; i++)
            {
                PlayerState current_state = _states[i];
                PlayerState next_state = _states[i + 1];

                if (render_tick < current_state.Tick)
                {
                    state = current_state;
                    return true;
                }

                if (render_tick <= next_state.Tick)
                {
                    from_state = current_state;
                    to_state = next_state;
                    break;
                }
            }

            if (from_state.Tick == to_state.Tick)
            {
                state = from_state;
                return true;
            }

            float t = Mathf.InverseLerp(from_state.Tick, to_state.Tick, render_tick);

            state = new PlayerState
            {
                Tick = Mathf.RoundToInt(render_tick),
                Position = Vector3.Lerp(from_state.Position, to_state.Position, t),
                Velocity = Vector3.Lerp(from_state.Velocity, to_state.Velocity, t),
                Rotation = Quaternion.Slerp(from_state.Rotation, to_state.Rotation, t),
                CameraPitch = Mathf.Lerp(from_state.CameraPitch, to_state.CameraPitch, t),
                RecoilPitch = Mathf.Lerp(from_state.RecoilPitch, to_state.RecoilPitch, t),
                RecoilYaw = Mathf.Lerp(from_state.RecoilYaw, to_state.RecoilYaw, t),
                IsGrounded = t < 0.5f ? from_state.IsGrounded : to_state.IsGrounded,
                Stance = t < 0.5f ? from_state.Stance : to_state.Stance,
                TimeSinceGrounded = Mathf.Lerp(
                    from_state.TimeSinceGrounded,
                    to_state.TimeSinceGrounded,
                    t),
                TimeSinceJumpPressed = Mathf.Lerp(
                    from_state.TimeSinceJumpPressed,
                    to_state.TimeSinceJumpPressed,
                    t),
            };

            return true;
        }
    }
}
