using UnityEngine;

namespace Game.ProcGen.Warehouse
{
    internal static class WarehouseVariantSelector
    {
        public static bool TryGetPrefab(
            WarehouseRecipe recipe,
            WarehouseLayoutData layout,
            WarehouseObjectPlacement placement,
            out GameObject prefab)
        {
            prefab = null;
            WarehousePlacementVariant variant = placement.Kind switch
            {
                WarehouseObjectKind.Ladder => GetVariant(recipe.ClimbAccessVariants, layout, placement),
                WarehouseObjectKind.Bridge => GetBridgeVariant(recipe, layout, placement),
                WarehouseObjectKind.PartialCover or WarehouseObjectKind.FullCover => GetVariant(recipe.CoverVariants, layout, placement),
                _ => null
            };

            if (variant == null || variant.Prefab == null)
                return false;

            prefab = variant.Prefab;
            return true;
        }

        public static int ChooseClimbAccessVariantIndex(
            WarehouseRecipe recipe,
            WarehouseLayoutData layout,
            WarehouseObjectPlacement placement,
            System.Random random)
        {
            return ChooseVariantIndex(recipe.ClimbAccessVariants, layout, placement, random);
        }

        public static int ChooseCoverVariantIndex(
            WarehouseRecipe recipe,
            WarehouseLayoutData layout,
            WarehouseObjectPlacement placement,
            System.Random random)
        {
            return ChooseVariantIndex(recipe.CoverVariants, layout, placement, random);
        }

        public static int ChooseBridgeVariantIndex(
            WarehouseRecipe recipe,
            WarehouseLayoutData layout,
            WarehouseObjectPlacement placement,
            System.Random random)
        {
            return ChooseVariantIndex(recipe.BridgeVariants, layout, placement, random);
        }

        private static WarehouseBridgeVariant GetBridgeVariant(
            WarehouseRecipe recipe,
            WarehouseLayoutData layout,
            WarehouseObjectPlacement placement)
        {
            return GetVariant(recipe.BridgeVariants, layout, placement);
        }

        private static T GetVariant<T>(
            T[] variants,
            WarehouseLayoutData layout,
            WarehouseObjectPlacement placement)
            where T : WarehousePlacementVariant
        {
            if (variants == null || variants.Length == 0)
                return null;

            if (placement.VariantIndex >= 0 &&
                placement.VariantIndex < variants.Length &&
                IsVariantValid(variants[placement.VariantIndex], layout, placement))
            {
                return variants[placement.VariantIndex];
            }

            int start = Mathf.Abs(HashPlacement(placement)) % variants.Length;
            for (int offset = 0; offset < variants.Length; offset++)
            {
                T variant = variants[(start + offset) % variants.Length];
                if (IsVariantValid(variant, layout, placement))
                    return variant;
            }

            return null;
        }

        private static int ChooseVariantIndex<T>(
            T[] variants,
            WarehouseLayoutData layout,
            WarehouseObjectPlacement placement,
            System.Random random)
            where T : WarehousePlacementVariant
        {
            if (variants == null || variants.Length == 0)
                return -1;

            float total = 0f;
            for (int i = 0; i < variants.Length; i++)
            {
                if (IsVariantValid(variants[i], layout, placement))
                    total += Mathf.Max(0f, variants[i].Weight);
            }

            if (total <= 0f)
                return -1;

            double roll = random.NextDouble() * total;
            float current = 0f;
            for (int i = 0; i < variants.Length; i++)
            {
                if (!IsVariantValid(variants[i], layout, placement))
                    continue;

                current += Mathf.Max(0f, variants[i].Weight);
                if (roll <= current)
                    return i;
            }

            return -1;
        }

        private static bool IsVariantValid(
            WarehousePlacementVariant variant,
            WarehouseLayoutData layout,
            WarehouseObjectPlacement placement)
        {
            if (variant == null ||
                variant.Prefab == null ||
                variant.Kind != placement.Kind ||
                !IsDirectionAllowed(variant, GetDirection(placement)))
            {
                return false;
            }

            if (variant is WarehouseBridgeVariant bridgeVariant &&
                bridgeVariant.ConnectionType != placement.BridgeConnectionType)
            {
                return false;
            }

            if (layout == null)
                return true;

            WarehouseDirection direction = GetDirection(placement);
            Vector2Int forward = DirectionToVector(direction);
            Vector2Int right = new(forward.y, -forward.x);
            Vector2Int left = new(-right.x, -right.y);

            return MatchesRule(layout, placement.Origin + forward, variant.ForwardRule, placement.Surface) &&
                   MatchesRule(layout, placement.Origin - forward, variant.BackRule, placement.Surface) &&
                   MatchesRule(layout, placement.Origin + left, variant.LeftRule, placement.Surface) &&
                   MatchesRule(layout, placement.Origin + right, variant.RightRule, placement.Surface);
        }

        private static bool MatchesRule(
            WarehouseLayoutData layout,
            Vector2Int cell,
            WarehouseNeighbourRule rule,
            WarehousePlacementSurface surface)
        {
            return rule switch
            {
                WarehouseNeighbourRule.Any => true,
                WarehouseNeighbourRule.Inside => layout.IsInside(cell),
                WarehouseNeighbourRule.EmptyGround => IsGroundEmpty(layout, cell),
                WarehouseNeighbourRule.LowContainer => HasKindAt(layout, cell, WarehouseObjectKind.ContainerLow),
                WarehouseNeighbourRule.AnyContainer => HasContainerAt(layout, cell),
                WarehouseNeighbourRule.Structure => HasStructureAt(layout, cell),
                WarehouseNeighbourRule.TopWalkable => HasTopWalkableAt(layout, cell),
                WarehouseNeighbourRule.NotStructure => layout.IsInside(cell) && !HasStructureAt(layout, cell),
                WarehouseNeighbourRule.NotLadder => layout.IsInside(cell) && !HasLadderAt(layout, cell),
                WarehouseNeighbourRule.OutsideOrWall => !layout.IsInside(cell),
                _ => false
            };
        }

        private static bool IsGroundEmpty(WarehouseLayoutData layout, Vector2Int cell)
        {
            return layout.IsInside(cell) &&
                   !HasStructureAt(layout, cell) &&
                   !HasLadderAt(layout, cell) &&
                   !HasCoverAt(layout, cell, WarehousePlacementSurface.Ground);
        }

        private static bool HasContainerAt(WarehouseLayoutData layout, Vector2Int cell)
        {
            return HasKindAt(layout, cell, WarehouseObjectKind.ContainerLow) ||
                   HasKindAt(layout, cell, WarehouseObjectKind.ContainerHigh);
        }

        private static bool HasStructureAt(WarehouseLayoutData layout, Vector2Int cell)
        {
            for (int i = 0; i < layout.Objects.Count; i++)
            {
                WarehouseObjectPlacement obj = layout.Objects[i];
                if (obj.IsStructure && GetRect(obj).Contains(cell))
                    return true;
            }

            return false;
        }

        private static bool HasTopWalkableAt(WarehouseLayoutData layout, Vector2Int cell)
        {
            for (int i = 0; i < layout.Objects.Count; i++)
            {
                WarehouseObjectPlacement obj = layout.Objects[i];
                if (obj.IsTopWalkableSource && GetRect(obj).Contains(cell))
                    return true;
            }

            return false;
        }

        private static bool HasLadderAt(WarehouseLayoutData layout, Vector2Int cell)
        {
            for (int i = 0; i < layout.Objects.Count; i++)
            {
                WarehouseObjectPlacement obj = layout.Objects[i];
                if (obj.IsLadder && GetRect(obj).Contains(cell))
                    return true;
            }

            return false;
        }

        private static bool HasCoverAt(WarehouseLayoutData layout, Vector2Int cell, WarehousePlacementSurface surface)
        {
            for (int i = 0; i < layout.Objects.Count; i++)
            {
                WarehouseObjectPlacement obj = layout.Objects[i];
                if (obj.IsCover && obj.Surface == surface && GetRect(obj).Contains(cell))
                    return true;
            }

            return false;
        }

        private static bool HasKindAt(WarehouseLayoutData layout, Vector2Int cell, WarehouseObjectKind kind)
        {
            for (int i = 0; i < layout.Objects.Count; i++)
            {
                WarehouseObjectPlacement obj = layout.Objects[i];
                if (obj.Kind == kind && GetRect(obj).Contains(cell))
                    return true;
            }

            return false;
        }

        private static bool IsDirectionAllowed(WarehousePlacementVariant variant, WarehouseDirection direction)
        {
            return (variant.AllowedDirections & DirectionToMask(direction)) != 0;
        }

        private static WarehouseDirection GetDirection(WarehouseObjectPlacement placement)
        {
            return placement.IsLadder || placement.Kind == WarehouseObjectKind.Bridge
                ? placement.Direction
                : RotationToDirection(placement.RotationY);
        }

        private static WarehouseDirectionMask DirectionToMask(WarehouseDirection direction)
        {
            return direction switch
            {
                WarehouseDirection.North => WarehouseDirectionMask.North,
                WarehouseDirection.South => WarehouseDirectionMask.South,
                WarehouseDirection.East => WarehouseDirectionMask.East,
                WarehouseDirection.West => WarehouseDirectionMask.West,
                _ => WarehouseDirectionMask.None
            };
        }

        private static Vector2Int DirectionToVector(WarehouseDirection direction)
        {
            return direction switch
            {
                WarehouseDirection.North => Vector2Int.up,
                WarehouseDirection.South => Vector2Int.down,
                WarehouseDirection.East => Vector2Int.right,
                WarehouseDirection.West => Vector2Int.left,
                _ => Vector2Int.zero
            };
        }

        private static WarehouseDirection RotationToDirection(float rotationY)
        {
            float angle = Mathf.Repeat(rotationY, 360f);
            if (Mathf.Abs(Mathf.DeltaAngle(angle, 90f)) <= 45f)
                return WarehouseDirection.East;
            if (Mathf.Abs(Mathf.DeltaAngle(angle, 180f)) <= 45f)
                return WarehouseDirection.South;
            if (Mathf.Abs(Mathf.DeltaAngle(angle, 270f)) <= 45f)
                return WarehouseDirection.West;

            return WarehouseDirection.North;
        }

        private static RectInt GetRect(WarehouseObjectPlacement obj)
        {
            return new RectInt(obj.Origin.x, obj.Origin.y, obj.Size.x, obj.Size.y);
        }

        private static int HashPlacement(WarehouseObjectPlacement placement)
        {
            return placement.Origin.x * 73856093 ^
                   placement.Origin.y * 19349663 ^
                   (int)placement.Kind * 83492791 ^
                   (int)GetDirection(placement) * 265443576;
        }
    }
}
