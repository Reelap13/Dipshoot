using System.Collections.Generic;
using UnityEngine;

namespace Scripts.DataType
{
    public class IdentifiersSet<S, T> : Singleton<S> where S : MonoBehaviour where T : IIdentifierable
    {
        [SerializeField] private List<T> _elements;

        private Dictionary<int, T> _identifiers = new();
        private bool _is_loaded = false;

        private void Awake()
        {
            Load();
        }

        public T GetElement(int id)
        {
            if (!_is_loaded)
                Load();

            if (_identifiers.ContainsKey(id))
                return _identifiers[id];

            Debug.LogError($"Error: try to get identifier from set '{name}' with unexisted id '{id}'");
            return default(T);
        }

        private void Load()
        {
            if (_is_loaded) return;

            foreach (var element in _elements)
            {
                if (_identifiers.TryGetValue(element.GetId(), out var identifier))
                    continue;
                _identifiers.Add(element.GetId(), element);
            }
        }

        public List<T> Elements
        {
            get
            {
                if (!_is_loaded)
                    Load();

                return _elements;
            }
        }
    }
}