using System;
using System.Collections.Generic;

namespace Game.ProcGen
{
    public sealed class GenerationResult
    {
        private readonly Dictionary<string, object> _artifacts;
        private readonly List<GenerationDiagnostic> _diagnostics;

        internal GenerationResult(
            GenerationRequest request,
            ProcGenRecipe recipe,
            Dictionary<string, object> artifacts,
            IReadOnlyList<GenerationDiagnostic> diagnostics)
        {
            Request = request;
            Recipe = recipe ?? throw new ArgumentNullException(nameof(recipe));
            _artifacts = artifacts ?? new Dictionary<string, object>();
            _diagnostics = new List<GenerationDiagnostic>();

            if (diagnostics == null)
                return;

            for (int i = 0; i < diagnostics.Count; i++)
                _diagnostics.Add(diagnostics[i]);
        }

        public GenerationRequest Request { get; }

        public ProcGenRecipe Recipe { get; }

        public IReadOnlyDictionary<string, object> Artifacts => _artifacts;

        public IReadOnlyList<GenerationDiagnostic> Diagnostics => _diagnostics;

        public bool HasErrors
        {
            get
            {
                for (int i = 0; i < _diagnostics.Count; i++)
                {
                    if (_diagnostics[i].Severity == GenerationDiagnosticSeverity.Error)
                        return true;
                }

                return false;
            }
        }

        public T GetRequired<T>(GenerationKey<T> key)
        {
            return GetRequired<T>(key.Id);
        }

        public T GetRequired<T>(string key)
        {
            if (_artifacts.TryGetValue(key, out object value) == false)
                throw new InvalidOperationException($"Generation result artifact '{key}' was not found.");

            if (value is T typedValue)
                return typedValue;

            throw new InvalidOperationException($"Generation result artifact '{key}' does not contain value of type {typeof(T).Name}.");
        }

        public bool TryGet<T>(GenerationKey<T> key, out T value)
        {
            return TryGet(key.Id, out value);
        }

        public bool TryGet<T>(string key, out T value)
        {
            if (_artifacts.TryGetValue(key, out object rawValue) && rawValue is T typedValue)
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
            return _artifacts.ContainsKey(key);
        }
    }
}
