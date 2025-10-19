using UnityEngine;

public class LevelRunner : MonoBehaviour
{
    public int numRocks = 3;
    public int numHoles = 2;
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

        // Asegura que la grilla esté lista
        grid.EnsureGridInitialized();

        generator = new BackFromGoalAlgorithm(grid);

        if (autoGenerateOnStart)
            GenerateLevel();
    }

    private void Update()
    {
        // Presiona T para regenerar nivel
        if (Input.GetKeyDown(KeyCode.T))
        {
            GenerateLevel();
        }
    }

    public void GenerateLevel()
    {
        if (generator == null)
            generator = new BackFromGoalAlgorithm(grid);

        generator.GenerateLevel(numRocks, numHoles, numWalls);
    }
}
