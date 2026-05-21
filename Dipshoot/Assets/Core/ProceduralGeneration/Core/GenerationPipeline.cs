using System;
using System.Collections.Generic;

namespace Game.ProcGen
{
    public interface IGenerationPipeline
    {
        GenerationResult Generate(GenerationRequest request, ProcGenRecipe recipe);
    }

    public sealed class GenerationPipeline : IGenerationPipeline
    {
        public GenerationResult Generate(GenerationRequest request, ProcGenRecipe recipe)
        {
            if (recipe == null)
                throw new ArgumentNullException(nameof(recipe));

            List<ProcGenPass> passes = new List<ProcGenPass>();
            recipe.BuildPasses(passes);

            List<GenerationPassContract> contracts = BuildContracts(passes);
            ValidateContracts(passes, contracts);

            GenerationBlackboard blackboard = new GenerationBlackboard();
            GenerationDiagnostics diagnostics = new GenerationDiagnostics();
            GenerationContext context = new GenerationContext(request, recipe, blackboard, diagnostics);

            for (int i = 0; i < passes.Count; i++)
            {
                ProcGenPass pass = passes[i];
                try
                {
                    pass.Execute(context);
                }
                catch (Exception exception)
                {
                    throw new InvalidOperationException($"Generation pass '{pass.Id}' failed.", exception);
                }
            }

            return new GenerationResult(request, recipe, blackboard.CreateSnapshot(), diagnostics.CreateSnapshot());
        }

        private static List<GenerationPassContract> BuildContracts(IReadOnlyList<ProcGenPass> passes)
        {
            List<GenerationPassContract> contracts = new List<GenerationPassContract>(passes.Count);

            for (int i = 0; i < passes.Count; i++)
            {
                ProcGenPass pass = passes[i];
                if (pass == null)
                    throw new InvalidOperationException($"Generation recipe contains a null pass at index {i}.");

                GenerationPassContract contract = new GenerationPassContract(pass.Backend);
                pass.Declare(contract);
                contracts.Add(contract);
            }

            return contracts;
        }

        private static void ValidateContracts(IReadOnlyList<ProcGenPass> passes, IReadOnlyList<GenerationPassContract> contracts)
        {
            HashSet<string> passIds = new HashSet<string>();
            HashSet<string> availableKeys = new HashSet<string>();

            for (int i = 0; i < passes.Count; i++)
            {
                ProcGenPass pass = passes[i];
                GenerationPassContract contract = contracts[i];

                if (string.IsNullOrWhiteSpace(pass.Id))
                    throw new InvalidOperationException($"Generation pass at index {i} has an empty id.");

                if (passIds.Add(pass.Id) == false)
                    throw new InvalidOperationException($"Generation pass id '{pass.Id}' is duplicated.");

                for (int readIndex = 0; readIndex < contract.Reads.Count; readIndex++)
                {
                    string key = contract.Reads[readIndex];
                    if (availableKeys.Contains(key) == false)
                        throw new InvalidOperationException($"Generation pass '{pass.Id}' reads key '{key}' before any previous pass writes it.");
                }

                for (int writeIndex = 0; writeIndex < contract.Writes.Count; writeIndex++)
                    availableKeys.Add(contract.Writes[writeIndex]);
            }
        }
    }
}
