using System.Collections.Generic;
using Game.MatchMode;
using Game.MatchConfig;
using Game.ProcGen.Chunked;
using Game.ProcGen.Warehouse;
using UnityEngine;

namespace Game.Level
{
    public class LevelController : MonoBehaviour
    {
        private const string LogPrefix = "[LevelController]";

        [SerializeField] private List<Transform> _spawn_points;
        [SerializeField] private List<Transform> _red_spawn_points;
        [SerializeField] private List<Transform> _blue_spawn_points;
        [SerializeField] private Transform _capture_point;
        [SerializeField] private float _spawn_marker_radius = 2f;
        [SerializeField] private float _capture_marker_radius = 4f;
        private bool _terrainGenerated;
        private bool _staticLayoutDisabled;
        private bool _missing_spawn_points_logged;

        public Transform CapturePoint => _capture_point == null ? transform : _capture_point;
        public IReadOnlyList<Transform> RedSpawnPoints => _red_spawn_points;
        public IReadOnlyList<Transform> BlueSpawnPoints => _blue_spawn_points;
        public float SpawnMarkerRadius => Mathf.Max(0.1f, _spawn_marker_radius);

        public Transform GetRandomSpawnPoint()
        {
            if (_spawn_points != null && _spawn_points.Count != 0)
                return _spawn_points[Random.Range(0, _spawn_points.Count)];

            LogMissingSpawnPoints(TeamId.None);
            return transform;
        }

        public Transform GetRandomSpawnPoint(TeamId team_id)
        {
            List<Transform> team_spawn_points = GetSpawnPoints(team_id);
            if (team_spawn_points != null && team_spawn_points.Count != 0)
                return team_spawn_points[Random.Range(0, team_spawn_points.Count)];

            LogMissingSpawnPoints(team_id);
            return GetRandomSpawnPoint();
        }

        public void ConfigureGeneratedLevel(
            List<Transform> spawn_points,
            List<Transform> red_spawn_points,
            List<Transform> blue_spawn_points,
            Transform capture_point,
            float spawn_marker_radius,
            float capture_marker_radius)
        {
            _spawn_points = spawn_points;
            _red_spawn_points = red_spawn_points;
            _blue_spawn_points = blue_spawn_points;
            _capture_point = capture_point;
            _spawn_marker_radius = spawn_marker_radius;
            _capture_marker_radius = capture_marker_radius;
            HideSpawnMarkerRenderers();
        }

        public void GenerateWarehouseLevel(WarehouseRecipe recipe, int seed)
        {
            if (recipe == null)
                return;

            SetStaticLayoutActive(false);
            WarehouseLevelGenerator.GenerateInto(transform, recipe, seed, "GeneratedWarehouse", true);
            _terrainGenerated = true;
        }

        private void Start()
        {
            if (ClientMatchPresetState.UsesGeneratedLevel)
            {
                SetStaticLayoutActive(false);
                _terrainGenerated = true;
            }

            if (_terrainGenerated)
            {
                HideSpawnMarkerRenderers();
                return;
            }

            _terrainGenerated = true;
            //GetOrCreateTerrainGenerator().GenerateLevel();
            CreateCapturePointMarker();
            HideSpawnMarkerRenderers();
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
            if (CapturePoint.Find("CapturePointMarker") != null)
                return;

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

        private void LogMissingSpawnPoints(TeamId team_id)
        {
            if (_missing_spawn_points_logged)
                return;

            _missing_spawn_points_logged = true;
            Debug.LogError($"{LogPrefix} Missing spawn points. team={team_id} level={name}");
        }

        private void SetStaticLayoutActive(bool active)
        {
            if (_staticLayoutDisabled == !active)
                return;

            _staticLayoutDisabled = !active;
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child.name == "GeneratedWarehouse")
                    continue;

                child.gameObject.SetActive(active);
            }
        }

        private void HideSpawnMarkerRenderers()
        {
            HideMarkerRenderers(_spawn_points);
            HideMarkerRenderers(_red_spawn_points);
            HideMarkerRenderers(_blue_spawn_points);
        }

        private static void HideMarkerRenderers(List<Transform> markers)
        {
            if (markers == null)
                return;

            for (int i = 0; i < markers.Count; i++)
            {
                Transform marker = markers[i];
                if (marker == null)
                    continue;

                Renderer[] renderers = marker.GetComponentsInChildren<Renderer>(true);
                for (int j = 0; j < renderers.Length; j++)
                    if (renderers[j] != null)
                        renderers[j].enabled = false;
            }
        }
    }
}
