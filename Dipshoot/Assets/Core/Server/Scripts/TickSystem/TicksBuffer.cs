using Game.Players.Input;
using System.Collections.Generic;
using UnityEngine;

namespace Server.Scripts.TickSystem
{
    public class TicksBuffer<T> where T : ITickable
    {
        private Dictionary<int, T> _inputs = new();

        public void Add(T input)
        {
            _inputs[input.GetTick()] = input;
        }

        public bool TryGet(int tick, out T input)
        {
            return _inputs.TryGetValue(tick, out input);
        }

        public bool TryGetFirstAfter(int tick, out T input)
        {
            input = default;
            bool has_input = false;
            int first_tick = int.MaxValue;

            foreach (var pair in _inputs)
            {
                if (pair.Key <= tick || pair.Key >= first_tick)
                    continue;

                first_tick = pair.Key;
                input = pair.Value;
                has_input = true;
            }

            return has_input;
        }

        public bool TryGetLastAtOrBefore(int tick, out T input)
        {
            input = default;
            bool has_input = false;
            int last_tick = int.MinValue;

            foreach (var pair in _inputs)
            {
                if (pair.Key > tick || pair.Key <= last_tick)
                    continue;

                last_tick = pair.Key;
                input = pair.Value;
                has_input = true;
            }

            return has_input;
        }

        public List<T> GetRange(int from_tick, int to_tick)
        {
            List<T> inputs = new();
            for (int i = from_tick; i <= to_tick; i++)
            {
                if (_inputs.TryGetValue(i, out var input))
                    inputs.Add(input);
            }
            return inputs;
        }

        public void RemoveUpTo(int tick)
        {
            var keys = new List<int>();

            foreach (var kvp in _inputs)
            {
                if (kvp.Key <= tick)
                    keys.Add(kvp.Key);
            }

            foreach (var k in keys)
                _inputs.Remove(k);
        }
    }
}
