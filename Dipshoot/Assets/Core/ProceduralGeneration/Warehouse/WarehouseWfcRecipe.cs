using System.Collections.Generic;
using UnityEngine;

namespace Game.ProcGen.Warehouse
{
    [CreateAssetMenu(menuName = "Dipshoot/ProcGen/Warehouse WFC Recipe", fileName = "WarehouseWfcRecipe")]
    public sealed class WarehouseWfcRecipe : WarehouseRecipe
    {
        [Header("WFC")]
        public int CollapseAttempts = 48;
        public int MaxPropagationSteps = 4096;
        public float EmptyWeight = 3.8f;
        public float ContainerLowWeight = 2.4f;
        public float ContainerHighWeight = 0.55f;
        public float BridgeWeight = 0.28f;
        public float LadderWeight = 0.32f;
        public float EntropyNoise = 0.05f;
        public float FailedAttemptPenalty = 50000f;

        [Header("WFC Shape")]
        public int ReservedPathWaypointMin = 2;
        public int ReservedPathWaypointMax = 4;
        public int ReservedPathJitter = 3;
        public int MinGroundPathTurns = 3;
        public int MaxStraightGroundSegment = 7;
        public float MinGroundPathDirectness = 1.2f;
        public float GroundPathTurnPenalty = 90f;
        public float GroundPathStraightPenalty = 45f;
        public float GroundPathDirectnessPenalty = 300f;

        public int AttemptCount => Mathf.Max(1, CollapseAttempts);
        public int PropagationStepLimit => Mathf.Max(128, MaxPropagationSteps);
        public float EmptyTileWeight => Mathf.Max(0.01f, EmptyWeight);
        public float ContainerLowTileWeight => Mathf.Max(0.01f, ContainerLowWeight);
        public float ContainerHighTileWeight => Mathf.Max(0.01f, ContainerHighWeight);
        public float BridgeTileWeight => Mathf.Max(0.01f, BridgeWeight);
        public float LadderTileWeight => Mathf.Max(0.01f, LadderWeight);
        public float EntropyNoiseValue => Mathf.Max(0f, EntropyNoise);
        public float FailedPenalty => Mathf.Max(0f, FailedAttemptPenalty);
        public int ReservedPathMinWaypoints => Mathf.Max(0, Mathf.Min(ReservedPathWaypointMin, ReservedPathWaypointMax));
        public int ReservedPathMaxWaypoints => Mathf.Max(ReservedPathMinWaypoints, ReservedPathWaypointMax);
        public int ReservedPathJitterCells => Mathf.Max(0, ReservedPathJitter);
        public int MinGroundPathTurnCount => Mathf.Max(0, MinGroundPathTurns);
        public int MaxStraightGroundSegmentCells => Mathf.Max(1, MaxStraightGroundSegment);
        public float MinGroundPathDirectnessValue => Mathf.Max(1f, MinGroundPathDirectness);
        public float GroundPathTurnPenaltyWeight => Mathf.Max(0f, GroundPathTurnPenalty);
        public float GroundPathStraightPenaltyWeight => Mathf.Max(0f, GroundPathStraightPenalty);
        public float GroundPathDirectnessPenaltyWeight => Mathf.Max(0f, GroundPathDirectnessPenalty);

        public override void BuildPasses(List<ProcGenPass> passes)
        {
            passes.Add(new WarehouseWfcPass());
        }
    }
}
