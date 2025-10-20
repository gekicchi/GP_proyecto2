using UnityEngine;

public class LevelRunner : MonoBehaviour
{
    public enum OptimizationMode { None, HillClimbing, Genetic }
    [Tooltip("Selecciona el tipo de optimización a ejecutar después de la generación inicial")]
    public OptimizationMode optimizationMode = OptimizationMode.HillClimbing;

    public int numRocksAndHoles = 3;
    public int numWalls = 4;
    public bool autoGenerateOnStart = true;

    private GridManager grid;
    private BackFromGoalAlgorithm generator;

    public void SetOptimizationMode(int index)
    {

        if (System.Enum.IsDefined(typeof(OptimizationMode), index))
        {
            optimizationMode = (OptimizationMode)index;
            Debug.Log($"Modo de Optimización seleccionado: {optimizationMode}");
        }
        else
        {
            Debug.LogError($"Índice de Dropdown {index} no corresponde a un valor válido en OptimizationMode.");
        }
    }
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

        // Buscar un componente existente o añadir uno al GridManager para que las corrutinas funcionen
        generator = FindFirstObjectByType<BackFromGoalAlgorithm>();
        if (generator == null && grid != null)
            generator = grid.gameObject.AddComponent<BackFromGoalAlgorithm>();
        if (generator != null)
            generator.Grid = grid;

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
        {
            Debug.LogError("BackFromGoalAlgorithm component not found and could not be created.");
            return;
        }

        // Forzar que el número de rocas sea igual al número de agujeros para evitar desajustes
        if (numRocksAndHoles < 1)
        {
            Debug.Log("LevelRunner: numHoles < 1, forcing to 1");
            numRocksAndHoles = 1;
        }

        // Si se desea optimización, obtener o crear un LevelOptimizer y lanzar el algoritmo elegido
        if (optimizationMode != OptimizationMode.None)
        {
            var optimizer = FindFirstObjectByType<LevelOptimizer>();
            if (optimizer == null && grid != null)
                optimizer = grid.gameObject.AddComponent<LevelOptimizer>();

            if (optimizer != null)
            {
                // Pasar referencias y parámetros básicos
                optimizer.generator = generator;
                optimizer.grid = grid;
                optimizer.numRocks = numRocksAndHoles;
                optimizer.numHoles = numRocksAndHoles;
                optimizer.numWalls = numWalls;

                // Elegir algoritmo
                if (optimizationMode == OptimizationMode.HillClimbing)
                {
                    optimizer.StartGenerationWith_HillClimbing();
                }
                else if (optimizationMode == OptimizationMode.Genetic)
                {
                    optimizer.StartGenerationWith_GeneticAlgorithm();
                }
                return;
            }
            else
            {
                Debug.LogWarning("LevelRunner: requested optimization but LevelOptimizer could not be created.");
            }
        }

        // Si no hay optimizador, llamar a GenerateLevel en el componente (lanza la corrutina internamente)
        generator.GenerateLevel(numRocksAndHoles, numRocksAndHoles, numWalls);
    }
}
