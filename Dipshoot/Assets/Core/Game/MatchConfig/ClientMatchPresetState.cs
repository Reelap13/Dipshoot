namespace Game.MatchConfig
{
    public static class ClientMatchPresetState
    {
        public static string PresetId { get; private set; }
        public static int Seed { get; private set; }
        public static string ResultUrl { get; private set; }
        public static bool UsesGeneratedLevel { get; private set; }

        public static void Set(string presetId, int seed, string resultUrl, bool usesGeneratedLevel)
        {
            PresetId = presetId;
            Seed = seed;
            ResultUrl = resultUrl;
            UsesGeneratedLevel = usesGeneratedLevel;
        }

        public static void Clear()
        {
            PresetId = string.Empty;
            Seed = 0;
            ResultUrl = string.Empty;
            UsesGeneratedLevel = false;
        }
    }
}
