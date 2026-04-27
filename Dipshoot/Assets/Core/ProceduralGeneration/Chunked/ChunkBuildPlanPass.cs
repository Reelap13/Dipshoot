namespace Game.ProcGen.Chunked
{
    public sealed class ChunkBuildPlanPass : ProcGenPass
    {
        public override string Id => "chunk-build-plan";

        public override void Declare(GenerationPassContract contract)
        {
            contract.Read(ProcGenBlackboardKeys.ChunkTerrain);
            contract.Read(ProcGenBlackboardKeys.TreePlacements);
            contract.Write(ProcGenBlackboardKeys.BuildPlan);
        }

        public override void Execute(GenerationContext context)
        {
            ChunkTerrainData terrainData = context.Blackboard.GetRequired<ChunkTerrainData>(ProcGenBlackboardKeys.ChunkTerrain);
            TreePlacementData treeData = context.Blackboard.GetRequired<TreePlacementData>(ProcGenBlackboardKeys.TreePlacements);
            MapBuildPlan plan = new MapBuildPlan();

            for (int i = 0; i < terrainData.HeightMaps.Count; i++)
            {
                ChunkHeightMap heightMap = terrainData.HeightMaps[i];
                plan.TerrainChunks.Add(new MapTerrainChunkPlan
                {
                    GridCoordinate = heightMap.Descriptor.GridCoordinate,
                    Origin = heightMap.Descriptor.Origin,
                    ChunkSize = heightMap.Descriptor.Size,
                    SamplesPerAxis = heightMap.SamplesPerAxis,
                    Heights = heightMap.Heights
                });
            }

            for (int i = 0; i < treeData.Placements.Count; i++)
            {
                TreePlacement placement = treeData.Placements[i];
                plan.ObjectPlacements.Add(new MapObjectPlacementPlan
                {
                    Position = placement.Position,
                    Rotation = placement.Rotation,
                    Scale = placement.Scale,
                    PrefabIndex = placement.PrefabIndex
                });
            }

            context.Blackboard.Set(ProcGenBlackboardKeys.BuildPlan, plan);
        }
    }
}
