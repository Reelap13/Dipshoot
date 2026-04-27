using System.Collections.Generic;
using Game.ProcGen.Chunked;
using UnityEngine;

namespace Game.Level
{
    public class LevelController : MonoBehaviour
    {
        [SerializeField] private List<Transform> _spawn_points;
        private bool _terrainGenerated;

        public Transform GetRandomSpawnPoint() => _spawn_points.Count != 0 ? _spawn_points[Random.Range(0, _spawn_points.Count)] : transform;

        private void Start()
        {
            if (_terrainGenerated)
                return;

            _terrainGenerated = true;
            GetOrCreateTerrainGenerator().GenerateLevel();
        }

        public ChunkedTerrainLevelGenerator GetOrCreateTerrainGenerator()
        {
            ChunkedTerrainLevelGenerator generator = GetComponent<ChunkedTerrainLevelGenerator>();
            if (generator == null)
                generator = gameObject.AddComponent<ChunkedTerrainLevelGenerator>();

            return generator;
        }
    }
}
