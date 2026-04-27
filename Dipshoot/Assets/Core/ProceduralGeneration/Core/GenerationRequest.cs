namespace Game.ProcGen
{
    public readonly struct GenerationRequest
    {
        public GenerationRequest(int seed, bool clearPreviousOutput = true)
        {
            Seed = seed;
            ClearPreviousOutput = clearPreviousOutput;
        }

        public int Seed { get; }

        public bool ClearPreviousOutput { get; }
    }
}
