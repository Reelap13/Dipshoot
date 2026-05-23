using System.Collections.Generic;
using Game.MatchMode;
using Game.ProcGen.Chunked;
using UnityEngine;

namespace Game.Level
{
    public class LevelController : MonoBehaviour
    {
        [SerializeField] private List<Transform> _spawn_points;
        [SerializeField] private List<Transform> _red_spawn_points;
        [SerializeField] private List<Transform> _blue_spawn_points;
        [SerializeField] private Transform _capture_point;
        [SerializeField] private float _capture_marker_radius = 4f;
        private bool _terrainGenerated;

        public Transform CapturePoint => _capture_point == null ? transform : _capture_point;

        public Transform GetRandomSpawnPoint() =>
            _spawn_points != null && _spawn_points.Count != 0
                ? _spawn_points[Random.Range(0, _spawn_points.Count)]
                : transform;

        public Transform GetRandomSpawnPoint(TeamId team_id)
        {
            List<Transform> team_spawn_points = GetSpawnPoints(team_id);
            return team_spawn_points != null && team_spawn_points.Count != 0
                ? team_spawn_points[Random.Range(0, team_spawn_points.Count)]
                : GetRandomSpawnPoint();
        }

        private void Start()
        {
            if (_terrainGenerated)
                return;

            _terrainGenerated = true;
            //GetOrCreateTerrainGenerator().GenerateLevel();
            CreateCapturePointMarker();
        }

        public ChunkedTerrainLevelGenerator GetOrCreateTerrainGenerator()
        {
            ChunkedTerrainLevelGenerator generator = GetComponent<ChunkedTerrainLevelGenerator>();
            if (generator == null)
                generator = gameObject.AddComponent<ChunkedTerrainLevelGenerator>();

            return generator;
        }

        private void CreateCapturePointMarker()
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "CapturePointMarker";
            marker.transform.SetParent(CapturePoint, false);
            marker.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            marker.transform.localScale = new Vector3(_capture_marker_radius * 2f, 0.04f, _capture_marker_radius * 2f);

            if (marker.TryGetComponent(out Collider marker_collider))
                Destroy(marker_collider);

            if (marker.TryGetComponent(out Renderer marker_renderer))
                marker_renderer.material.color = new Color(0.7f, 0.7f, 0.7f, 0.35f);
        }

        private List<Transform> GetSpawnPoints(TeamId team_id)
        {
            if (team_id == TeamId.Red)
                return _red_spawn_points;

            if (team_id == TeamId.Blue)
                return _blue_spawn_points;

            return _spawn_points;
        }
    }
}
