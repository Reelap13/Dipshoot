using UnityEngine;

namespace Game.ProcGen.Warehouse
{
    [CreateAssetMenu(menuName = "Dipshoot/ProcGen/Warehouse Climb Access Variant", fileName = "WarehouseClimbAccessVariant")]
    public sealed class WarehouseClimbAccessVariant : WarehousePlacementVariant
    {
        private void OnValidate()
        {
            Kind = WarehouseObjectKind.Ladder;
        }
    }
}
