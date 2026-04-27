using System;

namespace Game.ProcGen
{
    public sealed class GenerationContext
    {
        public GenerationContext(GenerationRequest request, ProcGenRecipe recipe, GenerationBlackboard blackboard)
        {
            Request = request;
            Recipe = recipe;
            Blackboard = blackboard;
        }

        public GenerationRequest Request { get; }

        public ProcGenRecipe Recipe { get; }

        public GenerationBlackboard Blackboard { get; }

        public TRecipe GetRecipe<TRecipe>() where TRecipe : ProcGenRecipe
        {
            if (Recipe is TRecipe typedRecipe)
                return typedRecipe;

            throw new InvalidOperationException($"Expected recipe of type {typeof(TRecipe).Name}, but got {Recipe.GetType().Name}.");
        }
    }
}
