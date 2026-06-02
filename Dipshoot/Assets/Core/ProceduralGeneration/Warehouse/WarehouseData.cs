using System.Collections.Generic;
using UnityEngine;

namespace Game.ProcGen.Warehouse
{
    public enum WarehouseObjectKind
    {
        ContainerLow,
        ContainerHigh,
        Bridge,
        Ladder,
        PartialCover,
        FullCover
    }

    public enum WarehouseSide
    {
        A,
        B
    }

    public enum WarehouseDirection
    {
        North,
        South,
        East,
        West
    }

    public enum WarehousePlacementSurface
    {
        Ground,
        StructureTop
    }

    public sealed class WarehouseObjectPlacement
    {
        public WarehouseObjectKind Kind;
        public WarehouseSide Side;
        public Vector2Int Origin;
        public Vector2Int Size = Vector2Int.one;
        public WarehousePlacementSurface Surface = WarehousePlacementSurface.Ground;
        public WarehouseDirection Direction;
        public float RotationY;
        public int VariantIndex = -1;

        public bool IsContainer => Kind == WarehouseObjectKind.ContainerLow || Kind == WarehouseObjectKind.ContainerHigh;
        public bool IsStructure => IsContainer || Kind == WarehouseObjectKind.Bridge;
        public bool IsGroundBlocker => IsContainer;
        public bool IsTopWalkableSource => Kind == WarehouseObjectKind.ContainerLow || Kind == WarehouseObjectKind.Bridge;
        public bool IsTopBlocker => Kind == WarehouseObjectKind.ContainerHigh;
        public bool IsCover => Kind == WarehouseObjectKind.PartialCover || Kind == WarehouseObjectKind.FullCover;
        public bool IsLadder => Kind == WarehouseObjectKind.Ladder;

        public Vector2 Center => new Vector2(Origin.x + Size.x * 0.5f, Origin.y + Size.y * 0.5f);
    }

    public sealed class WarehouseLayoutData
    {
        public int Width;
        public int Height;
        public float CellSize;
        public float CellSizeX;
        public float CellSizeZ;
        public Vector2 SpawnA;
        public Vector2 SpawnB;
        public Vector2 CapturePoint;
        public float SpawnClearRadius;
        public float CaptureClearRadius;
        public int LeftLaneX;
        public int RightLaneX;
        public int LowerConnectorY;
        public int UpperConnectorY;
        public readonly List<WarehouseObjectPlacement> Objects = new();

        public bool IsInside(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < Width && cell.y >= 0 && cell.y < Height;
        }
    }

    public sealed class WarehouseFitnessReport
    {
        public float PenaltyScore;
        public int StructureCells;
        public int TargetStructureCells;
        public int MinStructureCells;
        public int MaxStructureCells;
        public int PathCostA;
        public int PathCostB;
        public readonly List<string> Violations = new();
    }

    public sealed class WarehouseBuildPlan
    {
        public WarehouseLayoutData Layout;
        public WarehouseFitnessReport Fitness;
    }
}
