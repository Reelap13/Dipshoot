using UnityEngine;

namespace Game.ProcGen.Warehouse
{
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
