using System.Collections.Generic;
using UnityEngine;

namespace Game.Level
{
    public class LevelController : MonoBehaviour
    {
        [SerializeField] private List<Transform> _spawn_points;

        public Transform GetRandomSpawnPoint() => _spawn_points.Count != 0 ? _spawn_points[Random.Range(0, _spawn_points.Count)] : transform;
    }
}