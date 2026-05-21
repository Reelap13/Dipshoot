namespace Game.ProcGen
{
    public interface IMapGenerator
    {
        MapBuildPlan Generate(GenerationRequest request, ProcGenRecipe recipe);
    }

    public sealed class PipelineMapGenerator : IMapGenerator
    {
        private readonly GenerationPipeline _pipeline = new GenerationPipeline();

        public GenerationResult GenerateResult(GenerationRequest request, ProcGenRecipe recipe)
        {
            return _pipeline.Generate(request, recipe);
        }

        public MapBuildPlan Generate(GenerationRequest request, ProcGenRecipe recipe)
        {
            return GenerateResult(request, recipe).GetRequired(ProcGenBlackboardKeys.BuildPlanKey);
        }
    }
}
