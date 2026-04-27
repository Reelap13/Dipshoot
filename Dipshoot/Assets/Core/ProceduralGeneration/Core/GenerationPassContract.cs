using System.Collections.Generic;

namespace Game.ProcGen
{
    public sealed class GenerationPassContract
    {
        private readonly List<string> _reads = new List<string>();
        private readonly List<string> _writes = new List<string>();

        public GenerationPassContract(GeneratorBackendKind backend)
        {
            Backend = backend;
        }

        public GeneratorBackendKind Backend { get; }

        public IReadOnlyList<string> Reads => _reads;

        public IReadOnlyList<string> Writes => _writes;

        public void Read(string key)
        {
            if (_reads.Contains(key) == false)
                _reads.Add(key);
        }

        public void Write(string key)
        {
            if (_writes.Contains(key) == false)
                _writes.Add(key);
        }
    }
}
