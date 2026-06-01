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
        private readonly List<WarehouseLadderLink> _ladders = new();

        public WarehouseNavigationGrid(WarehouseLayoutData layout)
        {
            Width = layout.Width;
            Height = layout.Height;
            _groundBlocked = new bool[Width, Height];
            _ladderBlocked = new bool[Width, Height];
            _topWalkable = new bool[Width, Height];

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
            return FindPathCost(startPosition, endPosition, allowVerticalMovement: false);
        }

        private int FindPathCost(Vector2 startPosition, Vector2 endPosition, bool allowVerticalMovement)
        {
            Vector2Int start = GridPointToCell(startPosition);
            Vector2Int end = GridPointToCell(endPosition);
            if (!IsWalkable(start, 0) || !IsWalkable(end, 0))
                return -1;

            bool[,,] visited = new bool[Width, Height, 2];
            int[,,] cost = new int[Width, Height, 2];
            Queue<WarehouseNavNode> queue = new();
            WarehouseNavNode startNode = new(start.x, start.y, 0);
            visited[start.x, start.y, 0] = true;
            queue.Enqueue(startNode);

            while (queue.Count > 0)
            {
                WarehouseNavNode node = queue.Dequeue();
                int nodeCost = cost[node.X, node.Y, node.Level];
                if (node.X == end.x && node.Y == end.y && node.Level == 0)
                    return nodeCost;

                AddNeighbours(node, visited, cost, queue, nodeCost + 1, allowVerticalMovement);
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

                _ladders.Add(new WarehouseLadderLink(
                    placement.Origin,
                    placement.Origin + DirectionToVector(placement.Direction)));
            }
        }

        private void AddNeighbours(
            WarehouseNavNode node,
            bool[,,] visited,
            int[,,] cost,
            Queue<WarehouseNavNode> queue,
            int nextCost,
            bool allowVerticalMovement)
        {
            Vector2Int current = new(node.X, node.Y);
            for (int i = 0; i < CardinalDirections.Length; i++)
            {
                Vector2Int next = current + CardinalDirections[i];
                TryEnqueue(next.x, next.y, node.Level, visited, cost, queue, nextCost);

                if (node.Level == 1)
                    TryEnqueue(next.x, next.y, 0, visited, cost, queue, nextCost);
            }

            if (!allowVerticalMovement)
                return;

            if (node.Level == 0)
            {
                AddLadderAscents(current, visited, cost, queue, nextCost);
                return;
            }

            AddLadderDescents(current, visited, cost, queue, nextCost);
        }

        private void AddLadderAscents(
            Vector2Int current,
            bool[,,] visited,
            int[,,] cost,
            Queue<WarehouseNavNode> queue,
            int nextCost)
        {
            for (int i = 0; i < _ladders.Count; i++)
            {
                WarehouseLadderLink ladder = _ladders[i];
                if (!IsAdjacent(current, ladder.GroundCell))
                    continue;

                TryEnqueue(ladder.TopCell.x, ladder.TopCell.y, 1, visited, cost, queue, nextCost);
            }
        }

        private void AddLadderDescents(
            Vector2Int current,
            bool[,,] visited,
            int[,,] cost,
            Queue<WarehouseNavNode> queue,
            int nextCost)
        {
            for (int i = 0; i < _ladders.Count; i++)
            {
                WarehouseLadderLink ladder = _ladders[i];
                if (ladder.TopCell != current)
                    continue;

                for (int j = 0; j < CardinalDirections.Length; j++)
                {
                    Vector2Int groundExit = ladder.GroundCell + CardinalDirections[j];
                    TryEnqueue(groundExit.x, groundExit.y, 0, visited, cost, queue, nextCost);
                }
            }
        }

        private void TryEnqueue(
            int x,
            int y,
            int level,
            bool[,,] visited,
            int[,,] cost,
            Queue<WarehouseNavNode> queue,
            int nextCost)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height || level < 0 || level > 1)
                return;

            if (visited[x, y, level] || !IsWalkable(new Vector2Int(x, y), level))
                return;

            visited[x, y, level] = true;
            cost[x, y, level] = nextCost;
            queue.Enqueue(new WarehouseNavNode(x, y, level));
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

        private static bool IsAdjacent(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) == 1;
        }

        private readonly struct WarehouseLadderLink
        {
            public readonly Vector2Int GroundCell;
            public readonly Vector2Int TopCell;

            public WarehouseLadderLink(Vector2Int groundCell, Vector2Int topCell)
            {
                GroundCell = groundCell;
                TopCell = topCell;
            }
        }
    }
}
