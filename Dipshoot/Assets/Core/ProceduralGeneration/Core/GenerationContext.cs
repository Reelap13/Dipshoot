using System;

namespace Game.ProcGen
{
    public sealed class GenerationContext
    {
        public GenerationContext(GenerationRequest request, ProcGenRecipe recipe, GenerationBlackboard blackboard)
            : this(request, recipe, blackboard, new GenerationDiagnostics())
        {
        }

        public GenerationContext(GenerationRequest request, ProcGenRecipe recipe, GenerationBlackboard blackboard, GenerationDiagnostics diagnostics)
        {
            Request = request;
            Recipe = recipe ?? throw new ArgumentNullException(nameof(recipe));
            Blackboard = blackboard ?? throw new ArgumentNullException(nameof(blackboard));
            Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        }

        public GenerationRequest Request { get; }

        public ProcGenRecipe Recipe { get; }

        public GenerationBlackboard Blackboard { get; }

        public GenerationDiagnostics Diagnostics { get; }

        public TRecipe GetRecipe<TRecipe>() where TRecipe : ProcGenRecipe
        {
            if (Recipe is TRecipe typedRecipe)
                return typedRecipe;

            throw new InvalidOperationException($"Expected recipe of type {typeof(TRecipe).Name}, but got {Recipe.GetType().Name}.");
        }

        public System.Random CreateRandom(string streamId)
        {
            return GenerationRandom.Create(Request.Seed, streamId);
        }
    }
}
