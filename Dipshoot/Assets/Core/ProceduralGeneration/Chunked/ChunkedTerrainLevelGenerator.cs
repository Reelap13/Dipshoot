using UnityEngine;

namespace Game.ProcGen.Chunked
{
    public sealed class ChunkedTerrainLevelGenerator : MonoBehaviour
    {
        [SerializeField] private ChunkedTerrainRecipe _recipe;
        [SerializeField] private int _seed = 12345;
        [SerializeField] private string _generatedRootName = "GeneratedMap";

        private readonly PipelineMapGenerator _generator = new PipelineMapGenerator();

        public ChunkedTerrainRecipe Recipe => _recipe;

        public void GenerateLevel()
        {
            ChunkedTerrainRecipe recipe = GetOrCreateRecipe();
            GenerationRequest request = new GenerationRequest(_seed, true);
            MapBuildPlan plan = _generator.Generate(request, recipe);
            ChunkedTerrainPlanExecutor.Execute(plan, transform, recipe, _generatedRootName, request.ClearPreviousOutput);
        }

        private ChunkedTerrainRecipe GetOrCreateRecipe()
        {
            if (_recipe != null)
                return _recipe;

            _recipe = ScriptableObject.CreateInstance<ChunkedTerrainRecipe>();
            _recipe.name = "RuntimeChunkedTerrainRecipe";
            return _recipe;
        }
    }
}
