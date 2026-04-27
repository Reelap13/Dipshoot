using System.Collections.Generic;

namespace Game.ProcGen
{
    public interface IMapGenerator
    {
        MapBuildPlan Generate(GenerationRequest request, ProcGenRecipe recipe);
    }

    public sealed class PipelineMapGenerator : IMapGenerator
    {
        public MapBuildPlan Generate(GenerationRequest request, ProcGenRecipe recipe)
        {
            GenerationBlackboard blackboard = new GenerationBlackboard();
            GenerationContext context = new GenerationContext(request, recipe, blackboard);
            List<ProcGenPass> passes = new List<ProcGenPass>();
            recipe.BuildPasses(passes);

            for (int i = 0; i < passes.Count; i++)
            {
                ProcGenPass pass = passes[i];
                GenerationPassContract contract = new GenerationPassContract(pass.Backend);
                pass.Declare(contract);
                pass.Execute(context);
            }

            return blackboard.GetRequired<MapBuildPlan>(ProcGenBlackboardKeys.BuildPlan);
        }
    }
}
