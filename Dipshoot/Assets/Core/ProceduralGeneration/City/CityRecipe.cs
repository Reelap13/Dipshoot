using System.Collections.Generic;
using UnityEngine;

namespace Game.ProcGen.City
{
    [CreateAssetMenu(menuName = "Dipshoot/ProcGen/City Recipe", fileName = "CityRecipe")]
    public sealed class CityRecipe : ProcGenRecipe
    {
        [Header("Surface")]
        [SerializeField] private Vector2 _mapSize = new Vector2(120f, 90f);
        [SerializeField] private float _waterwayWidth = 8f;

        [Header("Zones")]
        [SerializeField] private Vector2Int _zoneGridSize = new Vector2Int(3, 3);
        [SerializeField] private float _zonePadding = 2f;

        [Header("Roads")]
        [SerializeField] private float _mainRoadWidth = 5f;
        [SerializeField] private float _localRoadWidth = 2f;
        [SerializeField] private float _localRoadSpacing = 18f;

        [Header("Architecture")]
        [SerializeField] private float _buildingSetback = 1.5f;
        [SerializeField] private Vector2 _buildingHeightRange = new Vector2(3f, 12f);
        [SerializeField] private float _wallHeight = 4f;
        [SerializeField] private float _towerHeight = 10f;

        [Header("Gameplay")]
        [SerializeField] private int _enemyMarkersPerDangerZone = 4;
        [SerializeField] private int _lootMarkersPerZone = 2;

        [Header("Visual Details")]
        [SerializeField] private float _detailSpacing = 12f;
        [SerializeField] private float _detailDensity = 1f;

        public Vector2 MapSize => new Vector2(Mathf.Max(30f, _mapSize.x), Mathf.Max(30f, _mapSize.y));

        public float WaterwayWidth => Mathf.Clamp(_waterwayWidth, 0f, MapSize.y * 0.3f);

        public Vector2Int ZoneGridSize => new Vector2Int(Mathf.Max(1, _zoneGridSize.x), Mathf.Max(1, _zoneGridSize.y));

        public float ZonePadding => Mathf.Max(0f, _zonePadding);

        public float MainRoadWidth => Mathf.Max(1f, _mainRoadWidth);

        public float LocalRoadWidth => Mathf.Max(0.5f, _localRoadWidth);

        public float LocalRoadSpacing => Mathf.Max(8f, _localRoadSpacing);

        public float BuildingSetback => Mathf.Max(0.25f, _buildingSetback);

        public Vector2 BuildingHeightRange => new Vector2(
            Mathf.Max(1f, Mathf.Min(_buildingHeightRange.x, _buildingHeightRange.y)),
            Mathf.Max(1f, Mathf.Max(_buildingHeightRange.x, _buildingHeightRange.y)));

        public float WallHeight => Mathf.Max(1f, _wallHeight);

        public float TowerHeight => Mathf.Max(WallHeight, _towerHeight);

        public int EnemyMarkersPerDangerZone => Mathf.Max(0, _enemyMarkersPerDangerZone);

        public int LootMarkersPerZone => Mathf.Max(0, _lootMarkersPerZone);

        public float DetailSpacing => Mathf.Max(4f, _detailSpacing);

        public float DetailDensity => Mathf.Max(0f, _detailDensity);

        public override void BuildPasses(List<ProcGenPass> passes)
        {
            passes.Add(new CitySurfacePass());
            passes.Add(new CityZonePass());
            passes.Add(new CityGlobalSkeletonPass());
            passes.Add(new CityLocalStructurePass());
            passes.Add(new CityArchitecturePass());
            passes.Add(new CityGameplayPass());
            passes.Add(new CityVisualDetailPass());
            passes.Add(new CityBuildPlanPass());
        }
    }
}
