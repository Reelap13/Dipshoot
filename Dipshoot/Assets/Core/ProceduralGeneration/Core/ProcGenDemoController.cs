using Game.ProcGen.Chunked;
using Game.ProcGen.City;
using Game.ProcGen.Warehouse;
using UnityEngine;

namespace Game.ProcGen
{
    public enum ProcGenDemoKind
    {
        City,
        ChunkedTerrain,
        Warehouse,
        WarehouseEvolution
    }

    public sealed class ProcGenDemoController : MonoBehaviour
    {
        [SerializeField] private ProcGenDemoKind _generator = ProcGenDemoKind.Warehouse;
        [SerializeField] private CityRecipe _cityRecipe;
        [SerializeField] private ChunkedTerrainRecipe _chunkedTerrainRecipe;
        [SerializeField] private WarehouseRecipe _warehouseRecipe;
        [SerializeField] private WarehouseEvolutionRecipe _warehouseEvolutionRecipe;
        [SerializeField] private int _seed = 444;
        [SerializeField] private bool _randomizeSeedOnGenerate;
        [SerializeField] private bool _generateOnStart = true;
        [SerializeField] private bool _logDiagnostics = true;

        private readonly GenerationPipeline _pipeline = new();
        private readonly PipelineMapGenerator _mapGenerator = new();

        private void Start()
        {
            if (_generateOnStart)
                Generate();
        }

        public void Generate()
        {
            int seed = ResolveSeed();
            switch (_generator)
            {
                case ProcGenDemoKind.City:
                    GenerateCity(seed);
                    break;
                case ProcGenDemoKind.ChunkedTerrain:
                    GenerateChunkedTerrain(seed);
                    break;
                case ProcGenDemoKind.Warehouse:
                    GenerateWarehouse(seed);
                    break;
                case ProcGenDemoKind.WarehouseEvolution:
                    GenerateWarehouseEvolution(seed);
                    break;
            }
        }

        public void ClearGenerated()
        {
            DestroyChild("GeneratedCity");
            DestroyChild("GeneratedMap");
            DestroyChild("GeneratedWarehouse");
            DestroyChild("GeneratedWarehouseEvolution");
        }

        private void GenerateCity(int seed)
        {
            CityRecipe recipe = GetOrCreateCityRecipe();
            GenerationRequest request = new(seed, true);
            GenerationResult result = _pipeline.Generate(request, recipe);
            LogDiagnostics(result);
            CityBuildPlan plan = result.GetRequired(CityKeys.BuildPlan);
            CityPlanExecutor.Execute(plan, transform, recipe, "GeneratedCity", request.ClearPreviousOutput);
        }

        private void GenerateChunkedTerrain(int seed)
        {
            ChunkedTerrainRecipe recipe = GetOrCreateChunkedTerrainRecipe();
            GenerationRequest request = new(seed, true);
            MapBuildPlan plan = _mapGenerator.Generate(request, recipe);
            ChunkedTerrainPlanExecutor.Execute(plan, transform, recipe, "GeneratedMap", request.ClearPreviousOutput);
        }

        private void GenerateWarehouse(int seed)
        {
            WarehouseRecipe recipe = GetOrCreateWarehouseRecipe();
            GenerationRequest request = new(seed, true);
            GenerationResult result = _pipeline.Generate(request, recipe);
            LogDiagnostics(result);
            WarehouseBuildPlan plan = result.GetRequired(WarehouseKeys.BuildPlan);
            WarehousePlanExecutor.Execute(plan, transform, recipe, "GeneratedWarehouse", request.ClearPreviousOutput);
        }

        private void GenerateWarehouseEvolution(int seed)
        {
            WarehouseEvolutionRecipe recipe = GetOrCreateWarehouseEvolutionRecipe();
            GenerationRequest request = new(seed, true);
            GenerationResult result = _pipeline.Generate(request, recipe);
            LogDiagnostics(result);
            WarehouseBuildPlan plan = result.GetRequired(WarehouseKeys.BuildPlan);
            WarehousePlanExecutor.Execute(plan, transform, recipe, "GeneratedWarehouseEvolution", request.ClearPreviousOutput);
        }

        private int ResolveSeed()
        {
            if (!_randomizeSeedOnGenerate)
                return _seed;

            unchecked
            {
                _seed = (int)System.DateTime.UtcNow.Ticks ^ GetInstanceID() * 397;
                return _seed;
            }
        }

        private CityRecipe GetOrCreateCityRecipe()
        {
            if (_cityRecipe != null)
                return _cityRecipe;

            _cityRecipe = ScriptableObject.CreateInstance<CityRecipe>();
            _cityRecipe.name = "RuntimeCityRecipe";
            return _cityRecipe;
        }

        private ChunkedTerrainRecipe GetOrCreateChunkedTerrainRecipe()
        {
            if (_chunkedTerrainRecipe != null)
                return _chunkedTerrainRecipe;

            _chunkedTerrainRecipe = ScriptableObject.CreateInstance<ChunkedTerrainRecipe>();
            _chunkedTerrainRecipe.name = "RuntimeChunkedTerrainRecipe";
            return _chunkedTerrainRecipe;
        }

        private WarehouseRecipe GetOrCreateWarehouseRecipe()
        {
            if (_warehouseRecipe != null)
                return _warehouseRecipe;

            _warehouseRecipe = ScriptableObject.CreateInstance<WarehouseRecipe>();
            _warehouseRecipe.name = "RuntimeWarehouseRecipe";
            return _warehouseRecipe;
        }

        private WarehouseEvolutionRecipe GetOrCreateWarehouseEvolutionRecipe()
        {
            if (_warehouseEvolutionRecipe != null)
                return _warehouseEvolutionRecipe;

            _warehouseEvolutionRecipe = ScriptableObject.CreateInstance<WarehouseEvolutionRecipe>();
            _warehouseEvolutionRecipe.name = "RuntimeWarehouseEvolutionRecipe";
            return _warehouseEvolutionRecipe;
        }

        private void LogDiagnostics(GenerationResult result)
        {
            if (!_logDiagnostics)
                return;

            for (int i = 0; i < result.Diagnostics.Count; i++)
            {
                GenerationDiagnostic diagnostic = result.Diagnostics[i];
                if (diagnostic.Severity == GenerationDiagnosticSeverity.Error)
                    Debug.LogError(diagnostic.ToString(), this);
                else if (diagnostic.Severity == GenerationDiagnosticSeverity.Warning)
                    Debug.LogWarning(diagnostic.ToString(), this);
                else
                    Debug.Log(diagnostic.ToString(), this);
            }
        }

        private void DestroyChild(string childName)
        {
            Transform child = transform.Find(childName);
            if (child == null)
                return;

            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
    }
}
