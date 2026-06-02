using UnityEngine;

namespace Game.ProcGen.Warehouse
{
    public abstract class WarehousePlacementVariant : ScriptableObject
    {
        public WarehouseObjectKind Kind;
        public GameObject Prefab;
        public float Weight = 1f;
        public WarehouseDirectionMask AllowedDirections = WarehouseDirectionMask.All;
        public WarehouseNeighbourRule ForwardRule = WarehouseNeighbourRule.Any;
        public WarehouseNeighbourRule BackRule = WarehouseNeighbourRule.Any;
        public WarehouseNeighbourRule LeftRule = WarehouseNeighbourRule.Any;
        public WarehouseNeighbourRule RightRule = WarehouseNeighbourRule.Any;
    }
}
