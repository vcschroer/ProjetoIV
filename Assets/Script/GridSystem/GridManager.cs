using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [SerializeField] private Vector3Int gridDimensions = new Vector3Int(10, 5, 10);
    private Dictionary<Vector3Int, BlockTile> grid = new Dictionary<Vector3Int, BlockTile>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        ScanGrid();
    }

    public void ScanGrid()
    {
        grid.Clear();
        BlockTile[] tiles = FindObjectsByType<BlockTile>(FindObjectsSortMode.None);

        foreach (var tile in tiles)
        {
            Vector3Int cellPos = Vector3Int.RoundToInt(tile.transform.position);
            tile.Setup(cellPos, tile.data);

            if (!grid.ContainsKey(cellPos))
            {
                grid.Add(cellPos, tile);
            }
        }
    }

    public BlockTile GetTileAt(Vector3Int pos)
    {
        grid.TryGetValue(pos, out BlockTile tile);
        return tile;
    }

    public List<Vector3Int> FindPath(Vector3Int start, Vector3Int target)
    {
        Queue<Vector3Int> frontier = new Queue<Vector3Int>();
        frontier.Enqueue(start);

        Dictionary<Vector3Int, Vector3Int> cameFrom = new Dictionary<Vector3Int, Vector3Int>();
        cameFrom[start] = start;

        Vector3Int[] directions = {
            new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0),
            new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1),
            new Vector3Int(1, 1, 0), new Vector3Int(-1, 1, 0),
            new Vector3Int(0, 1, 1), new Vector3Int(0, 1, -1),
            new Vector3Int(1, -1, 0), new Vector3Int(-1, -1, 0),
            new Vector3Int(0, -1, 1), new Vector3Int(0, -1, -1)
        };

        while (frontier.Count > 0)
        {
            Vector3Int current = frontier.Dequeue();

            if (current == target) break;

            foreach (var dir in directions)
            {
                Vector3Int next = current + dir;

                if (!cameFrom.ContainsKey(next) && IsValidStep(current, next))
                {
                    frontier.Enqueue(next);
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

    private bool IsValidStep(Vector3Int from, Vector3Int to)
    {
        Vector3Int groundPos = to + Vector3Int.down;
        BlockTile groundTile = GetTileAt(groundPos);

        if (groundTile == null || groundTile.data == null || !groundTile.data.isWalkable)
            return false;

        if (GetTileAt(to) != null)
            return false;

        return true;
    }
}