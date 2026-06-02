using UnityEngine;

namespace Game.ProcGen.Warehouse
{
    [CreateAssetMenu(menuName = "Dipshoot/ProcGen/Warehouse Decoration Variant", fileName = "WarehouseDecorationVariant")]
    public sealed class WarehouseDecorationVariant : ScriptableObject
    {
        public WarehouseDecorationKind Kind;
        public GameObject Prefab;
        public float Weight = 1f;
        public int MaxPerMap = 16;
        public Vector2 ScaleRange = Vector2.one;
        public bool RandomYaw = true;
        public bool NeedsStructureNear = true;
        public bool NeedsCorner;
        public bool AvoidLadders = true;
        public bool AvoidCovers = true;
        public float MinDistanceToSpawn = 2f;
        public float MinDistanceToCapture = 1f;
        public Vector2 OffsetXRange = new(-0.3f, 0.3f);
        public Vector2 OffsetZRange = new(-0.3f, 0.3f);
    }
}
