using Game.Players.Input;
using System.Collections.Generic;
using UnityEngine;

namespace Game.TickSystem
{
    public class InputBuffer
    {
        private Dictionary<int, InputData> _inputs = new();

        public void Add(InputData input)
        {
            _inputs[input.Tick] = input;
        }

        public bool TryGet(int tick, out InputData input)
        {
            return _inputs.TryGetValue(tick, out input);
        }

        public IEnumerable<InputData> GetRange(int from_tick, int to_tick)
        {
            for (int i = from_tick; i <= to_tick; i++)
            {
                if (_inputs.TryGetValue(i, out var input))
                    yield return input;
            }
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