using System.Collections.Generic;
using UnityEngine;

namespace Game.ProcGen.Warehouse
{
    internal static class WarehouseDecorationGenerator
    {
        private const float EdgeOffset = 0.34f;
        private const float CornerOffset = 0.34f;

        public static int Populate(WarehouseRecipe recipe, WarehouseLayoutData layout, System.Random random)
        {
            layout.Decorations.Clear();
            if (recipe.DecorationVariants == null ||
                recipe.DecorationVariants.Length == 0 ||
                recipe.MaxDecorationCount <= 0 ||
                recipe.DecorationPlacementAttemptCount <= 0 ||
                recipe.DecorationProbability <= 0f)
            {
                return 0;
            }

            bool[,] structures = BuildOccupancy(layout, obj => obj.IsStructure);
            bool[,] ladders = BuildOccupancy(layout, obj => obj.IsLadder);
            bool[,] covers = BuildOccupancy(layout, obj => obj.IsCover);
            int[,] cellCounts = new int[layout.Width, layout.Height];
            int[] variantCounts = new int[recipe.DecorationVariants.Length];

            for (int attempt = 0; attempt < recipe.DecorationPlacementAttemptCount && layout.Decorations.Count < recipe.MaxDecorationCount; attempt++)
            {
                if (random.NextDouble() > recipe.DecorationProbability)
                    continue;

                Vector2Int cell = new(random.Next(layout.Width), random.Next(layout.Height));
                if (cellCounts[cell.x, cell.y] >= recipe.MaxDecorationCountPerCell)
                    continue;

                int variantIndex = ChooseVariant(recipe, layout, random, cell, structures, ladders, covers, cellCounts, variantCounts);
                if (variantIndex < 0)
                    continue;

                WarehouseDecorationVariant variant = recipe.DecorationVariants[variantIndex];
                Vector2 edgeOffset = GetPlacementOffset(layout, cell, structures, random, variant);
                layout.Decorations.Add(new WarehouseDecorationPlacement
                {
                    Kind = variant.Kind,
                    Prefab = variant.Prefab,
                    Cell = cell,
                    Offset = edgeOffset,
                    RotationY = variant.RandomYaw ? (float)random.NextDouble() * 360f : 0f,
                    Scale = RandomRange(random, Mathf.Min(variant.ScaleRange.x, variant.ScaleRange.y), Mathf.Max(variant.ScaleRange.x, variant.ScaleRange.y))
                });

                cellCounts[cell.x, cell.y]++;
                variantCounts[variantIndex]++;
            }

            return layout.Decorations.Count;
        }

        private static int ChooseVariant(
            WarehouseRecipe recipe,
            WarehouseLayoutData layout,
            System.Random random,
            Vector2Int cell,
            bool[,] structures,
            bool[,] ladders,
            bool[,] covers,
            int[,] cellCounts,
            int[] variantCounts)
        {
            float totalWeight = 0f;
            for (int i = 0; i < recipe.DecorationVariants.Length; i++)
            {
                WarehouseDecorationVariant variant = recipe.DecorationVariants[i];
                if (IsVariantValid(variant, layout, cell, structures, ladders, covers, cellCounts, variantCounts[i]))
                    totalWeight += Mathf.Max(0f, variant.Weight);
            }

            if (totalWeight <= 0f)
                return -1;

            double roll = random.NextDouble() * totalWeight;
            float current = 0f;
            for (int i = 0; i < recipe.DecorationVariants.Length; i++)
            {
                WarehouseDecorationVariant variant = recipe.DecorationVariants[i];
                if (!IsVariantValid(variant, layout, cell, structures, ladders, covers, cellCounts, variantCounts[i]))
                    continue;

                current += Mathf.Max(0f, variant.Weight);
                if (roll <= current)
                    return i;
            }

            return -1;
        }

        private static bool IsVariantValid(
            WarehouseDecorationVariant variant,
            WarehouseLayoutData layout,
            Vector2Int cell,
            bool[,] structures,
            bool[,] ladders,
            bool[,] covers,
            int[,] cellCounts,
            int variantCount)
        {
            if (variant == null || variant.Prefab == null || variant.Weight <= 0f)
                return false;

            if (variant.MaxPerMap >= 0 && variantCount >= variant.MaxPerMap)
                return false;

            if (structures[cell.x, cell.y])
                return false;

            if (variant.AvoidLadders && ladders[cell.x, cell.y])
                return false;

            if (variant.AvoidCovers && covers[cell.x, cell.y])
                return false;

            if (variant.Kind == WarehouseDecorationKind.LargeProp && cellCounts[cell.x, cell.y] > 0)
                return false;

            if (variant.NeedsStructureNear && !HasStructureNear(layout, cell, structures))
                return false;

            if ((variant.NeedsCorner || variant.Kind == WarehouseDecorationKind.CornerClutter) && !TryGetCornerDirection(layout, cell, structures, out _))
                return false;

            if (TouchesPoint(cell, layout.SpawnA, Mathf.Max(0f, variant.MinDistanceToSpawn)) ||
                TouchesPoint(cell, layout.SpawnB, Mathf.Max(0f, variant.MinDistanceToSpawn)) ||
                TouchesPoint(cell, layout.CapturePoint, Mathf.Max(0f, variant.MinDistanceToCapture)))
            {
                return false;
            }

            return true;
        }

        private static Vector2 GetPlacementOffset(
            WarehouseLayoutData layout,
            Vector2Int cell,
            bool[,] structures,
            System.Random random,
            WarehouseDecorationVariant variant)
        {
            Vector2 offset = new(
                RandomRange(random, variant.OffsetXRange.x, variant.OffsetXRange.y),
                RandomRange(random, variant.OffsetZRange.x, variant.OffsetZRange.y));

            if (variant.NeedsCorner || variant.Kind == WarehouseDecorationKind.CornerClutter)
            {
                if (TryGetCornerDirection(layout, cell, structures, out Vector2 cornerDirection))
                    offset += cornerDirection * CornerOffset;
            }
            else if (TryGetNearestStructureDirection(layout, cell, structures, out Vector2 edgeDirection))
            {
                offset += edgeDirection * EdgeOffset;
            }

            return new Vector2(Mathf.Clamp(offset.x, -0.45f, 0.45f), Mathf.Clamp(offset.y, -0.45f, 0.45f));
        }

        private static bool[,] BuildOccupancy(WarehouseLayoutData layout, System.Func<WarehouseObjectPlacement, bool> predicate)
        {
            bool[,] occupied = new bool[layout.Width, layout.Height];
            for (int i = 0; i < layout.Objects.Count; i++)
            {
                WarehouseObjectPlacement obj = layout.Objects[i];
                if (!predicate(obj))
                    continue;

                RectInt rect = new(obj.Origin.x, obj.Origin.y, obj.Size.x, obj.Size.y);
                for (int y = rect.yMin; y < rect.yMax; y++)
                {
                    for (int x = rect.xMin; x < rect.xMax; x++)
                    {
                        Vector2Int cell = new(x, y);
                        if (layout.IsInside(cell))
                            occupied[x, y] = true;
                    }
                }
            }

            return occupied;
        }

        private static bool HasStructureNear(WarehouseLayoutData layout, Vector2Int cell, bool[,] structures)
        {
            for (int i = 0; i < WarehouseGenerationUtility.CardinalDirections.Length; i++)
            {
                Vector2Int neighbor = cell + WarehouseGenerationUtility.CardinalDirections[i];
                if (!layout.IsInside(neighbor) || structures[neighbor.x, neighbor.y])
                    return true;
            }

            return false;
        }

        private static bool TryGetNearestStructureDirection(WarehouseLayoutData layout, Vector2Int cell, bool[,] structures, out Vector2 direction)
        {
            for (int i = 0; i < WarehouseGenerationUtility.CardinalDirections.Length; i++)
            {
                Vector2Int neighbor = cell + WarehouseGenerationUtility.CardinalDirections[i];
                if (layout.IsInside(neighbor) && !structures[neighbor.x, neighbor.y])
                    continue;

                direction = new Vector2(WarehouseGenerationUtility.CardinalDirections[i].x, WarehouseGenerationUtility.CardinalDirections[i].y);
                return true;
            }

            direction = Vector2.zero;
            return false;
        }

        private static bool TryGetCornerDirection(WarehouseLayoutData layout, Vector2Int cell, bool[,] structures, out Vector2 direction)
        {
            bool north = IsStructureOrOutside(layout, cell + Vector2Int.up, structures);
            bool south = IsStructureOrOutside(layout, cell + Vector2Int.down, structures);
            bool east = IsStructureOrOutside(layout, cell + Vector2Int.right, structures);
            bool west = IsStructureOrOutside(layout, cell + Vector2Int.left, structures);

            if (north && east)
            {
                direction = new Vector2(1f, 1f).normalized;
                return true;
            }

            if (east && south)
            {
                direction = new Vector2(1f, -1f).normalized;
                return true;
            }

            if (south && west)
            {
                direction = new Vector2(-1f, -1f).normalized;
                return true;
            }

            if (west && north)
            {
                direction = new Vector2(-1f, 1f).normalized;
                return true;
            }

            direction = Vector2.zero;
            return false;
        }

        private static bool IsStructureOrOutside(WarehouseLayoutData layout, Vector2Int cell, bool[,] structures)
        {
            return !layout.IsInside(cell) || structures[cell.x, cell.y];
        }

        private static bool TouchesPoint(Vector2Int cell, Vector2 point, float radius)
        {
            if (radius <= 0f)
                return false;

            Vector2 center = new(cell.x + 0.5f, cell.y + 0.5f);
            return (center - point).sqrMagnitude <= radius * radius;
        }

        private static float RandomRange(System.Random random, float min, float max)
        {
            if (max < min)
                (min, max) = (max, min);

            return min + (float)random.NextDouble() * (max - min);
        }
    }
}
