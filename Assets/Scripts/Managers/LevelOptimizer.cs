using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq; // Necesario para OrderBy y Max

public class LevelOptimizer : MonoBehaviour
{
    public BackFromGoalAlgorithm generator; // Arrastra tu BFGA aquí
    public GridManager grid; // Arrastra tu GridManager

    [Header("Parámetros de Generación")]
    public int numRocks = 5;
    public int numHoles = 5;
    public int numWalls = 10;

    [Header("Parámetros de Hill Climbing")]
    public int hc_MaxIterations = 100; // Criterio de parada
    public int hc_NeighborsPerIteration = 20;

    [Header("Parámetros de Algoritmo Genético")]
    public int ag_Generations = 100; // Criterio de parada
    public int ag_PopulationSize = 50;
    public float ag_MutationRate = 0.05f; // 5% de probabilidad de mutar un individuo
    public int ag_TournamentSize = 3; // Cuántos individuos compiten en la selección


    // --- INICIO: Generación con Hill Climbing ---

    // Llama a esta función para usar BFGA + Hill Climbing
    public void StartGenerationWith_HillClimbing()
    {
        StartCoroutine(OptimizeLevel_HC_Coroutine());
    }

    private IEnumerator OptimizeLevel_HC_Coroutine()
    {
        Debug.Log("Iniciando generación (BFGA)...");
        // 1. SOLUCIÓN INICIAL (Usando tu Técnica Simple)
        LevelState solucion_actual = generator.GenerateLevel(numRocks, numHoles, numWalls);
        
        // 2. FITNESS INICIAL
        float valor_actual = CalculateFitness(solucion_actual);
        solucion_actual.fitness = valor_actual;
        Debug.Log($"Generación inicial (BFGA) completa. Fitness: {valor_actual}");

        // 3. BUCLE HILL CLIMBING
        for (int i = 0; i < hc_MaxIterations; i++)
        {
            // 4. GENERAR VECINOS
            List<LevelState> vecinos = GenerateNeighbors(solucion_actual, hc_NeighborsPerIteration);
            if (vecinos.Count == 0) break;

            // 5. EVALUAR VECINOS
            foreach (var vecino in vecinos) vecino.fitness = CalculateFitness(vecino);

            // 6. ENCONTRAR MEJOR VECINO
            LevelState mejor_vecino = vecinos.OrderByDescending(v => v.fitness).First();
            float valor_vecino = mejor_vecino.fitness;

            // 7. COMPARAR
            if (valor_vecino <= valor_actual)
            {
                Debug.Log($"Hill Climbing detenido en iteración {i}. Óptimo local.");
                break;
            }

            // 8. ACEPTAR MEJOR SOLUCIÓN
            solucion_actual = mejor_vecino;
            valor_actual = valor_vecino;
            yield return null; // Pausa
        }

        Debug.Log($"Optimización (HC) terminada. Mejor fitness: {valor_actual}");
        SpawnLevel(solucion_actual);
    }

    // (GenerateNeighbors se usa para Hill Climbing y como operador de Mutación para AG)
    private List<LevelState> GenerateNeighbors(LevelState currentState, int count)
    {
        List<LevelState> neighbors = new List<LevelState>();
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        for (int i = 0; i < count; i++)
        {
            LevelState neighbor = new LevelState(currentState);
            int rockIdx = Random.Range(0, neighbor.rocksPos.Count);
            Vector2Int dir = dirs[Random.Range(0, dirs.Length)];

            // Asegurarse de que el índice es válido
            if (rockIdx < 0 || rockIdx >= neighbor.rocksPos.Count) continue;

            Vector2Int currentRockPos = neighbor.rocksPos[rockIdx];
            Vector2Int targetPos = currentRockPos + dir;

            if (IsValidPosition(neighbor, targetPos, rockIdx)) // Pasamos rockIdx
            {
                neighbor.rocksPos[rockIdx] = targetPos; 
                neighbors.Add(neighbor);
            }
        }
        return neighbors;
    }


    // --- INICIO: Generación con Algoritmo Genético ---

    // Llama a esta función para usar el Algoritmo Genético
    public void StartGenerationWith_GeneticAlgorithm()
    {
        StartCoroutine(OptimizeLevel_AG_Coroutine());
    }

    private IEnumerator OptimizeLevel_AG_Coroutine()
    {
        Debug.Log("Iniciando generación (Algoritmo Genético)...");
        
        // 1. INICIALIZACIÓN: Crear población inicial basada en BFGA
        List<LevelState> population = InitializePopulation();
        if (population.Count == 0)
        {
            Debug.LogError("Falló la inicialización de la población.");
            yield break;
        }
        
        // 2. EVALUACIÓN INICIAL
        foreach (var individual in population) individual.fitness = CalculateFitness(individual);

        // 3. BUCLE EVOLUTIVO
        for (int gen = 0; gen < ag_Generations; gen++)
        {
            List<LevelState> newPopulation = new List<LevelState>();

            // Elitismo: Mantener al mejor individuo
            var elite = population.OrderByDescending(ind => ind.fitness).First();
            newPopulation.Add(new LevelState(elite)); // Añadir una copia

            // Llenar el resto de la población
            while (newPopulation.Count < ag_PopulationSize)
            {
                // 4. SELECCIÓN (Usamos Torneo)
                LevelState parent1 = Selection_Tournament(population);
                LevelState parent2 = Selection_Tournament(population);

                // 5. CRUCE (Crossover)
                LevelState child = Crossover(parent1, parent2);

                // 6. MUTACIÓN
                if (Random.value < ag_MutationRate)
                {
                    Mutate(child);
                }

                // 7. EVALUAR Y AÑADIR
                child.fitness = CalculateFitness(child);
                newPopulation.Add(child);
            }

            population = newPopulation; // La nueva generación reemplaza a la antigua

            if (gen % 10 == 0) // Log cada 10 generaciones
            {
                Debug.Log($"Generación {gen}. Mejor Fitness: {population.Max(ind => ind.fitness)}");
                yield return null; // Pausa
            }
        }

        // 8. FIN: Seleccionar y spawnear al mejor
        LevelState bestSolution = population.OrderByDescending(ind => ind.fitness).First();
        Debug.Log($"Optimización (AG) terminada. Mejor fitness: {bestSolution.fitness}");
        SpawnLevel(bestSolution);
    }

    // --- Funciones del Algoritmo Genético ---

    // Inicializa la población usando BFGA para garantizar solvencia inicial
    private List<LevelState> InitializePopulation()
    {
        Debug.Log("Inicializando población usando BFGA...");
        
        // 1. Generamos UNA solución base resoluble
        LevelState baseState = generator.GenerateLevel(numRocks, numHoles, numWalls);
        
        List<LevelState> population = new List<LevelState>();
        if (baseState == null) return population; // Fallo en generación
        
        // 2. Llenamos la población con copias de esa solución
        for (int i = 0; i < ag_PopulationSize; i++)
        {
            population.Add(new LevelState(baseState)); 
        }
        
        Debug.Log($"Población inicial ({ag_PopulationSize}) creada.");
        return population;
    }

    // SELECCIÓN: Elige al mejor de N individuos aleatorios
    private LevelState Selection_Tournament(List<LevelState> population)
    {
        LevelState best = null;
        for (int i = 0; i < ag_TournamentSize; i++)
        {
            LevelState randomInd = population[Random.Range(0, population.Count)];
            if (best == null || randomInd.fitness > best.fitness)
            {
                best = randomInd;
            }
        }
        return best;
    }

    // CRUCE: Combina dos padres para crear un hijo
    private LevelState Crossover(LevelState parent1, LevelState parent2)
    {
        // Crear hijo "vacío" (copia de P1 pero sin rocas)
        LevelState child = new LevelState(parent1.gridLayout, parent1.holes, parent1.goalPos, parent1.walls, new List<Vector2Int>(), parent1.playerPos);

        // Crossover de un punto: tomar la mitad de rocas de P1 y la mitad de P2
        int splitPoint = numRocks / 2;
        
        // Tomar rocas de P1 (evitando duplicados)
        for (int i = 0; i < splitPoint; i++)
        {
             if (i < parent1.rocksPos.Count)
                child.rocksPos.Add(parent1.rocksPos[i]);
        }
        
        // Tomar rocas de P2 (evitando duplicados)
        for (int i = splitPoint; i < numRocks; i++)
        {
            if (i < parent2.rocksPos.Count)
            {
                Vector2Int rockPos = parent2.rocksPos[i];
                if (!child.rocksPos.Contains(rockPos) && IsValidPosition(child, rockPos, -1))
                {
                    child.rocksPos.Add(rockPos);
                }
            }
        }
        
        // Si faltan rocas (por colisión o error), añadir aleatorias
        int attempts = 0;
        while (child.rocksPos.Count < numRocks && attempts < 1000)
        {
            attempts++;
            Vector2Int pos = new Vector2Int(Random.Range(1, grid.width - 1), Random.Range(1, grid.height - 1));
            if (IsValidPosition(child, pos, -1))
            {
                child.rocksPos.Add(pos);
            }
        }

        return child;
    }

    // MUTACIÓN: Mueve una roca al azar (reutiliza la lógica de GenerateNeighbors)
    private void Mutate(LevelState individual)
    {
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        
        if (individual.rocksPos.Count == 0) return; // No hay rocas que mutar

        int rockIdx = Random.Range(0, individual.rocksPos.Count);
        Vector2Int dir = dirs[Random.Range(0, dirs.Length)];
        Vector2Int currentRockPos = individual.rocksPos[rockIdx];
        Vector2Int targetPos = currentRockPos + dir;

        // Aplicar la mutación si es válida
        if (IsValidPosition(individual, targetPos, rockIdx))
        {
            individual.rocksPos[rockIdx] = targetPos;
        }
    }


    // --- FUNCIONES HELPER (Comunes) ---

    // Esta es tu funcion_fitness MEJORADA
    private float CalculateFitness(LevelState state)
    {
        float totalScore = 0;

        // --- 1. MÉTRICAS DE DISTANCIA (Lo que ya tenías) ---
        float distanceScore = 0;
        foreach (var rockPos in state.rocksPos)
        {
            // Lejos de los agujeros (bueno)
            distanceScore += MinDistanceToHoles(rockPos, state.holes);
            // Lejos del jugador (bueno)
            distanceScore += ManhattanDistance(rockPos, state.playerPos);
        }
        totalScore += distanceScore * 1.0f;


        // --- 2. MÉTRICAS DE PENALIZACIÓN (Evitar niveles rotos) ---
        float penaltyScore = 0;
        foreach (var rockPos in state.rocksPos)
        {
            // PENALIZACIÓN 1: Deadlock en Esquina
            if (IsCornerPosition(state.gridLayout, rockPos))
            {
                penaltyScore += 1000.0f; // Penalización masiva
            }

            // PENALIZACIÓN 2: Clúster de Rocas
            foreach (var otherRock in state.rocksPos)
            {
                if (rockPos == otherRock) continue;
                if (ManhattanDistance(rockPos, otherRock) <= 1)
                {
                    penaltyScore += 50.0f; // Penalización alta
                }
            }
        }
        totalScore -= penaltyScore;


        // --- 3. MÉTRICAS DE "JUGABILIDAD" (Lo más importante) ---
        
        // MÉTRICA 3A: Libertad del Jugador (BFS)
        int playerArea = GetReachableArea(state, state.playerPos);
        totalScore += playerArea * 0.5f; // Bonificación por espacio

        // MÉTRICA 3B: "Empujabilidad" de la Roca
        int trappedRocks = 0;
        foreach (var rockPos in state.rocksPos)
        {
            int validPushes = 0;
            Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            
            foreach (var dir in dirs)
            {
                Vector2Int targetPos = rockPos + dir;
                Vector2Int behindPos = rockPos - dir;
                
                // Un "empuje válido" requiere que la celda objetivo Y
                // la celda "detrás" (donde estaría el jugador) estén libres.
                if (IsValidPosition(state, targetPos, -1) && IsValidPosition(state, behindPos, -1))
                {
                    validPushes++;
                }
            }

            if (validPushes == 0)
            {
                trappedRocks++;
            }
        }

        // Penaliza fuertemente las rocas que están 100% atascadas
        totalScore -= trappedRocks * 200.0f; 

        return totalScore;
    }


    // Función final para mostrar el nivel
    private void SpawnLevel(LevelState state)
    {
        // El BFGA ya spawneó los muros/hoyos/meta.
        // Solo necesitamos actualizar los elementos dinámicos.
        grid.SpawnRocks(state.rocksPos);
        grid.SpawnPlayer(state.playerPos);
    }

    // Modificado para aceptar el índice de la roca que se mueve
    private bool IsValidPosition(LevelState state, Vector2Int pos, int movingRockIndex)
    {
        // 1. Dentro del grid (sin bordes)
        if (pos.x <= 0 || pos.y <= 0 || pos.x >= grid.width - 1 || pos.y >= grid.height - 1)
            return false;
        
        // 2. No es un muro
        if (state.gridLayout[pos.x, pos.y] == GridCellType.Wall)
            return false;

        // 3. No es otra roca
        for (int i = 0; i < state.rocksPos.Count; i++)
        {
            if (i == movingRockIndex) continue; // No chocar consigo misma
            if (state.rocksPos[i] == pos) return false;
        }
        
        return true;
    }

    // HELPER: BFS para contar área accesible
    private int GetReachableArea(LevelState state, Vector2Int start)
    {
        if (state == null || state.gridLayout == null) return 0;

        int w = grid.width, h = grid.height;
        bool[,] visited = new bool[w, h];
        Queue<Vector2Int> q = new Queue<Vector2Int>();
        
        // Validar punto de inicio
        if (start.x < 0 || start.x >= w || start.y < 0 || start.y >= h) return 0;
        
        q.Enqueue(start); 
        visited[start.x, start.y] = true;
        int reachableCount = 0;
        
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            reachableCount++; // Contamos la celda actual

            foreach (var d in dirs)
            {
                Vector2Int nxt = cur + d;
                
                if (nxt.x < 0 || nxt.y < 0 || nxt.x >= w || nxt.y >= h) continue;
                if (visited[nxt.x, nxt.y]) continue;
                if (state.gridLayout[nxt.x, nxt.y] == GridCellType.Wall) continue;
                
                bool isRock = false;
                foreach(var r in state.rocksPos) {
                    if (r == nxt) {
                        isRock = true;
                        break;
                    }
                }
                if (isRock) continue;

                visited[nxt.x, nxt.y] = true; 
                q.Enqueue(nxt);
            }
        }
        return reachableCount;
    }

    // HELPER: Detecta esquinas (Deadlocks)
    private bool IsCornerPosition(GridCellType[,] simGrid, Vector2Int pos)
    {
        System.Func<Vector2Int, bool> IsWallOrOut = (Vector2Int p) =>
        {
            if (p.x < 0 || p.y < 0 || p.x >= grid.width || p.y >= grid.height) return true;
            return simGrid[p.x, p.y] == GridCellType.Wall;
        };

        bool vert = IsWallOrOut(pos + Vector2Int.up) || IsWallOrOut(pos + Vector2Int.down);
        bool hor = IsWallOrOut(pos + Vector2Int.left) || IsWallOrOut(pos + Vector2Int.right);
        
        return vert && hor;
    }


    // HELPER: Distancia a agujeros
    private int MinDistanceToHoles(Vector2Int pos, List<Vector2Int> holes)
    {
        if (holes == null || holes.Count == 0) return 0;
        int min = int.MaxValue;
        foreach (var h in holes)
        {
            int d = ManhattanDistance(pos, h);
            if (d < min) min = d;
        }
        return (min == int.MaxValue) ? 0 : min;
    }

    // HELPER: Distancia Manhattan
    private int ManhattanDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}