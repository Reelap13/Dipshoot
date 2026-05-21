using UnityEngine;

namespace Game.ProcGen.City
{
    public sealed class CityLevelGenerator : MonoBehaviour
    {
        [SerializeField] private CityRecipe _recipe;
        [SerializeField] private int _seed = 12345;
        [SerializeField] private bool _randomizeSeedOnStart;
        [SerializeField] private string _generatedRootName = "GeneratedCity";
        [SerializeField] private bool _generateOnStart = true;
        [SerializeField] private bool _logDiagnostics = true;

        private readonly GenerationPipeline _pipeline = new GenerationPipeline();

        public CityRecipe Recipe => _recipe;

        private void Start()
        {
            if (_generateOnStart)
                GenerateLevel();
        }

        public void GenerateLevel()
        {
            CityRecipe recipe = GetOrCreateRecipe();
            int seed = GetSeed();
            GenerationRequest request = new GenerationRequest(seed, true);
            GenerationResult result = _pipeline.Generate(request, recipe);

            if (_logDiagnostics)
            {
                Debug.Log($"City generation seed: {seed}");
                LogDiagnostics(result);
            }

            CityBuildPlan plan = result.GetRequired(CityKeys.BuildPlan);
            CityPlanExecutor.Execute(plan, transform, recipe, _generatedRootName, request.ClearPreviousOutput);
        }

        private int GetSeed()
        {
            if (_randomizeSeedOnStart == false)
                return _seed;

            unchecked
            {
                int timeHash = (int)System.DateTime.UtcNow.Ticks;
                int objectHash = GetInstanceID() * 397;
                return timeHash ^ objectHash;
            }
        }

        private CityRecipe GetOrCreateRecipe()
        {
            if (_recipe != null)
                return _recipe;

            _recipe = ScriptableObject.CreateInstance<CityRecipe>();
            _recipe.name = "RuntimeCityRecipe";
            return _recipe;
        }

        private static void LogDiagnostics(GenerationResult result)
        {
            for (int i = 0; i < result.Diagnostics.Count; i++)
            {
                GenerationDiagnostic diagnostic = result.Diagnostics[i];
                if (diagnostic.Severity == GenerationDiagnosticSeverity.Error)
                    Debug.LogError(diagnostic.ToString());
                else if (diagnostic.Severity == GenerationDiagnosticSeverity.Warning)
                    Debug.LogWarning(diagnostic.ToString());
                else
                    Debug.Log(diagnostic.ToString());
            }
        }
    }
}
