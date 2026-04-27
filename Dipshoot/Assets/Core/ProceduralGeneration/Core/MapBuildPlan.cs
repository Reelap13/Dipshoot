using System.Collections.Generic;
using UnityEngine;

namespace Game.ProcGen
{
    public sealed class MapBuildPlan
    {
        public readonly List<MapTerrainChunkPlan> TerrainChunks = new List<MapTerrainChunkPlan>();
        public readonly List<MapObjectPlacementPlan> ObjectPlacements = new List<MapObjectPlacementPlan>();
    }

    public sealed class MapTerrainChunkPlan
    {
        public Vector2Int GridCoordinate;
        public Vector3 Origin;
        public Vector2 ChunkSize;
        public int SamplesPerAxis;
        public float[] Heights;
    }

    public sealed class MapObjectPlacementPlan
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
        public int PrefabIndex;
    }
}
