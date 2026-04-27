using UnityEngine;

namespace Game.ProcGen.Chunked
{
    public sealed class PerlinHeightPass : ProcGenPass
    {
        public override string Id => "chunk-heights";

        public override void Declare(GenerationPassContract contract)
        {
            contract.Read(ProcGenBlackboardKeys.ChunkLayout);
            contract.Write(ProcGenBlackboardKeys.ChunkTerrain);
        }

        public override void Execute(GenerationContext context)
        {
            ChunkedTerrainRecipe recipe = context.GetRecipe<ChunkedTerrainRecipe>();
            ChunkLayoutData layout = context.Blackboard.GetRequired<ChunkLayoutData>(ProcGenBlackboardKeys.ChunkLayout);
            ChunkTerrainData terrainData = new ChunkTerrainData();
            int samplesPerAxis = recipe.HeightSamplesPerAxis;

            for (int i = 0; i < layout.Chunks.Count; i++)
            {
                ChunkDescriptor descriptor = layout.Chunks[i];
                ChunkHeightMap heightMap = new ChunkHeightMap
                {
                    Descriptor = descriptor,
                    SamplesPerAxis = samplesPerAxis,
                    Heights = new float[samplesPerAxis * samplesPerAxis]
                };

                float stepX = descriptor.Size.x / (samplesPerAxis - 1);
                float stepZ = descriptor.Size.y / (samplesPerAxis - 1);

                for (int z = 0; z < samplesPerAxis; z++)
                {
                    for (int x = 0; x < samplesPerAxis; x++)
                    {
                        float worldX = descriptor.Origin.x + x * stepX;
                        float worldZ = descriptor.Origin.z + z * stepZ;
                        heightMap.Heights[z * samplesPerAxis + x] = SampleHeight(recipe, context.Request.Seed, worldX, worldZ);
                    }
                }

                terrainData.HeightMaps.Add(heightMap);
            }

            context.Blackboard.Set(ProcGenBlackboardKeys.ChunkTerrain, terrainData);
        }

        private static float SampleHeight(ChunkedTerrainRecipe recipe, int seed, float worldX, float worldZ)
        {
            float amplitude = 1f;
            float frequency = 1f;
            float value = 0f;
            float maxAmplitude = 0f;
            float seedShift = seed * 0.00137f;

            for (int octave = 0; octave < recipe.Octaves; octave++)
            {
                float sampleX = ((worldX + recipe.NoiseOffset.x) / recipe.NoiseScale + seedShift) * frequency;
                float sampleZ = ((worldZ + recipe.NoiseOffset.y) / recipe.NoiseScale + seedShift) * frequency;
                float noise = Mathf.PerlinNoise(sampleX, sampleZ);
                value += noise * amplitude;
                maxAmplitude += amplitude;
                amplitude *= recipe.Persistence;
                frequency *= recipe.Lacunarity;
            }

            if (maxAmplitude <= 0f)
                return 0f;

            return value / maxAmplitude * recipe.HeightMultiplier;
        }
    }
}
