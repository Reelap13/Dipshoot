using Game.ProcGen.Warehouse;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.ProcGen
{
    public enum ProcGenDemoKind
    {
        AgentRuleBased,
        Evolutionary,
        WaveFunctionCollapse
    }

    public sealed class ProcGenDemoController : MonoBehaviour
    {
        [SerializeField] private ProcGenDemoKind _generator = ProcGenDemoKind.AgentRuleBased;
        [FormerlySerializedAs("_warehouseRecipe")]
        [SerializeField] private WarehouseRecipe _agentRuleBasedRecipe;
        [FormerlySerializedAs("_warehouseEvolutionRecipe")]
        [SerializeField] private WarehouseEvolutionRecipe _evolutionaryRecipe;
        [SerializeField] private WarehouseWfcRecipe _waveFunctionCollapseRecipe;
        [SerializeField] private Transform _agentRuleBasedOutputRoot;
        [SerializeField] private Transform _evolutionaryOutputRoot;
        [SerializeField] private Transform _waveFunctionCollapseOutputRoot;
        [SerializeField] private int _seed = 444;
        [SerializeField] private bool _randomizeSeedOnGenerate;
        [SerializeField] private bool _generateOnStart = true;
        [SerializeField] private bool _logDiagnostics = true;

        private readonly GenerationPipeline _pipeline = new();

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
                case ProcGenDemoKind.AgentRuleBased:
                    GenerateAgentRuleBased(seed);
                    break;
                case ProcGenDemoKind.Evolutionary:
                    GenerateEvolutionary(seed);
                    break;
                case ProcGenDemoKind.WaveFunctionCollapse:
                    GenerateWaveFunctionCollapse(seed);
                    break;
            }
        }

        public void ClearGenerated()
        {
            DestroyGenerated(_agentRuleBasedOutputRoot);
            DestroyGenerated(_evolutionaryOutputRoot);
            DestroyGenerated(_waveFunctionCollapseOutputRoot);
            DestroyDirectChild("GeneratedWarehouse");
            DestroyDirectChild("GeneratedWarehouseEvolution");
            DestroyDirectChild("GeneratedCity");
            DestroyDirectChild("GeneratedMap");
        }

        [ContextMenu("Ensure Algorithm Roots")]
        public void EnsureAlgorithmRoots()
        {
            _agentRuleBasedOutputRoot = GetOrCreateOutputRoot(_agentRuleBasedOutputRoot, "Agent Rule-Based");
            _evolutionaryOutputRoot = GetOrCreateOutputRoot(_evolutionaryOutputRoot, "Evolutionary");
            _waveFunctionCollapseOutputRoot = GetOrCreateOutputRoot(_waveFunctionCollapseOutputRoot, "Wave Function Collapse");
        }

        private void GenerateAgentRuleBased(int seed)
        {
            GenerateWarehouse(seed, GetOrCreateAgentRuleBasedRecipe(), ref _agentRuleBasedOutputRoot, "Agent Rule-Based");
        }

        private void GenerateEvolutionary(int seed)
        {
            GenerateWarehouse(seed, GetOrCreateEvolutionaryRecipe(), ref _evolutionaryOutputRoot, "Evolutionary");
        }

        private void GenerateWaveFunctionCollapse(int seed)
        {
            GenerateWarehouse(seed, GetOrCreateWaveFunctionCollapseRecipe(), ref _waveFunctionCollapseOutputRoot, "Wave Function Collapse");
        }

        private void GenerateWarehouse(int seed, WarehouseRecipe recipe, ref Transform outputRoot, string rootName)
        {
            outputRoot = GetOrCreateOutputRoot(outputRoot, rootName);
            GenerationRequest request = new(seed, true);
            GenerationResult result = _pipeline.Generate(request, recipe);
            LogDiagnostics(result);
            WarehouseBuildPlan plan = result.GetRequired(WarehouseKeys.BuildPlan);
            WarehousePlanExecutor.Execute(plan, outputRoot, recipe, "Generated", request.ClearPreviousOutput);
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

        private WarehouseRecipe GetOrCreateAgentRuleBasedRecipe()
        {
            if (_agentRuleBasedRecipe != null)
                return _agentRuleBasedRecipe;

            _agentRuleBasedRecipe = ScriptableObject.CreateInstance<WarehouseRecipe>();
            _agentRuleBasedRecipe.name = "RuntimeAgentRuleBasedRecipe";
            return _agentRuleBasedRecipe;
        }

        private WarehouseEvolutionRecipe GetOrCreateEvolutionaryRecipe()
        {
            if (_evolutionaryRecipe != null)
                return _evolutionaryRecipe;

            _evolutionaryRecipe = ScriptableObject.CreateInstance<WarehouseEvolutionRecipe>();
            _evolutionaryRecipe.name = "RuntimeEvolutionaryRecipe";
            return _evolutionaryRecipe;
        }

        private WarehouseWfcRecipe GetOrCreateWaveFunctionCollapseRecipe()
        {
            if (_waveFunctionCollapseRecipe != null)
                return _waveFunctionCollapseRecipe;

            _waveFunctionCollapseRecipe = ScriptableObject.CreateInstance<WarehouseWfcRecipe>();
            _waveFunctionCollapseRecipe.name = "RuntimeWaveFunctionCollapseRecipe";
            return _waveFunctionCollapseRecipe;
        }

        private Transform GetOrCreateOutputRoot(Transform current, string rootName)
        {
            if (current != null)
                return current;

            Transform existing = transform.Find(rootName);
            if (existing != null)
                return existing;

            Transform root = new GameObject(rootName).transform;
            root.SetParent(transform, false);
            return root;
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

        private static void DestroyGenerated(Transform outputRoot)
        {
            if (outputRoot == null)
                return;

            Transform child = outputRoot.Find("Generated");
            if (child == null)
                return;

            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }

        private void DestroyDirectChild(string childName)
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
