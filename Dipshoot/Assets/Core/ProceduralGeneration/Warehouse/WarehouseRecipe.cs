using System.Collections.Generic;
using UnityEngine;

namespace Game.ProcGen.Warehouse
{
    public enum WarehouseSymmetryMode
    {
        MirroredHalf,
        IndependentHalves
    }

    [System.Serializable]
    public sealed class WarehousePrefabVariant
    {
        public GameObject Prefab;
        public float Weight = 1f;
    }

    [CreateAssetMenu(menuName = "Dipshoot/ProcGen/Warehouse Recipe", fileName = "WarehouseRecipe")]
    public sealed class WarehouseRecipe : ProcGenRecipe
    {
        [Header("Map")]
        public int MapWidth = 15;
        public int MapHeight = 30;
        public float CellSize = 3f;

        [Header("Fixed Points")]
        public Vector2 SpawnAPosition = new Vector2(7.5f, 1f);
        public Vector2 SpawnBPosition = new Vector2(7.5f, 29f);
        public Vector2 CapturePointPosition = new Vector2(7.5f, 15f);

        [Header("Clear Zones")]
        public float SpawnClearRadius = 2f;
        public float CaptureClearRadius = 3f;

        [Header("Generation")]
        public WarehouseSymmetryMode SymmetryMode = WarehouseSymmetryMode.MirroredHalf;
        public float InitialStructureCellRatio = 0.4f;
        public float TargetStructureCellRatio = 0.55f;
        public float MinStructureCellRatio = 0.4f;
        public float MaxStructureCellRatio = 0.7f;
        public float TargetCoverCellRatio = 0.12f;
        public int StructurePlacementAttempts = 1200;
        public int CoverPlacementAttempts = 700;
        public float HighContainerChance = 0.15f;
        public float BridgeChance = 0.25f;
        public int BridgePlacementAttempts = 300;
        public int MaxBridgeCellsPerHalf = 6;
        public int MaxBridgePairCountPerHalf = 2;
        public int MaxBridgeBlockedContainerSides = 3;
        public float TopCoverChance = 0.45f;
        public float FullCoverChance = 0.45f;
        public int StructureCellsPerLadder = 8;
        public int ExtraCoverPairs = 0;
        public float StructureDensityPenalty = 10f;
        public int MinTopComponentSizeForLadder = 2;
        public float CoverClusterRadius = 2f;
        public int MaxCoverNeighbors = 3;
        public float CoverClusterPenalty = 50f;

        [Header("Gameplay")]
        public float MaxPathCostDifference = 8f;

        [Header("Prefab Metrics")]
        public float ContainerLowHeight = 2.7f;
        public float ContainerHighHeight = 5.4f;
        public float BridgeHeight = 0.25f;

        [Header("Prefabs")]
        public GameObject ContainerLowPrefab;
        public GameObject ContainerHighPrefab;
        public GameObject BridgePrefab;
        public GameObject LadderPrefab;
        public GameObject PartialCoverPrefab;
        public GameObject FullCoverPrefab;
        public GameObject FloorPrefab;
        public GameObject WallPrefab;
        public GameObject SpawnMarkerPrefab;
        public GameObject CapturePointMarkerPrefab;

        [Header("Cover Variants")]
        public WarehousePrefabVariant[] PartialCoverVariants;
        public WarehousePrefabVariant[] FullCoverVariants;

        public int Width => Mathf.Max(3, MapWidth);
        public int Height => Mathf.Max(6, MapHeight);
        public float GridCellSize => Mathf.Max(0.5f, CellSize);
        public int HalfHeight => Height / 2;
        public float InitialStructureRatio => Mathf.Clamp01(InitialStructureCellRatio);
        public int ExtraCoverPairCount => Mathf.Max(0, ExtraCoverPairs);
        public float TargetCoverRatio => Mathf.Clamp01(TargetCoverCellRatio);
        public int StructurePlacementAttemptCount => Mathf.Max(1, StructurePlacementAttempts);
        public int CoverPlacementAttemptCount => Mathf.Max(1, CoverPlacementAttempts);
        public float TallContainerProbability => Mathf.Clamp01(HighContainerChance);
        public float BridgeProbability => Mathf.Clamp01(BridgeChance);
        public int BridgePlacementAttemptCount => Mathf.Max(1, BridgePlacementAttempts);
        public int MaxBridgeCellCount => Mathf.Max(0, MaxBridgeCellsPerHalf);
        public int MaxBridgePairCount => Mathf.Max(0, MaxBridgePairCountPerHalf);
        public int MaxBridgeBlockedSides => Mathf.Clamp(MaxBridgeBlockedContainerSides, 0, 4);
        public float TopCoverProbability => Mathf.Clamp01(TopCoverChance);
        public float FullCoverProbability => Mathf.Clamp01(FullCoverChance);
        public int LadderSpacing => Mathf.Max(3, StructureCellsPerLadder);
        public float LowContainerTopHeight => Mathf.Max(0f, ContainerLowHeight);
        public float HighContainerTopHeight => Mathf.Max(0f, ContainerHighHeight);
        public float BridgeTopHeight => LowContainerTopHeight + Mathf.Max(0f, BridgeHeight);
        public float AllowedPathCostDifference => Mathf.Max(0f, MaxPathCostDifference);
        public float TargetStructureRatio => Mathf.Clamp01(TargetStructureCellRatio);
        public float MinStructureRatio => Mathf.Clamp01(Mathf.Min(MinStructureCellRatio, MaxStructureCellRatio));
        public float MaxStructureRatio => Mathf.Clamp01(Mathf.Max(MinStructureCellRatio, MaxStructureCellRatio));
        public float StructureDensityPenaltyWeight => Mathf.Max(0f, StructureDensityPenalty);
        public int MinTopComponentSize => Mathf.Max(1, MinTopComponentSizeForLadder);
        public float CoverClusterRadiusValue => Mathf.Max(0f, CoverClusterRadius);
        public int MaxCoverNeighborCount => Mathf.Max(0, MaxCoverNeighbors);
        public float CoverClusterPenaltyWeight => Mathf.Max(0f, CoverClusterPenalty);

        public GameObject GetPrefab(WarehouseObjectKind kind)
        {
            return kind switch
            {
                WarehouseObjectKind.ContainerLow => ContainerLowPrefab != null ? ContainerLowPrefab : LoadPrefab("ContainerLow"),
                WarehouseObjectKind.ContainerHigh => ContainerHighPrefab != null ? ContainerHighPrefab : LoadPrefab("ContainerHigh"),
                WarehouseObjectKind.Bridge => BridgePrefab != null ? BridgePrefab : LoadPrefab("Bridge"),
                WarehouseObjectKind.Ladder => LadderPrefab != null ? LadderPrefab : LoadPrefab("Ladder"),
                WarehouseObjectKind.PartialCover => PartialCoverPrefab != null ? PartialCoverPrefab : LoadPrefab("PartialCover"),
                WarehouseObjectKind.FullCover => FullCoverPrefab != null ? FullCoverPrefab : LoadPrefab("FullCover"),
                _ => null
            };
        }

        public GameObject GetPrefab(WarehouseObjectKind kind, int variantIndex)
        {
            WarehousePrefabVariant[] variants = GetCoverVariants(kind);
            if (variants != null &&
                variantIndex >= 0 &&
                variantIndex < variants.Length &&
                variants[variantIndex] != null &&
                variants[variantIndex].Prefab != null)
            {
                return variants[variantIndex].Prefab;
            }

            return GetPrefab(kind);
        }

        public WarehousePrefabVariant[] GetCoverVariants(WarehouseObjectKind kind)
        {
            return kind switch
            {
                WarehouseObjectKind.PartialCover => PartialCoverVariants,
                WarehouseObjectKind.FullCover => FullCoverVariants,
                _ => null
            };
        }

        public GameObject GetFloorPrefab()
        {
            return FloorPrefab != null ? FloorPrefab : LoadPrefab("WarehouseFloor");
        }

        public GameObject GetWallPrefab()
        {
            return WallPrefab != null ? WallPrefab : LoadPrefab("WarehouseWall");
        }

        public GameObject GetSpawnMarkerPrefab()
        {
            return SpawnMarkerPrefab != null ? SpawnMarkerPrefab : LoadPrefab("SpawnMarker");
        }

        public GameObject GetCapturePointMarkerPrefab()
        {
            return CapturePointMarkerPrefab != null ? CapturePointMarkerPrefab : LoadPrefab("CapturePointMarker");
        }

        private static GameObject LoadPrefab(string prefabName)
        {
            return Resources.Load<GameObject>($"Presentation/Warehouse/{prefabName}");
        }

        public Vector3 GridToWorld(Vector2 gridPosition)
        {
            return new Vector3(
                (gridPosition.x - Width * 0.5f) * GridCellSize,
                0f,
                (gridPosition.y - Height * 0.5f) * GridCellSize);
        }

        public override void BuildPasses(List<ProcGenPass> passes)
        {
            passes.Add(new WarehouseLayoutPass());
            passes.Add(new WarehouseStructureGenerationPass());
            passes.Add(new WarehouseCoverGenerationPass());
            passes.Add(new WarehouseSymmetryPass());
            passes.Add(new WarehouseRepairPass());
            passes.Add(new WarehouseNavigationPass());
            passes.Add(new WarehouseFitnessPass());
            passes.Add(new WarehouseBuildPlanPass());
        }
    }
}
