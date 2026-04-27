using System;
using UnityEngine;

namespace Game.ProcGen.Chunked
{
    public sealed class TreeScatterPass : ProcGenPass
    {
        public override string Id => "tree-scatter";

        public override void Declare(GenerationPassContract contract)
        {
            contract.Read(ProcGenBlackboardKeys.ChunkTerrain);
            contract.Write(ProcGenBlackboardKeys.TreePlacements);
        }

        public override void Execute(GenerationContext context)
        {
            ChunkedTerrainRecipe recipe = context.GetRecipe<ChunkedTerrainRecipe>();
            ChunkTerrainData terrainData = context.Blackboard.GetRequired<ChunkTerrainData>(ProcGenBlackboardKeys.ChunkTerrain);
            TreePlacementData placementData = new TreePlacementData();

            for (int chunkIndex = 0; chunkIndex < terrainData.HeightMaps.Count; chunkIndex++)
            {
                ChunkHeightMap heightMap = terrainData.HeightMaps[chunkIndex];
                int chunkSeed = GetChunkSeed(context.Request.Seed, heightMap.Descriptor.GridCoordinate);
                System.Random random = new System.Random(chunkSeed);
                int treeCount = random.Next(recipe.MinTreesPerChunk, recipe.MaxTreesPerChunk + 1);

                for (int treeIndex = 0; treeIndex < treeCount; treeIndex++)
                {
                    Vector3? worldPosition = TryFindTreePosition(recipe, heightMap, random);
                    if (worldPosition.HasValue == false)
                        continue;

                    float uniformScale = Mathf.Lerp(recipe.TreeScaleRange.x, recipe.TreeScaleRange.y, (float)random.NextDouble());
                    int prefabIndex = recipe.TreeCandidates.Count == 0 ? -1 : random.Next(0, recipe.TreeCandidates.Count);

                    placementData.Placements.Add(new TreePlacement
                    {
                        ChunkCoordinate = heightMap.Descriptor.GridCoordinate,
                        Position = worldPosition.Value,
                        Rotation = Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f),
                        Scale = Vector3.one * uniformScale,
                        PrefabIndex = prefabIndex
                    });
                }
            }

            context.Blackboard.Set(ProcGenBlackboardKeys.TreePlacements, placementData);
        }

        private static int GetChunkSeed(int seed, Vector2Int chunkCoordinate)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + seed;
                hash = hash * 31 + chunkCoordinate.x;
                hash = hash * 31 + chunkCoordinate.y;
                return hash;
            }
        }

        private static Vector3? TryFindTreePosition(ChunkedTerrainRecipe recipe, ChunkHeightMap heightMap, System.Random random)
        {
            const int maxAttempts = 8;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                float localX = Mathf.Lerp(recipe.TreePlacementPadding, heightMap.Descriptor.Size.x - recipe.TreePlacementPadding, (float)random.NextDouble());
                float localZ = Mathf.Lerp(recipe.TreePlacementPadding, heightMap.Descriptor.Size.y - recipe.TreePlacementPadding, (float)random.NextDouble());
                float height = SampleHeight(heightMap, localX, localZ);
                float slope = EstimateSlope(heightMap, localX, localZ);

                if (slope > recipe.MaxTreeSlope)
                    continue;

                return new Vector3(heightMap.Descriptor.Origin.x + localX, height, heightMap.Descriptor.Origin.z + localZ);
            }

            return null;
        }

        private static float SampleHeight(ChunkHeightMap heightMap, float localX, float localZ)
        {
            int samples = heightMap.SamplesPerAxis;
            float normalizedX = Mathf.Clamp01(localX / heightMap.Descriptor.Size.x);
            float normalizedZ = Mathf.Clamp01(localZ / heightMap.Descriptor.Size.y);
            float sampleX = normalizedX * (samples - 1);
            float sampleZ = normalizedZ * (samples - 1);

            int x0 = Mathf.Clamp(Mathf.FloorToInt(sampleX), 0, samples - 1);
            int z0 = Mathf.Clamp(Mathf.FloorToInt(sampleZ), 0, samples - 1);
            int x1 = Mathf.Clamp(x0 + 1, 0, samples - 1);
            int z1 = Mathf.Clamp(z0 + 1, 0, samples - 1);

            float tx = sampleX - x0;
            float tz = sampleZ - z0;

            float h00 = GetHeight(heightMap, x0, z0);
            float h10 = GetHeight(heightMap, x1, z0);
            float h01 = GetHeight(heightMap, x0, z1);
            float h11 = GetHeight(heightMap, x1, z1);

            float hx0 = Mathf.Lerp(h00, h10, tx);
            float hx1 = Mathf.Lerp(h01, h11, tx);
            return Mathf.Lerp(hx0, hx1, tz);
        }

        private static float EstimateSlope(ChunkHeightMap heightMap, float localX, float localZ)
        {
            float epsilonX = heightMap.Descriptor.Size.x / (heightMap.SamplesPerAxis - 1);
            float epsilonZ = heightMap.Descriptor.Size.y / (heightMap.SamplesPerAxis - 1);

            float left = SampleHeight(heightMap, Mathf.Max(0f, localX - epsilonX), localZ);
            float right = SampleHeight(heightMap, Mathf.Min(heightMap.Descriptor.Size.x, localX + epsilonX), localZ);
            float down = SampleHeight(heightMap, localX, Mathf.Max(0f, localZ - epsilonZ));
            float up = SampleHeight(heightMap, localX, Mathf.Min(heightMap.Descriptor.Size.y, localZ + epsilonZ));

            float dx = Math.Abs(right - left);
            float dz = Math.Abs(up - down);
            return Mathf.Max(dx, dz);
        }

        private static float GetHeight(ChunkHeightMap heightMap, int x, int z)
        {
            return heightMap.Heights[z * heightMap.SamplesPerAxis + x];
        }
    }
}
