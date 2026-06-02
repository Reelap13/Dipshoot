using System.Collections.Generic;
using UnityEngine;

namespace Game.ProcGen.Warehouse
{
    public sealed class WarehouseContainerPalettePass : ProcGenPass
    {
        public override string Id => "warehouse-container-palette";

        public override void Declare(GenerationPassContract contract)
        {
            contract.Read(WarehouseKeys.Layout);
            contract.Write(WarehouseKeys.Layout);
        }

        public override void Execute(GenerationContext context)
        {
            WarehouseRecipe recipe = context.GetRecipe<WarehouseRecipe>();
            WarehouseLayoutData layout = context.Blackboard.GetRequired(WarehouseKeys.Layout);
            Apply(recipe, layout, context.CreateRandom(Id));
        }

        public static void Apply(WarehouseRecipe recipe, WarehouseLayoutData layout, System.Random random)
        {
            WarehouseContainerPalette palette = recipe.ContainerPalette;
            if (palette == null || palette.MaterialCount <= 0)
                return;

            if (!palette.ColorByGroups)
            {
                ColorContainersIndividually(layout, palette, random);
                return;
            }

            ColorContainerGroups(layout, palette, random);
        }

        private static void ColorContainersIndividually(WarehouseLayoutData layout, WarehouseContainerPalette palette, System.Random random)
        {
            for (int i = 0; i < layout.Objects.Count; i++)
            {
                WarehouseObjectPlacement obj = layout.Objects[i];
                if (obj.IsContainer)
                    obj.PaletteIndex = random.Next(palette.MaterialCount);
            }
        }

        private static void ColorContainerGroups(WarehouseLayoutData layout, WarehouseContainerPalette palette, System.Random random)
        {
            Dictionary<Vector2Int, WarehouseObjectPlacement> containers = new();
            for (int i = 0; i < layout.Objects.Count; i++)
            {
                WarehouseObjectPlacement obj = layout.Objects[i];
                if (obj.IsContainer)
                    containers[obj.Origin] = obj;
            }

            HashSet<Vector2Int> visited = new();
            Queue<Vector2Int> queue = new();
            foreach (KeyValuePair<Vector2Int, WarehouseObjectPlacement> entry in containers)
            {
                if (!visited.Add(entry.Key))
                    continue;

                int baseIndex = random.Next(palette.MaterialCount);
                queue.Enqueue(entry.Key);
                while (queue.Count > 0)
                {
                    Vector2Int cell = queue.Dequeue();
                    WarehouseObjectPlacement obj = containers[cell];
                    obj.PaletteIndex = ChoosePaletteIndex(palette, random, baseIndex);

                    for (int i = 0; i < WarehouseGenerationUtility.CardinalDirections.Length; i++)
                    {
                        Vector2Int next = cell + WarehouseGenerationUtility.CardinalDirections[i];
                        if (containers.ContainsKey(next) && visited.Add(next))
                            queue.Enqueue(next);
                    }
                }
            }
        }

        private static int ChoosePaletteIndex(WarehouseContainerPalette palette, System.Random random, int baseIndex)
        {
            if (palette.MaterialCount <= 1 || random.NextDouble() > Mathf.Clamp01(palette.AlternateColorChance))
                return baseIndex;

            int index = random.Next(palette.MaterialCount - 1);
            return index >= baseIndex ? index + 1 : index;
        }
    }
}
