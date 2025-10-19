using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public int width, height;
    public GameObject tilePrefab;
    public GameObject playerPrefab;
    public GameObject holePrefab;
    public GameObject wallPrefab;
    public GameObject goalPrefab;
    public Rock rockPrefab;
    public Transform gridParent;
    public Vector2Int playerPos;

    public Vector2Int goalPos;

    private Vector3 origin = Vector3.zero;
    private GridCellType[,] gridLogic;
    public List<Vector2Int> holesList;

    private void Awake()
    {
        holesList = new List<Vector2Int>();
    }

    public void GenerateGrid()
    {
        gridLogic = new GridCellType[width, height];
        float offsetX = width / 2f - 0.5f;
        float offsetY = height / 2f - 0.5f;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 position = new Vector3(x - offsetX, y - offsetY, 0);
                Instantiate(tilePrefab, position + origin, Quaternion.identity, gridParent);
                gridLogic[x, y] = GridCellType.Empty;
            }
        }
    }

    public bool IsInsideGrid(Vector2Int pos) => pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;

    public Vector3 GridToWorld(Vector2Int pos)
    {
        float offsetX = width / 2f - 0.5f;
        float offsetY = height / 2f - 0.5f;

        return new Vector3(pos.x - offsetX, pos.y - offsetY, 0) + origin;
    }

    public GridCellType GetCell(Vector2Int pos)
    {
        if (!IsInsideGrid(pos))
            return GridCellType.Empty;

        return gridLogic[pos.x, pos.y];
    }

    public void SetCell(Vector2Int pos, GridCellType type)
    {
        if (!IsInsideGrid(pos))
            return;

        gridLogic[pos.x, pos.y] = type;
    }

    public void SpawnPlayer(Vector2Int pos)
    {
        var prevPlayer = FindFirstObjectByType<PlayerController>();
        if (prevPlayer != null)
            Destroy(prevPlayer.gameObject);

        Instantiate(playerPrefab, GridToWorld(pos), Quaternion.identity, gridParent);
        Debug.Log("putting player in: " + pos);
        playerPos = pos;
        SetCell(pos, GridCellType.Player);
    }

    public void SpawnRocks(List<Vector2Int> rocksPos)
    {
        foreach (var rockPos in rocksPos)
        {
            Rock rock = Instantiate(rockPrefab, GridToWorld(rockPos), Quaternion.identity, gridParent);
            rock.gridPos = rockPos;
            SetCell(rockPos, GridCellType.Rock);
        }
    }

    public void SpawnHoles(List<Vector2Int> holes)
    {
        foreach (var holePos in holes)
        {
            SetCell(holePos, GridCellType.Hole);
            if (!holesList.Contains(holePos)) holesList.Add(holePos);
            Instantiate(holePrefab, GridToWorld(holePos), Quaternion.identity, gridParent);
        }
    }

    public void SpawnWalls(List<Vector2Int> walls)
    {
        foreach (var wallPos in walls)
        {
            SetCell(wallPos, GridCellType.Wall);
            Instantiate(wallPrefab, GridToWorld(wallPos), Quaternion.identity, gridParent);
        }
    }

    public void PlaceGoal(Vector2Int pos)
    {
        goalPos = pos;
        SetCell(pos, GridCellType.Goal);
        Instantiate(goalPrefab, GridToWorld(pos), Quaternion.identity, gridParent);
    }

    public bool IsRockAt(Vector2Int pos) => GetCell(pos) == GridCellType.Rock;

    public bool IsHoleCovered(Vector2Int pos) => GetCell(pos) == GridCellType.Rock;

    public bool IsGoalUnlocked()
    {
        foreach (Vector2Int hole in holesList)
            if (!IsHoleCovered(hole)) return false;
        return true;
    }

    public void EnsureGridInitialized()
    {
        if (gridLogic == null || gridLogic.Length == 0)
            GenerateGrid();
    }

    public Vector2Int GetEmptyAdjacent(Vector2Int pos)
    {
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        foreach (var d in dirs)
        {
            Vector2Int check = pos + d;
            if (IsInsideGrid(check) && GetCell(check) == GridCellType.Empty)
                return check;
        }
        return pos;
    }

    public List<Vector2Int> GenerateRandomHoles(int numHoles)
    {
        List<Vector2Int> holes = new List<Vector2Int>();
        int attempts = 0;
        while (holes.Count < numHoles && attempts < 1000)
        {
            attempts++;
            Vector2Int pos = new Vector2Int(Random.Range(0, width), Random.Range(0, height));
            if (GetCell(pos) == GridCellType.Empty && pos != goalPos && !holes.Contains(pos))
            {
                holes.Add(pos);
                SetCell(pos, GridCellType.Hole);
            }
        }
        return holes;
    }

    public void ClearLevel()
    {
        holesList.Clear();
        foreach (Transform child in gridParent) Destroy(child.gameObject);
        gridLogic = new GridCellType[width, height];
        GenerateGrid();
    }
}
