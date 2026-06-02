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

    [CreateAssetMenu(menuName = "Dipshoot/ProcGen/Warehouse Climb Access Variant", fileName = "WarehouseClimbAccessVariant")]
    public sealed class WarehouseClimbAccessVariant : WarehousePlacementVariant
    {
        private void OnValidate()
        {
            Kind = WarehouseObjectKind.Ladder;
        }
    }

    [CreateAssetMenu(menuName = "Dipshoot/ProcGen/Warehouse Cover Variant", fileName = "WarehouseCoverVariant")]
    public sealed class WarehouseCoverVariant : WarehousePlacementVariant
    {
    }

    [CreateAssetMenu(menuName = "Dipshoot/ProcGen/Warehouse Bridge Variant", fileName = "WarehouseBridgeVariant")]
    public sealed class WarehouseBridgeVariant : WarehousePlacementVariant
    {
        public WarehouseBridgeConnectionType ConnectionType = WarehouseBridgeConnectionType.Straight;

        private void OnValidate()
        {
            Kind = WarehouseObjectKind.Bridge;
        }
    }
}
