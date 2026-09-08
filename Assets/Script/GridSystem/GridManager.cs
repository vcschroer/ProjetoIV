using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [SerializeField] private Vector3Int gridDimensions = new Vector3Int(10, 5, 10);

    private readonly Dictionary<Vector3Int, BlockTile> grid = new Dictionary<Vector3Int, BlockTile>();
    private readonly Dictionary<Vector2Int, int> columnTopY = new Dictionary<Vector2Int, int>();

    private static readonly Vector3Int[] Directions3D = {
        new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0),
        new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1),
        new Vector3Int(1, 1, 0), new Vector3Int(-1, 1, 0),
        new Vector3Int(0, 1, 1), new Vector3Int(0, 1, -1),
        new Vector3Int(1, -1, 0), new Vector3Int(-1, -1, 0),
        new Vector3Int(0, -1, 1), new Vector3Int(0, -1, -1)
    };

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        ScanGrid();
    }

    public void ScanGrid()
    {
        grid.Clear();
        columnTopY.Clear();

        BlockTile[] tiles = FindObjectsByType<BlockTile>(FindObjectsSortMode.None);

        foreach (var tile in tiles)
        {
            Vector3Int cellPos = Vector3Int.RoundToInt(tile.transform.position);
            tile.Setup(cellPos, tile.data);
            AddTileToDictionaries(cellPos, tile);
        }
    }



    public void AddTile(BlockTile tile)
    {
        Vector3Int cellPos = Vector3Int.RoundToInt(tile.transform.position);
        tile.Setup(cellPos, tile.data);
        AddTileToDictionaries(cellPos, tile);
    }

    public void RemoveTile(Vector3Int pos)
    {
        if (grid.ContainsKey(pos))
        {
            grid.Remove(pos);
            RecalculateColumnTopY(new Vector2Int(pos.x, pos.z));
        }
    }

    private void AddTileToDictionaries(Vector3Int cellPos, BlockTile tile)
    {
        if (!grid.ContainsKey(cellPos))
        {
            grid.Add(cellPos, tile);
        }

        Vector2Int xz = new Vector2Int(cellPos.x, cellPos.z);
        if (!columnTopY.ContainsKey(xz) || cellPos.y > columnTopY[xz])
        {
            columnTopY[xz] = cellPos.y;
        }
    }

    private void RecalculateColumnTopY(Vector2Int xz)
    {
        columnTopY.Remove(xz);
        int highestY = int.MinValue;
        bool found = false;

        foreach (var pos in grid.Keys)
        {
            if (pos.x == xz.x && pos.z == xz.y)
            {
                if (pos.y > highestY) highestY = pos.y;
                found = true;
            }
        }

        if (found) columnTopY[xz] = highestY;
    }

    public BlockTile GetTileAt(Vector3Int pos)
    {
        grid.TryGetValue(pos, out BlockTile tile);
        return tile;
    }



    public List<Vector3Int> Find2DPathWithHeightLimit(Vector2Int startXZ, Vector2Int targetXZ, int maxStepHeight = 1)
    {
        startXZ = GetNearestColumnXZ(startXZ);
        targetXZ = GetNearestColumnXZ(targetXZ);

        Queue<Vector2Int> frontier = new Queue<Vector2Int>();
        frontier.Enqueue(startXZ);

        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>
        {
            [startXZ] = startXZ
        };

        Vector2Int[] directions2D = {
            new Vector2Int(1, 0), new Vector2Int(-1, 0),
            new Vector2Int(0, 1), new Vector2Int(0, -1)
        };

        while (frontier.Count > 0)
        {
            Vector2Int current = frontier.Dequeue();

            if (current == targetXZ) break;

            foreach (var dir in directions2D)
            {
                Vector2Int next = current + dir;

                if (cameFrom.ContainsKey(next)) continue;

                if (columnTopY.ContainsKey(next))
                {
                    frontier.Enqueue(next);
                    cameFrom[next] = current;
                }
            }
        }

        if (!cameFrom.ContainsKey(targetXZ)) return null;

        List<Vector3Int> path = new List<Vector3Int>();
        Vector2Int curr = targetXZ;

        while (curr != startXZ)
        {
            int y = columnTopY[curr];
            path.Add(new Vector3Int(curr.x, y, curr.y));
            curr = cameFrom[curr];
        }

        path.Add(new Vector3Int(startXZ.x, columnTopY[startXZ], startXZ.y));
        path.Reverse();
        return path;
    }

    private Vector2Int GetNearestColumnXZ(Vector2Int targetXZ)
    {
        if (columnTopY.ContainsKey(targetXZ)) return targetXZ;

        Vector2Int nearest = targetXZ;
        float minDistance = float.MaxValue;

        foreach (var xz in columnTopY.Keys)
        {
            float dist = Vector2Int.Distance(targetXZ, xz);
            if (dist < minDistance)
            {
                minDistance = dist;
                nearest = xz;
            }
        }

        return nearest;
    }


    public List<Vector3Int> FindPath(Vector3Int start, Vector3Int target, int currentStackCount = 0)
    {
        List<Vector3Int> frontier = new List<Vector3Int>();
        frontier.Add(start);

        Dictionary<Vector3Int, Vector3Int> cameFrom = new Dictionary<Vector3Int, Vector3Int>
        {
            [start] = start
        };

        while (frontier.Count > 0)
        {
            Vector3Int current = frontier[0];
            frontier.RemoveAt(0);

            if (current == target) break;

            foreach (var dir in Directions3D)
            {
                Vector3Int next = current + dir;

                if (!cameFrom.ContainsKey(next) && IsValidStep(current, next, currentStackCount))
                {
                    InsertIntoFrontier(frontier, next, target);
                    cameFrom[next] = current;
                }
            }
        }

        if (!cameFrom.ContainsKey(target)) return null;

        List<Vector3Int> path = new List<Vector3Int>();
        Vector3Int curr = target;
        while (curr != start)
        {
            path.Add(curr);
            curr = cameFrom[curr];
        }
        path.Reverse();
        return path;
    }

    private void InsertIntoFrontier(List<Vector3Int> frontier, Vector3Int node, Vector3Int target)
    {
        float dist = Vector3Int.Distance(node, target);

        for (int i = 0; i < frontier.Count; i++)
        {
            if (Vector3Int.Distance(frontier[i], target) > dist)
            {
                frontier.Insert(i, node);
                return;
            }
        }
        frontier.Add(node);
    }

    private bool IsValidStep(Vector3Int from, Vector3Int to, int currentStackCount)
    {
        Vector3Int groundPos = to + Vector3Int.down;
        BlockTile groundTile = GetTileAt(groundPos);

        if (groundTile == null || groundTile.data == null || !groundTile.data.isWalkable)
            return false;

        if (GetTileAt(to) != null)
            return false;

        if (groundTile.maxAllowedHeight > 0)
        {
            int playerTotalHeight = 1 + currentStackCount;
            if (playerTotalHeight > groundTile.maxAllowedHeight)
                return false;
        }

        return true;
    }
}