using System.Collections.Generic;
using UnityEngine;

namespace Game.ProcGen.Chunked
{
    [CreateAssetMenu(menuName = "Dipshoot/ProcGen/Chunked Terrain Recipe", fileName = "ChunkedTerrainRecipe")]
    public sealed class ChunkedTerrainRecipe : ProcGenRecipe
    {
        [Header("Chunk Layout")]
        [SerializeField] private Vector2Int _chunkGridSize = new Vector2Int(5, 5);
        [SerializeField] private Vector2 _chunkSize = new Vector2(16f, 16f);
        [SerializeField] private int _heightSamplesPerAxis = 17;

        [Header("Height Noise")]
        [SerializeField] private float _noiseScale = 48f;
        [SerializeField] private float _heightMultiplier = 8f;
        [SerializeField] private int _octaves = 3;
        [SerializeField] private float _persistence = 0.5f;
        [SerializeField] private float _lacunarity = 2f;
        [SerializeField] private Vector2 _noiseOffset = new Vector2(1000f, 1000f);

        [Header("Props")]
        [SerializeField] private int _minTreesPerChunk = 3;
        [SerializeField] private int _maxTreesPerChunk = 9;
        [SerializeField] private float _treePlacementPadding = 1.5f;
        [SerializeField] private float _maxTreeSlope = 1.25f;
        [SerializeField] private Vector2 _treeScaleRange = new Vector2(0.85f, 1.35f);
        [SerializeField] private List<GameObject> _treeCandidates = new List<GameObject>();

        [Header("Rendering")]
        [SerializeField] private Material _terrainMaterial;

        public Vector2Int ChunkGridSize => new Vector2Int(Mathf.Max(1, _chunkGridSize.x), Mathf.Max(1, _chunkGridSize.y));

        public Vector2 ChunkSize => new Vector2(Mathf.Max(1f, _chunkSize.x), Mathf.Max(1f, _chunkSize.y));

        public int HeightSamplesPerAxis => Mathf.Max(2, _heightSamplesPerAxis);

        public float NoiseScale => Mathf.Max(0.001f, _noiseScale);

        public float HeightMultiplier => _heightMultiplier;

        public int Octaves => Mathf.Max(1, _octaves);

        public float Persistence => Mathf.Clamp01(_persistence);

        public float Lacunarity => Mathf.Max(1f, _lacunarity);

        public Vector2 NoiseOffset => _noiseOffset;

        public int MinTreesPerChunk => Mathf.Max(0, Mathf.Min(_minTreesPerChunk, _maxTreesPerChunk));

        public int MaxTreesPerChunk => Mathf.Max(_minTreesPerChunk, _maxTreesPerChunk);

        public float TreePlacementPadding => Mathf.Clamp(_treePlacementPadding, 0f, Mathf.Min(_chunkSize.x, _chunkSize.y) * 0.45f);

        public float MaxTreeSlope => Mathf.Max(0.05f, _maxTreeSlope);

        public Vector2 TreeScaleRange => new Vector2(
            Mathf.Max(0.1f, Mathf.Min(_treeScaleRange.x, _treeScaleRange.y)),
            Mathf.Max(0.1f, Mathf.Max(_treeScaleRange.x, _treeScaleRange.y)));

        public IReadOnlyList<GameObject> TreeCandidates => _treeCandidates;

        public Material TerrainMaterial => _terrainMaterial;

        public override void BuildPasses(List<ProcGenPass> passes)
        {
            passes.Add(new ChunkLayoutPass());
            passes.Add(new PerlinHeightPass());
            passes.Add(new TreeScatterPass());
            passes.Add(new ChunkBuildPlanPass());
        }
    }
}
