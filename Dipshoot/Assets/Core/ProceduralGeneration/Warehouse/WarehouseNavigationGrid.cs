using System.Collections.Generic;
using UnityEngine;

namespace Game.ProcGen.Warehouse
{
    public readonly struct WarehouseNavNode
    {
        public readonly int X;
        public readonly int Y;
        public readonly int Level;

        public WarehouseNavNode(int x, int y, int level)
        {
            X = x;
            Y = y;
            Level = level;
        }
    }

    public sealed class WarehouseNavigationGrid
    {
        private static readonly Vector2Int[] CardinalDirections =
        {
            new(0, 1),
            new(0, -1),
            new(1, 0),
            new(-1, 0)
        };

        private readonly bool[,] _groundBlocked;
        private readonly bool[,] _ladderBlocked;
        private readonly bool[,] _topWalkable;
        private readonly int[,] _groundExtraCost;
        private readonly int[,] _topExtraCost;
        private readonly Vector2Int[,] _groundCoverDirection;
        private readonly Vector2Int[,] _topCoverDirection;
        private readonly int _partialCoverExtraCost;
        private readonly int _fullCoverExtraCost;
        private readonly List<WarehouseLadderLink> _ladders = new();

        public WarehouseNavigationGrid(
            WarehouseLayoutData layout,
            int partialCoverExtraCost = 2,
            int fullCoverExtraCost = 4)
        {
            Width = layout.Width;
            Height = layout.Height;
            _groundBlocked = new bool[Width, Height];
            _ladderBlocked = new bool[Width, Height];
            _topWalkable = new bool[Width, Height];
            _groundExtraCost = new int[Width, Height];
            _topExtraCost = new int[Width, Height];
            _groundCoverDirection = new Vector2Int[Width, Height];
            _topCoverDirection = new Vector2Int[Width, Height];
            _partialCoverExtraCost = Mathf.Max(0, partialCoverExtraCost);
            _fullCoverExtraCost = Mathf.Max(0, fullCoverExtraCost);

            for (int i = 0; i < layout.Objects.Count; i++)
                AddObject(layout.Objects[i]);
        }

        public int Width { get; }
        public int Height { get; }

        public bool IsGroundBlocked(Vector2Int cell)
        {
            return IsInside(cell) && _groundBlocked[cell.x, cell.y];
        }

        public bool IsTopWalkable(Vector2Int cell)
        {
            return IsInside(cell) && _topWalkable[cell.x, cell.y];
        }

        public int FindPathCost(Vector2 startPosition, Vector2 endPosition)
        {
            return FindPathCost(startPosition, endPosition, allowVerticalMovement: true);
        }

        public int FindGroundPathCost(Vector2 startPosition, Vector2 endPosition)
        {
            return FindGroundPathCostUnweighted(startPosition, endPosition);
        }

        public bool TryFindGroundPath(Vector2 startPosition, Vector2 endPosition, List<Vector2Int> path)
        {
            path.Clear();
            Vector2Int start = GridPointToCell(startPosition);
            Vector2Int end = GridPointToCell(endPosition);
            if (!IsWalkable(start, 0) || !IsWalkable(end, 0))
                return false;

            bool[,] visited = new bool[Width, Height];
            Vector2Int[,] previous = new Vector2Int[Width, Height];
            Queue<Vector2Int> queue = new();
            visited[start.x, start.y] = true;
            previous[start.x, start.y] = new Vector2Int(-1, -1);
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                if (current == end)
                {
                    BuildPath(previous, end, path);
                    return true;
                }

                for (int i = 0; i < CardinalDirections.Length; i++)
                {
                    Vector2Int next = current + CardinalDirections[i];
                    if (!IsInside(next) ||
                        visited[next.x, next.y] ||
                        !IsWalkable(next, 0) ||
                        IsMovementBlockedByCover(current, next, 0))
                    {
                        continue;
                    }

                    visited[next.x, next.y] = true;
                    previous[next.x, next.y] = current;
                    queue.Enqueue(next);
                }
            }

            return false;
        }

        private int FindPathCost(Vector2 startPosition, Vector2 endPosition, bool allowVerticalMovement)
        {
            Vector2Int start = GridPointToCell(startPosition);
            Vector2Int end = GridPointToCell(endPosition);
            if (!IsWalkable(start, 0) || !IsWalkable(end, 0))
                return -1;

            bool[,,] visited = new bool[Width, Height, 2];
            int[,,] cost = new int[Width, Height, 2];
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    cost[x, y, 0] = int.MaxValue;
                    cost[x, y, 1] = int.MaxValue;
                }
            }

            WarehousePathQueue queue = new();
            WarehouseNavNode startNode = new(start.x, start.y, 0);
            cost[start.x, start.y, 0] = 0;
            queue.Push(startNode, 0);

            while (queue.Count > 0)
            {
                WarehousePathQueueItem item = queue.Pop();
                WarehouseNavNode node = item.Node;
                if (visited[node.X, node.Y, node.Level] ||
                    item.Cost != cost[node.X, node.Y, node.Level])
                {
                    continue;
                }

                visited[node.X, node.Y, node.Level] = true;
                if (node.X == end.x && node.Y == end.y && node.Level == 0)
                    return item.Cost;

                AddNeighbours(node, visited, cost, queue, item.Cost, allowVerticalMovement);
            }

            return -1;
        }

        public bool HasLineOfSight(Vector2 fromPosition, Vector2 toPosition)
        {
            Vector2Int from = GridPointToCell(fromPosition);
            Vector2Int to = GridPointToCell(toPosition);
            int x = from.x;
            int y = from.y;
            int dx = Mathf.Abs(to.x - from.x);
            int dy = -Mathf.Abs(to.y - from.y);
            int sx = from.x < to.x ? 1 : -1;
            int sy = from.y < to.y ? 1 : -1;
            int error = dx + dy;

            while (true)
            {
                Vector2Int cell = new(x, y);
                if (!cell.Equals(from) && !cell.Equals(to) && IsGroundBlocked(cell))
                    return false;

                if (x == to.x && y == to.y)
                    return true;

                int e2 = error * 2;
                if (e2 >= dy)
                {
                    error += dy;
                    x += sx;
                }

                if (e2 <= dx)
                {
                    error += dx;
                    y += sy;
                }
            }
        }

        private void AddObject(WarehouseObjectPlacement placement)
        {
            if (placement.IsStructure)
            {
                ForEachCell(placement, cell =>
                {
                    if (!IsInside(cell))
                        return;

                    if (placement.IsGroundBlocker)
                        _groundBlocked[cell.x, cell.y] = true;

                    if (placement.IsTopWalkableSource)
                        _topWalkable[cell.x, cell.y] = true;
                });
                return;
            }

            if (placement.IsLadder)
            {
                if (IsInside(placement.Origin))
                    _ladderBlocked[placement.Origin.x, placement.Origin.y] = true;

                Vector2Int forward = DirectionToVector(placement.Direction);
                _ladders.Add(new WarehouseLadderLink(
                    placement.Origin,
                    placement.Origin + forward,
                    placement.Origin - forward));
                return;
            }

            if (placement.IsCover)
            {
                int extraCost = placement.Kind == WarehouseObjectKind.FullCover
                    ? _fullCoverExtraCost
                    : _partialCoverExtraCost;

                ForEachCell(placement, cell =>
                {
                    if (!IsInside(cell))
                        return;

                    int[,] costs = placement.Surface == WarehousePlacementSurface.StructureTop
                        ? _topExtraCost
                        : _groundExtraCost;
                    Vector2Int[,] directions = placement.Surface == WarehousePlacementSurface.StructureTop
                        ? _topCoverDirection
                        : _groundCoverDirection;
                    costs[cell.x, cell.y] = Mathf.Max(costs[cell.x, cell.y], extraCost);
                    directions[cell.x, cell.y] = RotationToVector(placement.RotationY);
                });
            }
        }

        private void AddNeighbours(
            WarehouseNavNode node,
            bool[,,] visited,
            int[,,] cost,
            WarehousePathQueue queue,
            int nodeCost,
            bool allowVerticalMovement)
        {
            Vector2Int current = new(node.X, node.Y);
            for (int i = 0; i < CardinalDirections.Length; i++)
            {
                Vector2Int next = current + CardinalDirections[i];
                if (!IsMovementBlockedByCover(current, next, node.Level))
                    TryRelax(next.x, next.y, node.Level, visited, cost, queue, nodeCost + GetStepCost(next, node.Level));

                if (node.Level == 1 && !IsMovementBlockedByCover(current, next, 1))
                    TryRelax(next.x, next.y, 0, visited, cost, queue, nodeCost + GetStepCost(next, 0));
            }

            if (!allowVerticalMovement)
                return;

            if (node.Level == 0)
            {
                AddLadderAscents(current, visited, cost, queue, nodeCost);
                return;
            }

            AddLadderDescents(current, visited, cost, queue, nodeCost);
        }

        private void AddLadderAscents(
            Vector2Int current,
            bool[,,] visited,
            int[,,] cost,
            WarehousePathQueue queue,
            int nodeCost)
        {
            for (int i = 0; i < _ladders.Count; i++)
            {
                WarehouseLadderLink ladder = _ladders[i];
                if (current != ladder.ApproachCell)
                    continue;

                TryRelax(
                    ladder.TopCell.x,
                    ladder.TopCell.y,
                    1,
                    visited,
                    cost,
                    queue,
                    nodeCost + GetStepCost(ladder.TopCell, 1));
            }
        }

        private void AddLadderDescents(
            Vector2Int current,
            bool[,,] visited,
            int[,,] cost,
            WarehousePathQueue queue,
            int nodeCost)
        {
            for (int i = 0; i < _ladders.Count; i++)
            {
                WarehouseLadderLink ladder = _ladders[i];
                if (ladder.TopCell != current)
                    continue;

                TryRelax(
                    ladder.ApproachCell.x,
                    ladder.ApproachCell.y,
                    0,
                    visited,
                    cost,
                    queue,
                    nodeCost + GetStepCost(ladder.ApproachCell, 0));
            }
        }

        private void TryRelax(
            int x,
            int y,
            int level,
            bool[,,] visited,
            int[,,] cost,
            WarehousePathQueue queue,
            int nextCost)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height || level < 0 || level > 1)
                return;

            if (visited[x, y, level] || !IsWalkable(new Vector2Int(x, y), level))
                return;

            if (nextCost >= cost[x, y, level])
                return;

            cost[x, y, level] = nextCost;
            queue.Push(new WarehouseNavNode(x, y, level), nextCost);
        }

        private int FindGroundPathCostUnweighted(Vector2 startPosition, Vector2 endPosition)
        {
            Vector2Int start = GridPointToCell(startPosition);
            Vector2Int end = GridPointToCell(endPosition);
            if (!IsWalkable(start, 0) || !IsWalkable(end, 0))
                return -1;

            bool[,] visited = new bool[Width, Height];
            int[,] cost = new int[Width, Height];
            Queue<Vector2Int> queue = new();
            visited[start.x, start.y] = true;
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                if (current == end)
                    return cost[current.x, current.y];

                int nextCost = cost[current.x, current.y] + 1;
                for (int i = 0; i < CardinalDirections.Length; i++)
                {
                    Vector2Int next = current + CardinalDirections[i];
                    if (!IsInside(next) ||
                        visited[next.x, next.y] ||
                        !IsWalkable(next, 0) ||
                        IsMovementBlockedByCover(current, next, 0))
                    {
                        continue;
                    }

                    visited[next.x, next.y] = true;
                    cost[next.x, next.y] = nextCost;
                    queue.Enqueue(next);
                }
            }

            return -1;
        }

        private int GetStepCost(Vector2Int cell, int level)
        {
            if (!IsInside(cell))
                return 1;

            return 1 + (level == 0 ? _groundExtraCost[cell.x, cell.y] : _topExtraCost[cell.x, cell.y]);
        }

        private bool IsMovementBlockedByCover(Vector2Int from, Vector2Int to, int level)
        {
            if (!IsInside(from) || !IsInside(to))
                return false;

            Vector2Int movement = to - from;
            if (Mathf.Abs(movement.x) + Mathf.Abs(movement.y) != 1)
                return false;

            return IsCoverBlockingMovement(from, movement, level) ||
                   IsCoverBlockingMovement(to, movement, level);
        }

        private bool IsCoverBlockingMovement(Vector2Int cell, Vector2Int movement, int level)
        {
            Vector2Int direction = level == 0
                ? _groundCoverDirection[cell.x, cell.y]
                : _topCoverDirection[cell.x, cell.y];

            return direction != Vector2Int.zero &&
                   (movement == direction || movement == -direction);
        }

        private bool IsWalkable(Vector2Int cell, int level)
        {
            if (!IsInside(cell))
                return false;

            return level == 0
                ? !_groundBlocked[cell.x, cell.y] && !_ladderBlocked[cell.x, cell.y]
                : _topWalkable[cell.x, cell.y];
        }

        private bool IsInside(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < Width && cell.y >= 0 && cell.y < Height;
        }

        private static Vector2Int GridPointToCell(Vector2 position)
        {
            return new Vector2Int(
                Mathf.Clamp(Mathf.FloorToInt(position.x), 0, int.MaxValue),
                Mathf.Clamp(Mathf.FloorToInt(position.y), 0, int.MaxValue));
        }

        private static void ForEachCell(WarehouseObjectPlacement placement, System.Action<Vector2Int> action)
        {
            for (int y = 0; y < placement.Size.y; y++)
            {
                for (int x = 0; x < placement.Size.x; x++)
                    action(new Vector2Int(placement.Origin.x + x, placement.Origin.y + y));
            }
        }

        private static void BuildPath(Vector2Int[,] previous, Vector2Int end, List<Vector2Int> path)
        {
            Vector2Int current = end;
            while (current.x >= 0 && current.y >= 0)
            {
                path.Add(current);
                current = previous[current.x, current.y];
            }

            path.Reverse();
        }

        public static Vector2Int DirectionToVector(WarehouseDirection direction)
        {
            return direction switch
            {
                WarehouseDirection.North => new Vector2Int(0, 1),
                WarehouseDirection.South => new Vector2Int(0, -1),
                WarehouseDirection.East => new Vector2Int(1, 0),
                WarehouseDirection.West => new Vector2Int(-1, 0),
                _ => Vector2Int.zero
            };
        }

        private static Vector2Int RotationToVector(float rotationY)
        {
            float angle = Mathf.Repeat(rotationY, 360f);
            if (Mathf.Abs(Mathf.DeltaAngle(angle, 90f)) <= 45f)
                return Vector2Int.right;
            if (Mathf.Abs(Mathf.DeltaAngle(angle, 180f)) <= 45f)
                return Vector2Int.down;
            if (Mathf.Abs(Mathf.DeltaAngle(angle, 270f)) <= 45f)
                return Vector2Int.left;

            return Vector2Int.up;
        }

        private sealed class WarehousePathQueue
        {
            private readonly List<WarehousePathQueueItem> _items = new();

            public int Count => _items.Count;

            public void Push(WarehouseNavNode node, int cost)
            {
                _items.Add(new WarehousePathQueueItem(node, cost));
                int index = _items.Count - 1;
                while (index > 0)
                {
                    int parent = (index - 1) / 2;
                    if (_items[parent].Cost <= cost)
                        break;

                    _items[index] = _items[parent];
                    index = parent;
                }

                _items[index] = new WarehousePathQueueItem(node, cost);
            }

            public WarehousePathQueueItem Pop()
            {
                WarehousePathQueueItem result = _items[0];
                WarehousePathQueueItem last = _items[_items.Count - 1];
                _items.RemoveAt(_items.Count - 1);
                if (_items.Count == 0)
                    return result;

                int index = 0;
                while (true)
                {
                    int left = index * 2 + 1;
                    if (left >= _items.Count)
                        break;

                    int right = left + 1;
                    int child = right < _items.Count && _items[right].Cost < _items[left].Cost
                        ? right
                        : left;

                    if (_items[child].Cost >= last.Cost)
                        break;

                    _items[index] = _items[child];
                    index = child;
                }

                _items[index] = last;
                return result;
            }
        }

        private readonly struct WarehousePathQueueItem
        {
            public readonly WarehouseNavNode Node;
            public readonly int Cost;

            public WarehousePathQueueItem(WarehouseNavNode node, int cost)
            {
                Node = node;
                Cost = cost;
            }
        }

        private readonly struct WarehouseLadderLink
        {
            public readonly Vector2Int GroundCell;
            public readonly Vector2Int TopCell;
            public readonly Vector2Int ApproachCell;

            public WarehouseLadderLink(Vector2Int groundCell, Vector2Int topCell, Vector2Int approachCell)
            {
                GroundCell = groundCell;
                TopCell = topCell;
                ApproachCell = approachCell;
            }
        }
    }
}
