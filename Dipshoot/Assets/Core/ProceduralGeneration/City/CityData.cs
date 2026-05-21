using System.Collections.Generic;
using UnityEngine;

namespace Game.ProcGen.City
{
    public enum CityZoneType
    {
        Safe,
        Market,
        Residential,
        Industrial,
        Port,
        Ruins,
        Danger,
        Boss
    }

    public enum CityStructureKind
    {
        Building,
        Warehouse,
        MarketStall,
        Plaza,
        Ruin,
        Wall,
        Tower
    }

    public enum CityGameplayMarkerKind
    {
        PlayerSpawn,
        EnemySpawn,
        Loot,
        Trader,
        Quest,
        Encounter
    }

    public enum CityDetailKind
    {
        Lamp,
        Sign,
        Crate,
        Debris,
        Tree
    }

    public sealed class CitySurfaceData
    {
        public CitySurfaceData(Vector2 size, Rect bounds)
        {
            Size = size;
            Bounds = bounds;
        }

        public Vector2 Size { get; }

        public Rect Bounds { get; }

        public readonly List<Rect> WaterAreas = new List<Rect>();
        public readonly List<Rect> BlockedAreas = new List<Rect>();

        public bool IsBuildable(Rect footprint)
        {
            if (Bounds.Contains(new Vector2(footprint.xMin, footprint.yMin)) == false)
                return false;

            if (Bounds.Contains(new Vector2(footprint.xMax, footprint.yMax)) == false)
                return false;

            for (int i = 0; i < WaterAreas.Count; i++)
            {
                if (WaterAreas[i].Overlaps(footprint))
                    return false;
            }

            for (int i = 0; i < BlockedAreas.Count; i++)
            {
                if (BlockedAreas[i].Overlaps(footprint))
                    return false;
            }

            return true;
        }
    }

    public sealed class CityZone
    {
        public int Id;
        public CityZoneType Type;
        public Rect Bounds;
        public Vector2 Center;
        public float DangerLevel;
    }

    public sealed class CityZoneData
    {
        public readonly List<CityZone> Zones = new List<CityZone>();
    }

    public struct CityRoadSegment
    {
        public Vector2 Start;
        public Vector2 End;
        public float Width;
        public bool IsMain;
        public int ZoneId;
    }

    public sealed class CityRoadGraphData
    {
        public readonly List<CityRoadSegment> Roads = new List<CityRoadSegment>();
    }

    public sealed class CityBlock
    {
        public int ZoneId;
        public Rect Bounds;
        public bool ReservedOpenSpace;
    }

    public sealed class CityLocalLayoutData
    {
        public readonly List<CityRoadSegment> LocalRoads = new List<CityRoadSegment>();
        public readonly List<CityBlock> Blocks = new List<CityBlock>();
    }

    public sealed class CityStructurePlacement
    {
        public CityStructureKind Kind;
        public CityZoneType ZoneType;
        public int ZoneId;
        public Vector2 Position;
        public float RotationY;
        public Vector2 Size;
        public float Height;
    }

    public sealed class CityArchitectureData
    {
        public readonly List<CityStructurePlacement> Structures = new List<CityStructurePlacement>();
    }

    public sealed class CityGameplayPlacement
    {
        public CityGameplayMarkerKind Kind;
        public int ZoneId;
        public Vector2 Position;
        public float RotationY;
        public float Radius;
    }

    public sealed class CityGameplayData
    {
        public readonly List<CityGameplayPlacement> Placements = new List<CityGameplayPlacement>();
    }

    public sealed class CityDetailPlacement
    {
        public CityDetailKind Kind;
        public int ZoneId;
        public Vector2 Position;
        public float RotationY;
        public Vector3 Scale;
    }

    public sealed class CityDetailData
    {
        public readonly List<CityDetailPlacement> Placements = new List<CityDetailPlacement>();
    }

    public sealed class CityBuildPlan
    {
        public CitySurfaceData Surface;
        public CityZoneData Zones;
        public CityRoadGraphData GlobalRoads;
        public CityLocalLayoutData LocalLayout;
        public CityArchitectureData Architecture;
        public CityGameplayData Gameplay;
        public CityDetailData Details;
    }
}
