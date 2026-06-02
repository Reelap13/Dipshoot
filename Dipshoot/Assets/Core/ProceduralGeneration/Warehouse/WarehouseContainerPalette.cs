using UnityEngine;

namespace Game.ProcGen.Warehouse
{
    [CreateAssetMenu(menuName = "Dipshoot/ProcGen/Warehouse Container Palette", fileName = "WarehouseContainerPalette")]
    public sealed class WarehouseContainerPalette : ScriptableObject
    {
        public Material[] Materials;
        public float AlternateColorChance = 0.15f;
        public bool ColorByGroups = true;

        public int MaterialCount => Materials == null ? 0 : Materials.Length;

        public Material GetMaterial(int index)
        {
            if (Materials == null || index < 0 || index >= Materials.Length)
                return null;

            return Materials[index];
        }
    }
}
