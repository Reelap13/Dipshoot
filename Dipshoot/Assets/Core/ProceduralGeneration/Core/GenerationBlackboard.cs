using System;
using System.Collections.Generic;

namespace Game.ProcGen
{
    public sealed class GenerationBlackboard
    {
        private readonly Dictionary<string, object> _values = new Dictionary<string, object>();

        public IEnumerable<string> Keys => _values.Keys;

        public void Set<T>(GenerationKey<T> key, T value)
        {
            Set(key.Id, value);
        }

        public void Set<T>(string key, T value)
        {
            _values[key] = value;
        }

        public T GetRequired<T>(GenerationKey<T> key)
        {
            return GetRequired<T>(key.Id);
        }

        public T GetRequired<T>(string key)
        {
            if (_values.TryGetValue(key, out object value) == false)
                throw new InvalidOperationException($"Generation blackboard key '{key}' was not found.");

            if (value is T typedValue)
                return typedValue;

            throw new InvalidOperationException($"Generation blackboard key '{key}' does not contain value of type {typeof(T).Name}.");
        }

        public bool TryGet<T>(GenerationKey<T> key, out T value)
        {
            return TryGet(key.Id, out value);
        }

        public bool TryGet<T>(string key, out T value)
        {
            if (_values.TryGetValue(key, out object rawValue) && rawValue is T typedValue)
            {
                value = typedValue;
                return true;
            }

            value = default;
            return false;
        }

        public bool Contains<T>(GenerationKey<T> key)
        {
            return Contains(key.Id);
        }

        public bool Contains(string key)
        {
            return _values.ContainsKey(key);
        }

        public Dictionary<string, object> CreateSnapshot()
        {
            return new Dictionary<string, object>(_values);
        }

        public void Clear()
        {
            _values.Clear();
        }
    }
}
