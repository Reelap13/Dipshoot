using System.Collections.Generic;

namespace Game.ProcGen
{
    public sealed class GenerationDiagnostics
    {
        private readonly List<GenerationDiagnostic> _items = new List<GenerationDiagnostic>();

        public IReadOnlyList<GenerationDiagnostic> Items => _items;

        public bool HasErrors
        {
            get
            {
                for (int i = 0; i < _items.Count; i++)
                {
                    if (_items[i].Severity == GenerationDiagnosticSeverity.Error)
                        return true;
                }

                return false;
            }
        }

        public void Add(GenerationDiagnosticSeverity severity, string message, string passId = null)
        {
            _items.Add(new GenerationDiagnostic(severity, message, passId));
        }

        public void Info(string message, string passId = null)
        {
            Add(GenerationDiagnosticSeverity.Info, message, passId);
        }

        public void Warning(string message, string passId = null)
        {
            Add(GenerationDiagnosticSeverity.Warning, message, passId);
        }

        public void Error(string message, string passId = null)
        {
            Add(GenerationDiagnosticSeverity.Error, message, passId);
        }

        internal List<GenerationDiagnostic> CreateSnapshot()
        {
            return new List<GenerationDiagnostic>(_items);
        }
    }
}
