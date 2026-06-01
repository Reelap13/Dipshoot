using UnityEngine;

namespace Game.ProcGen.Warehouse
{
    public sealed class WarehouseLevelGenerator : MonoBehaviour
    {
        [SerializeField] private WarehouseRecipe _recipe;
        [SerializeField] private int _seed = 12345;
        [SerializeField] private bool _randomizeSeedOnStart;
        [SerializeField] private string _generatedRootName = "GeneratedWarehouse";
        [SerializeField] private bool _generateOnStart = true;
        [SerializeField] private bool _logDiagnostics = true;

        private readonly GenerationPipeline _pipeline = new();

        public WarehouseRecipe Recipe => _recipe;

        private void Start()
        {
            if (_generateOnStart)
                GenerateLevel();
        }

        public void GenerateLevel()
        {
            WarehouseRecipe recipe = GetOrCreateRecipe();
            int seed = GetSeed();
            GenerationRequest request = new(seed, true);
            GenerationResult result = _pipeline.Generate(request, recipe);

            if (_logDiagnostics)
                LogDiagnostics(result);

            WarehouseBuildPlan plan = result.GetRequired(WarehouseKeys.BuildPlan);
            WarehousePlanExecutor.Execute(plan, transform, recipe, _generatedRootName, request.ClearPreviousOutput);
        }

        private int GetSeed()
        {
            if (!_randomizeSeedOnStart)
                return _seed;

            unchecked
            {
                return (int)System.DateTime.UtcNow.Ticks ^ GetInstanceID() * 397;
            }
        }

        private WarehouseRecipe GetOrCreateRecipe()
        {
            if (_recipe != null)
                return _recipe;

            _recipe = ScriptableObject.CreateInstance<WarehouseRecipe>();
            _recipe.name = "RuntimeWarehouseRecipe";
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
