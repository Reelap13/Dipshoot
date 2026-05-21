using System;

namespace Game.ProcGen
{
    public readonly struct GenerationKey<T>
    {
        public GenerationKey(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Generation key id cannot be empty.", nameof(id));

            Id = id;
        }

        public string Id { get; }

        public override string ToString()
        {
            return Id;
        }
    }
}
