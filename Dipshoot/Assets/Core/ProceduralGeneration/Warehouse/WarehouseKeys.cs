namespace Game.ProcGen.Warehouse
{
    public static class WarehouseKeys
    {
        public static readonly GenerationKey<WarehouseLayoutData> Layout = new("warehouse.layout");
        public static readonly GenerationKey<WarehouseNavigationGrid> Navigation = new("warehouse.navigation");
        public static readonly GenerationKey<WarehouseFitnessReport> Fitness = new("warehouse.fitness");
        public static readonly GenerationKey<WarehouseBuildPlan> BuildPlan = new("warehouse.build-plan");
    }
}
