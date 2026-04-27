using System.Collections.Generic;
using UnityEngine;

namespace Game.ProcGen.Chunked
{
    public sealed class ChunkLayoutData
    {
        public readonly List<ChunkDescriptor> Chunks = new List<ChunkDescriptor>();
    }

    public sealed class ChunkTerrainData
    {
        public readonly List<ChunkHeightMap> HeightMaps = new List<ChunkHeightMap>();
    }

    public sealed class TreePlacementData
    {
        public readonly List<TreePlacement> Placements = new List<TreePlacement>();
    }

    public struct ChunkDescriptor
    {
        public Vector2Int GridCoordinate;
        public Vector3 Origin;
        public Vector2 Size;
    }

    public sealed class ChunkHeightMap
    {
        public ChunkDescriptor Descriptor;
        public int SamplesPerAxis;
        public float[] Heights;
    }

    public struct TreePlacement
    {
        public Vector2Int ChunkCoordinate;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
        public int PrefabIndex;
    }
}
