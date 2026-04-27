using UnityEngine;

namespace Game.ProcGen.Chunked
{
    public sealed class ChunkLayoutPass : ProcGenPass
    {
        public override string Id => "chunk-layout";

        public override void Declare(GenerationPassContract contract)
        {
            contract.Write(ProcGenBlackboardKeys.ChunkLayout);
        }

        public override void Execute(GenerationContext context)
        {
            ChunkedTerrainRecipe recipe = context.GetRecipe<ChunkedTerrainRecipe>();
            ChunkLayoutData layout = new ChunkLayoutData();

            Vector2Int gridSize = recipe.ChunkGridSize;
            Vector2 chunkSize = recipe.ChunkSize;
            float xOffset = (gridSize.x - 1) * chunkSize.x * 0.5f;
            float zOffset = (gridSize.y - 1) * chunkSize.y * 0.5f;

            for (int y = 0; y < gridSize.y; y++)
            {
                for (int x = 0; x < gridSize.x; x++)
                {
                    Vector3 origin = new Vector3(x * chunkSize.x - xOffset, 0f, y * chunkSize.y - zOffset);
                    layout.Chunks.Add(new ChunkDescriptor
                    {
                        GridCoordinate = new Vector2Int(x, y),
                        Origin = origin,
                        Size = chunkSize
                    });
                }
            }

            context.Blackboard.Set(ProcGenBlackboardKeys.ChunkLayout, layout);
        }
    }
}
