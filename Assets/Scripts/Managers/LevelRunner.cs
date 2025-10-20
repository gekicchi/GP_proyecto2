using UnityEngine;

public class LevelRunner : MonoBehaviour
{
    public int numRocksAndHoles = 3;
    public int numWalls = 4;
    public bool autoGenerateOnStart = true;

    private GridManager grid;
    private BackFromGoalAlgorithm generator;

    private void Start()
    {
        grid = FindFirstObjectByType<GridManager>();
        if (grid == null)
        {
            Debug.LogError("GridManager not found in scene. Add a GridManager GameObject.");
            return;
        }

        // Asegura que la grilla est� lista
        grid.EnsureGridInitialized();

        generator = new BackFromGoalAlgorithm(grid);

        if (autoGenerateOnStart)
            GenerateLevel();
    }

    public void ReGenerar()
    {
            GenerateLevel();
    }

    public void GenerateLevel()
    {
        if (generator == null)
            generator = new BackFromGoalAlgorithm(grid);

        // Forzar que el número de rocas sea igual al número de agujeros para evitar desajustes
        if (numRocksAndHoles < 1)
        {
            Debug.Log("LevelRunner: numHoles < 1, forcing to 1");
            numRocksAndHoles = 1;
        }
        generator.GenerateLevel(numRocksAndHoles, numRocksAndHoles, numWalls);
    }
}
