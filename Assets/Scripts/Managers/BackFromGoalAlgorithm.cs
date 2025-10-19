using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class BackFromGoalAlgorithm : MonoBehaviour
{
    private GridManager grid;
    private int backwardsSteps = 20;

    public BackFromGoalAlgorithm(GridManager grid)
    {
        this.grid = grid;
    }

    public void GenerateLevel(int numRocks, int numHoles, int numWalls)
    {
        grid.ClearLevel();

        // ------------ COLOCANDO PAREDES ------------ //
        List<Vector2Int> walls = new List<Vector2Int>();
        int attempts = 0;

        while (walls.Count < numWalls && attempts < 1000)
        {
            attempts++;

            Vector2Int pos = new Vector2Int(Random.Range(0, grid.width), Random.Range(0, grid.height));
            if (grid.GetCell(pos) == GridCellType.Empty)
                walls.Add(pos);
        }
        grid.SpawnWalls(walls);

        // ------------ COLOCANDO META ------------ //
        Vector2Int goalPos;
        do
        {
            goalPos = new Vector2Int(Random.Range(1, grid.width - 1), Random.Range(1, grid.height - 1));
        } while (grid.GetCell(goalPos) != GridCellType.Empty);
        grid.PlaceGoal(goalPos);

        // ------------ COLOCANDO AGUJEROS ------------ //
        List<Vector2Int> holes = grid.GenerateRandomHoles(numHoles);

        // ------------ COLOCANDO ROCAS ------------ //
        List<Vector2Int> rocksFinal = new List<Vector2Int>();
        for (int i = 0; i < numRocks; i++)
        {
            Vector2Int rockPos;
            int rockAttempts = 0;
            do
            {
                rockPos = new Vector2Int(Random.Range(1, grid.width - 1), Random.Range(1, grid.height - 1));
                rockAttempts++;
            } while ((grid.GetCell(rockPos) != GridCellType.Empty || holes.Contains(rockPos) || rockAttempts > 100));

            rocksFinal.Add(rockPos);
        }

        // ------------ COLOCANDO JUGADOR ------------ //
        Vector2Int playerFinalPos = grid.GetEmptyAdjacent(goalPos);

        // ------------ RETROCEDER PASOS ------------ //
        Vector2Int playerPos = playerFinalPos;
        List<Vector2Int> rocksPos = new List<Vector2Int>(rocksFinal);

        for (int i = 0; i <= backwardsSteps; i++)
        {
            Vector2Int dir = GetRandomDirection();
            Vector2Int newPlayerPos = playerPos + dir;

            if (!grid.IsInsideGrid(newPlayerPos) || grid.GetCell(newPlayerPos) == GridCellType.Wall)
                continue;

            if (IsMovableBackward(newPlayerPos))
                playerPos = newPlayerPos;
            else
            {
                int rockIndex = rocksPos.FindIndex(r => r == newPlayerPos);
                if (rockIndex == -1)
                    continue;

                Vector2Int rockPrevPos = newPlayerPos - dir;

                if (grid.IsInsideGrid(rockPrevPos) && IsMovableBackward(rockPrevPos))
                {
                    rocksPos[rockIndex] = rockPrevPos;
                    playerPos = newPlayerPos;
                }
            }
        }

        grid.SpawnHoles(holes);
        grid.SpawnRocks(rocksPos);
        grid.SpawnPlayer(playerPos);
    }

    // Determina si la celda es movible hacia atr�s (vac�a o agujero) y evita bordes
    private bool IsMovableBackward(Vector2Int pos)
    {
        if (pos.x <= 0 || pos.y <= 0 || pos.x >= grid.width - 1 || pos.y >= grid.height - 1)
            return false;

        GridCellType type = grid.GetCell(pos);
        return type == GridCellType.Empty || type == GridCellType.Hole;
    }

    // Direcciones aleatorias
    private Vector2Int GetRandomDirection()
    {
        int r = Random.Range(0, 4);
        switch (r)
        {
            case 0: return Vector2Int.up;
            case 1: return Vector2Int.down;
            case 2: return Vector2Int.left;
            case 3: return Vector2Int.right;
        }
        return Vector2Int.zero;
    }
}
