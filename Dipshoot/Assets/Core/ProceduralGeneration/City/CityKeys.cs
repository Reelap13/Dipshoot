namespace Game.ProcGen.City
{
    public static class CityKeys
    {
        public static readonly GenerationKey<CitySurfaceData> Surface = new GenerationKey<CitySurfaceData>("city.surface");
        public static readonly GenerationKey<CityZoneData> Zones = new GenerationKey<CityZoneData>("city.zones");
        public static readonly GenerationKey<CityRoadGraphData> GlobalRoads = new GenerationKey<CityRoadGraphData>("city.global-roads");
        public static readonly GenerationKey<CityLocalLayoutData> LocalLayout = new GenerationKey<CityLocalLayoutData>("city.local-layout");
        public static readonly GenerationKey<CityArchitectureData> Architecture = new GenerationKey<CityArchitectureData>("city.architecture");
        public static readonly GenerationKey<CityGameplayData> Gameplay = new GenerationKey<CityGameplayData>("city.gameplay");
        public static readonly GenerationKey<CityDetailData> Details = new GenerationKey<CityDetailData>("city.details");
        public static readonly GenerationKey<CityBuildPlan> BuildPlan = new GenerationKey<CityBuildPlan>("city.build-plan");
    }
}
