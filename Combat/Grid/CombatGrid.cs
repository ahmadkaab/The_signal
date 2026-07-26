using Godot;
using System.Collections.Generic;
using TheSignal.Core;
using TheSignal.Data;
using TheSignal.Combat.Units;

namespace TheSignal.Combat;

[GlobalClass]
public partial class CombatGrid : Node3D
{
    public const float CellSize = 1.0f;
    public const int GridWidth = 20;
    public const int GridHeight = 15;

    private TileMap _groundTileMap;
    private TileMap _coverTileMap;
    private TileMap _highlightTileMap;
    private Dictionary<Vector2I, GridCell> _cells = new();

    public override void _Ready()
    {
        _groundTileMap = GetNode<TileMap>("GroundTileMap");
        _coverTileMap = GetNode<TileMap>("CoverTileMap");
        _highlightTileMap = GetNode<TileMap>("HighlightTileMap");

        InitializeGrid();
    }

    private void InitializeGrid()
    {
        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                var coord = new Vector2I(x, y);
                _cells[coord] = new GridCell { Coord = coord, IsWalkable = true };
            }
        }
    }

    public Vector3 GridToWorld(Vector2I gridPos)
    {
        return new Vector3(
            (gridPos.X - GridWidth / 2f) * CellSize,
            0,
            (gridPos.Y - GridHeight / 2f) * CellSize
        );
    }

    public Vector2I WorldToGrid(Vector3 worldPos)
    {
        return new Vector2I(
            Mathf.FloorToInt(worldPos.X / CellSize + GridWidth / 2f),
            Mathf.FloorToInt(worldPos.Z / CellSize + GridHeight / 2f)
        );
    }

    public bool IsValidCell(Vector2I coord)
    {
        return coord.X >= 0 && coord.X < GridWidth &&
               coord.Y >= 0 && coord.Y < GridHeight &&
               _cells.ContainsKey(coord);
    }

    public GridCell GetCell(Vector2I coord)
    {
        return _cells.GetValueOrDefault(coord);
    }

    public void SetWalkable(Vector2I coord, bool walkable)
    {
        if (_cells.TryGetValue(coord, out var cell))
        {
            cell.IsWalkable = walkable;
        }
    }

    public void SetCover(Vector2I coord, CoverType coverType)
    {
        if (!_cells.TryGetValue(coord, out var cell)) return;
        cell.CoverType = coverType;

        if (_coverTileMap != null)
        {
            if (coverType == CoverType.None)
            {
                _coverTileMap.EraseCell(0, coord);
            }
            else
            {
                int tileId = coverType == CoverType.Half ? 0 : 1;
                _coverTileMap.SetCell(0, coord, 0, new Vector2I(tileId, 0));
            }
        }
    }

    public CoverType GetCover(Vector2I coord)
    {
        return _cells.TryGetValue(coord, out var cell) ? cell.CoverType : CoverType.None;
    }

    public CoverType GetCoverBetween(Vector2I from, Vector2I to)
    {
        var cells = GetCellsInLine(from, to);
        CoverType bestCover = CoverType.None;

        foreach (var cell in cells)
        {
            if (cell.Equals(from) || cell.Equals(to)) continue;
            var cover = GetCover(cell);
            if (cover > bestCover) bestCover = cover;
            if (bestCover == CoverType.Full) break;
        }
        return bestCover;
    }

    public bool HasLineOfSight(Vector2I from, Vector2I to)
    {
        var cells = GetCellsInLine(from, to);
        foreach (var cell in cells)
        {
            if (cell.Equals(from) || cell.Equals(to)) continue;
            var cover = GetCover(cell);
            if (cover == CoverType.Full) return false;
        }
        return true;
    }

    public List<Vector2I> GetCellsInLine(Vector2I from, Vector2I to)
    {
        var result = new List<Vector2I>();
        int x0 = from.X, y0 = from.Y;
        int x1 = to.X, y1 = to.Y;

        int dx = Mathf.Abs(x1 - x0);
        int dy = -Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx + dy;

        while (true)
        {
            result.Add(new Vector2I(x0, y0));
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
        return result;
    }

    public List<Vector2I> GetCellsInRange(Vector2I center, int range)
    {
        var result = new List<Vector2I>();
        for (int x = -range; x <= range; x++)
        {
            for (int y = -range; y <= range; y++)
            {
                var coord = new Vector2I(center.X + x, center.Y + y);
                if (IsValidCell(coord) && center.DistanceTo(coord) <= range)
                {
                    result.Add(coord);
                }
            }
        }
        return result;
    }

    public List<Vector2I> GetCellsInCone(Vector2I origin, Vector2I direction, int range, int angleDegrees)
    {
        var result = new List<Vector2I>();
        var dir = direction - origin;
        float baseAngle = Mathf.Atan2(dir.Y, dir.X);
        float halfAngle = Mathf.DegToRad(angleDegrees / 2f);

        for (int x = -range; x <= range; x++)
        {
            for (int y = -range; y <= range; y++)
            {
                var coord = new Vector2I(origin.X + x, origin.Y + y);
                if (!IsValidCell(coord)) continue;

                var toCell = coord - origin;
                float dist = toCell.Length();
                if (dist > range || dist == 0) continue;

                float cellAngle = Mathf.Atan2(toCell.Y, toCell.X);
                float diff = Mathf.Abs(Mathf.Wrap(cellAngle - baseAngle, -Mathf.Pi, Mathf.Pi));
                if (diff <= halfAngle)
                {
                    result.Add(coord);
                }
            }
        }
        return result;
    }

    public List<Vector2I> GetCellsInCircle(Vector2I center, int radius)
    {
        var result = new List<Vector2I>();
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                var coord = new Vector2I(center.X + x, center.Y + y);
                if (IsValidCell(coord) && center.DistanceTo(coord) <= radius)
                {
                    result.Add(coord);
                }
            }
        }
        return result;
    }

    public List<Vector2I> FindPath(Vector2I start, Vector2I goal, UnitInstance unit)
    {
        if (!IsValidCell(start) || !IsValidCell(goal)) return null;
        if (!GetCell(goal).IsWalkable) return null;

        var openSet = new PriorityQueue<Vector2I, float>();
        var cameFrom = new Dictionary<Vector2I, Vector2I>();
        var gScore = new Dictionary<Vector2I, float>();
        var fScore = new Dictionary<Vector2I, float>();

        openSet.Enqueue(start, 0);
        gScore[start] = 0;
        fScore[start] = Heuristic(start, goal);

        while (openSet.Count > 0)
        {
            openSet.TryDequeue(out var current, out _);

            if (current.Equals(goal))
            {
                return ReconstructPath(cameFrom, current);
            }

            foreach (var neighbor in GetNeighbors(current, unit))
            {
                float tentativeG = gScore[current] + GetMoveCost(current, neighbor, unit);
                if (!gScore.ContainsKey(neighbor) || tentativeG < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;
                    fScore[neighbor] = tentativeG + Heuristic(neighbor, goal);
                    openSet.Enqueue(neighbor, fScore[neighbor]);
                }
            }
        }
        return null; // No path found
    }

    private List<Vector2I> GetNeighbors(Vector2I cell, UnitInstance unit)
    {
        var neighbors = new List<Vector2I>();
        var directions = new Vector2I[]
        {
            new(1, 0), new(-1, 0), new(0, 1), new(0, -1),
            new(1, 1), new(1, -1), new(-1, 1), new(-1, -1)
        };

        foreach (var dir in directions)
        {
            var neighbor = cell + dir;
            if (IsValidCell(neighbor) && GetCell(neighbor).IsWalkable)
            {
                // Check diagonal movement through corners
                if (Mathf.Abs(dir.X) == 1 && Mathf.Abs(dir.Y) == 1)
                {
                    var adj1 = new Vector2I(cell.X + dir.X, cell.Y);
                    var adj2 = new Vector2I(cell.X, cell.Y + dir.Y);
                    if (!IsValidCell(adj1) || !GetCell(adj1).IsWalkable ||
                        !IsValidCell(adj2) || !GetCell(adj2).IsWalkable)
                        continue;
                }
                neighbors.Add(neighbor);
            }
        }
        return neighbors;
    }

    private float GetMoveCost(Vector2I from, Vector2I to, UnitInstance unit)
    {
        // Diagonal costs more
        if (from.X != to.X && from.Y != to.Y) return 1.414f;
        return 1.0f;
    }

    private float Heuristic(Vector2I a, Vector2I b)
    {
        int dx = Mathf.Abs(a.X - b.X);
        int dy = Mathf.Abs(a.Y - b.Y);
        return (dx + dy) + (1.414f - 2) * Mathf.Min(dx, dy); // Octile distance
    }

    private List<Vector2I> ReconstructPath(Dictionary<Vector2I, Vector2I> cameFrom, Vector2I current)
    {
        var path = new List<Vector2I> { current };
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Add(current);
        }
        path.Reverse();
        return path;
    }

    public void HighlightCell(Vector2I coord, Color color)
    {
        if (_highlightTileMap != null && IsValidCell(coord))
        {
            _highlightTileMap.SetCell(0, coord, 0, new Vector2I(2, 0));
            _highlightTileMap.SetCellModulate(0, coord, color);
        }
    }

    public void ClearHighlights()
    {
        _highlightTileMap?.Clear();
    }

    public void HighlightPath(List<Vector2I> path, Color color)
    {
        foreach (var coord in path)
        {
            HighlightCell(coord, color);
        }
    }

    public void HighlightRange(List<Vector2I> cells, Color color)
    {
        foreach (var coord in cells)
        {
            HighlightCell(coord, color);
        }
    }

    public void SetUnitOccupying(Vector2I coord, UnitInstance unit)
    {
        if (_cells.TryGetValue(coord, out var cell))
        {
            cell.OccupyingUnit = unit;
        }
    }

    public UnitInstance GetUnitAt(Vector2I coord)
    {
        return _cells.TryGetValue(coord, out var cell) ? cell.OccupyingUnit : null;
    }

    public bool IsCellOccupied(Vector2I coord)
    {
        return GetUnitAt(coord) != null;
    }
}

public class GridCell
{
    public Vector2I Coord { get; set; }
    public bool IsWalkable { get; set; } = true;
    public CoverType CoverType { get; set; } = CoverType.None;
    public UnitInstance OccupyingUnit { get; set; }
    public List<FieldEffect> ActiveFields { get; set; } = new();
}

public enum CoverType
{
    None = 0,
    Half = 1,
    Full = 2
}