using System.Collections.Generic;
using UnityEngine;

// --- Objeto para guardar el estado del nivel ---
// Puedes mover esta clase a su propio archivo LevelState.cs
public class LevelState
{
    // Elementos estáticos (no cambian en Hill Climbing)
    public GridCellType[,] gridLayout; // El grid base (muros, vacío)
    public List<Vector2Int> holes;
    public Vector2Int goalPos;
    public List<Vector2Int> walls;

    // Elementos dinámicos (lo que optimiza Hill Climbing)
    public List<Vector2Int> rocksPos;
    public Vector2Int playerPos;

    public float fitness; // Puntuación de esta solución

    // Constructor para la generación inicial
    public LevelState(GridCellType[,] grid, List<Vector2Int> h, Vector2Int g, List<Vector2Int> w, List<Vector2Int> r, Vector2Int p)
    {
        this.gridLayout = grid;
        this.holes = h;
        this.goalPos = g;
        this.walls = w;
        this.rocksPos = r;
        this.playerPos = p;
        this.fitness = 0;
    }

    // Constructor para clonar (necesario para generar vecinos)
    public LevelState(LevelState original)
    {
        // Los estáticos se pueden referenciar, no cambian
        this.gridLayout = original.gridLayout;
        this.holes = original.holes;
        this.goalPos = original.goalPos;
        this.walls = original.walls;

        // Los dinámicos deben ser copias profundas
        this.rocksPos = new List<Vector2Int>(original.rocksPos);
        this.playerPos = original.playerPos;
        this.fitness = original.fitness;
    }
}


// --- Algoritmo de Generación ---
public class BackFromGoalAlgorithm : MonoBehaviour
{
    private GridManager grid;

    [Tooltip("Número de pasos máximos por roca al retroceder desde la meta")]
    public int backwardsSteps = 20;
    [Tooltip("Profundidad máxima de empujes encadenados")]
    public int maxPushDepth = 5;
    [Tooltip("Habilitar logs de depuración de la simulación")]
    public bool debugSimulation = true;
    [Tooltip("Distancia Manhattan objetivo a agujeros al dispersar rocas (heurística)")]
    public int scatterDistance = 3;

    // Unity MonoBehaviours should not rely on constructors for initialization.
    // Provide an explicit Initialize method so other scripts can set the GridManager.
    public void Initialize(GridManager grid)
    {
        this.grid = grid;
    }

    // Public property to allow other scripts to set/get the GridManager without calling Initialize.
    public GridManager Grid
    {
        get { return grid; }
        set { grid = value; }
    }

    // Representa una acción de empuje para mover una roca (índice en rocksPos -> nueva posición)
    private struct PushAction
    {
        public int rockIndex;
        public Vector2Int to;
        public PushAction(int rockIndex, Vector2Int to)
        {
            this.rockIndex = rockIndex;
            this.to = to;
        }
    }

    // ... (CanPlayerReachWithPush y sus helpers DFS/Recursive no cambian) ...

    private bool CanPlayerReachWithPush(Vector2Int start, Vector2Int target, GridCellType[,] simGrid, List<Vector2Int> rocksPos, int excludedRockIdx, out List<PushAction> outPushes)
    {
        // Quick path: sin empujes
        if (CanPlayerReach(start, target, simGrid, rocksPos))
        {
            outPushes = new List<PushAction>();
            return true;
        }

        // Depth-limited search trying single/multiple pushes up to maxPushDepth
        List<PushAction> result = new List<PushAction>();
        bool ok = CanPlayerReachWithPushDFS(start, target, simGrid, rocksPos, excludedRockIdx, 0, result);
        outPushes = ok ? result : null;
        return ok;
    }

    private bool CanPlayerReachWithPushDFS(Vector2Int start, Vector2Int target, GridCellType[,] simGrid, List<Vector2Int> rocksPos, int excludedRockIdx, int depth, List<PushAction> accumulated)
    {
        if (depth >= maxPushDepth) return false;
        // Try all rocks (except excluded) and directions as candidate pushes
        int rockCount = rocksPos.Count;
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        for (int ri = 0; ri < rockCount; ri++)
        {
            if (ri == excludedRockIdx) continue;
            foreach (var d in dirs)
            {
                // make temp copies
                List<Vector2Int> tempRocks = new List<Vector2Int>(rocksPos);
                GridCellType[,] tempGrid = (GridCellType[,])simGrid.Clone();
                List<PushAction> pushesForThis;
                if (!TryComputePushesForIndex(ri, d, tempGrid, tempRocks, excludedRockIdx, out pushesForThis))
                    continue;
                // apply pushesForThis already mutated tempRocks inside TryComputePushesForIndex
                // check reachable now
                if (CanPlayerReach(start, target, tempGrid, tempRocks))
                {
                    // append pushes and return
                    accumulated.AddRange(pushesForThis);
                    return true;
                }
                // else, try deeper sequences
                List<PushAction> deeperAccum = new List<PushAction>(accumulated);
                deeperAccum.AddRange(pushesForThis);
                if (CanPlayerReachWithPushDFS(start, target, tempGrid, tempRocks, excludedRockIdx, depth + 1, deeperAccum))
                {
                    // copy back the found sequence
                    accumulated.Clear();
                    accumulated.AddRange(deeperAccum);
                    return true;
                }
            }
        }
        return false;
    }

    private bool TryComputePushesForIndex(int rockIndex, Vector2Int dir, GridCellType[,] simGrid, List<Vector2Int> tempRocks, int excludedRockIdx, out List<PushAction> outPushes)
    {
        outPushes = new List<PushAction>();
        // use a helper that mutates tempRocks and fills outPushes
        return TryComputePushesRecursive(rockIndex, dir, simGrid, tempRocks, 0, outPushes, excludedRockIdx);
    }

    private bool TryComputePushesRecursive(int rockIndex, Vector2Int dir, GridCellType[,] simGrid, List<Vector2Int> tempRocks, int depth, List<PushAction> pushes, int excludedRockIdx)
    {
        if (depth > maxPushDepth) return false;
        if (rockIndex < 0 || rockIndex >= tempRocks.Count) return false;
        Vector2Int curPos = tempRocks[rockIndex];
        Vector2Int targetPos = curPos + dir;
        // bounds and wall
        if (!grid.IsInsideGrid(targetPos)) return false;
        if (simGrid[targetPos.x, targetPos.y] == GridCellType.Wall) return false;
        // avoid pushing into borders or corners (same heuristics as generator)
        if (IsBorder(targetPos)) return false;
        if (IsCornerPosition(simGrid, targetPos)) return false;
        // check if another rock occupies targetPos
        int hit = tempRocks.FindIndex(r => r == targetPos);
        if (hit == -1)
        {
            // can move current rock into empty target
            pushes.Add(new PushAction(rockIndex, targetPos));
            // apply to tempRocks
            tempRocks[rockIndex] = targetPos;
            return true;
        }
        // if hit is the excluded rock, cannot push it
        if (hit == excludedRockIdx) return false;
        // otherwise attempt to push that rock further
        if (depth + 1 > maxPushDepth) return false;
        if (!TryComputePushesRecursive(hit, dir, simGrid, tempRocks, depth + 1, pushes, excludedRockIdx))
            return false;
        // after successfully pushing the blocking rock, move current
        pushes.Add(new PushAction(rockIndex, targetPos));
        tempRocks[rockIndex] = targetPos;
        return true;
    }

    // --- CAMBIO AQUÍ ---
    // Devuelve un 'LevelState' en lugar de void o IEnumerator
    public LevelState GenerateLevel(int numRocks, int numHoles, int numWalls)
    {
        grid.ClearLevel();

        // 1) Colocar paredes aleatorias
        List<Vector2Int> walls = new List<Vector2Int>();
        int attempts = 0;
        while (walls.Count < numWalls && attempts < 2000)
        {
            attempts++;
            Vector2Int p = new Vector2Int(Random.Range(0, grid.width), Random.Range(0, grid.height));
            if (grid.GetCell(p) == GridCellType.Empty)
                walls.Add(p);
        }
        grid.SpawnWalls(walls); // Spawneamos estáticos

        // 2) Colocar meta
        Vector2Int goalPos;
        do
        {
            goalPos = new Vector2Int(Random.Range(1, grid.width - 1), Random.Range(1, grid.height - 1));
        } while (grid.GetCell(goalPos) != GridCellType.Empty);
        grid.PlaceGoal(goalPos); // Spawneamos estáticos

        // 3) Validar parámetros
        if (numHoles < 1) numHoles = 1;
        if (numRocks < numHoles) numRocks = numHoles;

        // 4) Colocar agujeros
        List<Vector2Int> holes = grid.GenerateRandomHoles(numHoles);
        List<Vector2Int> finalHoles = new List<Vector2Int>();
        HashSet<Vector2Int> used = new HashSet<Vector2Int>(holes);
        foreach (var h in holes)
        {
            if (!IsBorder(h))
            {
                finalHoles.Add(h);
                continue;
            }
            // (Lógica de reemplazo de agujeros en borde...)
            bool found = false;
            for (int attempt = 0; attempt < 500; attempt++)
            {
                Vector2Int cand = new Vector2Int(Random.Range(1, grid.width - 1), Random.Range(1, grid.height - 1));
                if (used.Contains(cand)) continue;
                if (grid.GetCell(cand) != GridCellType.Empty) continue;
                finalHoles.Add(cand);
                used.Add(cand);
                found = true;
                break;
            }
            if (!found)
            {
                finalHoles.Add(h);
                if (debugSimulation) Debug.LogWarning($"[BackFromGoal] No replacement found for border hole {h}, keeping it.");
            }
        }
        
        if (finalHoles != null && finalHoles.Count > 0)
        {
            grid.SpawnHoles(finalHoles); // Spawneamos estáticos
            if (debugSimulation) Debug.Log($"[BackFromGoal] Spawned {finalHoles.Count} hole prefabs (final)");
        }
        if (finalHoles.Count < numHoles)
        {
             // (Lógica de ajuste de numRocks...)
            if (debugSimulation) Debug.LogWarning($"[BackFromGoal] Could only generate {finalHoles.Count} holes instead of requested {numHoles}");
            if (numRocks < finalHoles.Count)
            {
                if (debugSimulation) Debug.Log($"[BackFromGoal] Increasing numRocks from {numRocks} to {finalHoles.Count} to match actual holes");
                numRocks = finalHoles.Count;
            }
        }

        // 5) Colocar rocas (primero en agujeros)
        List<Vector2Int> rocksPos = new List<Vector2Int>();
        List<Vector2Int> holesCopy = new List<Vector2Int>(finalHoles);
        int placeInHoles = Mathf.Min(numRocks, holesCopy.Count);
        for (int i = 0; i < placeInHoles; i++)
        {
            int idx = Random.Range(0, holesCopy.Count);
            rocksPos.Add(holesCopy[idx]);
            holesCopy.RemoveAt(idx);
        }

        // 6) Preparar simulación
        GridCellType[,] simGrid = new GridCellType[grid.width, grid.height];
        for (int x = 0; x < grid.width; x++)
            for (int y = 0; y < grid.height; y++)
                simGrid[x, y] = grid.GetCell(new Vector2Int(x, y));

        foreach (var r in rocksPos)
            simGrid[r.x, r.y] = GridCellType.Rock;

        Vector2Int playerPos = goalPos;
        List<int> holeRockIndices = new List<int>();
        for (int i = 0; i < rocksPos.Count; i++)
            if (finalHoles.Contains(rocksPos[i]))
                holeRockIndices.Add(i);

        if (debugSimulation)
            Debug.Log($"[BackFromGoal] goal={goalPos}, holes={string.Join(",", finalHoles)}, initialRocks={string.Join(",", rocksPos)}");

        // 7) Retroceso global corregido: jalar todas las rocas fuera de los agujeros
        Dictionary<int, Vector2Int> lastMove = new Dictionary<int, Vector2Int>();

        for (int step = 0; step < backwardsSteps; step++)
        {
            bool anyMoved = false;

            for (int i = 0; i < rocksPos.Count; i++)
            {
                Vector2Int rpos = rocksPos[i];

                if (MinDistanceToHoles(rpos, finalHoles) > scatterDistance + 2)
                    continue;

                Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
                System.Array.Sort(dirs, (a, b) =>
                {
                    int da = MinDistanceToHoles(rpos + a, finalHoles);
                    int db = MinDistanceToHoles(rpos + b, finalHoles);
                    return db.CompareTo(da); // priorizar alejamiento
                });

                foreach (var dir in dirs)
                {
                    if (lastMove.ContainsKey(i) && lastMove[i] == -dir)
                        continue;

                    Vector2Int targ = rpos + dir;
                    Vector2Int behind = rpos - dir;

                    if (!grid.IsInsideGrid(targ) || !grid.IsInsideGrid(behind))
                        continue;
                    if (simGrid[targ.x, targ.y] == GridCellType.Wall) continue;
                    if (IsRockAtPosition(targ, rocksPos)) continue;
                    if (IsBorder(targ)) continue;
                    if (IsCornerPosition(simGrid, targ)) continue;
                    
                    List<PushAction> pushesToReach; 
                    if (!CanPlayerReachWithPush(playerPos, behind, simGrid, rocksPos, i, out pushesToReach))
                    {
                        continue;
                    }

                    // (La heurística anti-muro de 1 celda ya fue eliminada)

                    // mover jugador detrás y actualizar
                    playerPos = behind;
                    simGrid[rpos.x, rpos.y] = finalHoles.Contains(rpos) ? GridCellType.Hole : GridCellType.Empty;
                    simGrid[targ.x, targ.y] = GridCellType.Rock;
                    rocksPos[i] = targ;
                    playerPos = rpos; // termina donde estaba la roca
                    lastMove[i] = dir;

                    if (debugSimulation)
                        Debug.Log($"[BackFromGoal] Step {step}: rock {i} {rpos}->{targ}, player {behind}->{rpos}");

                    anyMoved = true;
                    break; // siguiente roca
                }
            }

            if (!anyMoved)
            {
                if (debugSimulation) Debug.Log("[BackFromGoal] no rocks moved this iteration, stopping.");
                break;
            }
        }


        // 8) --- CAMBIO FINAL ---
        // NO SPAWNEAR, DEVOLVER EL ESTADO PARA EL OPTIMIZADOR
        // grid.SpawnRocks(rocksPos);   <-- ELIMINADO
        // grid.SpawnPlayer(playerPos); <-- ELIMINADO

        if (debugSimulation)
            Debug.Log($"[BackFromGoal] Generación inicial terminada. Player: {playerPos}, Rocks: {rocksPos.Count}");

        // El 'simGrid' que guardamos es el estado *antes* de mover las rocas.
        // El optimizador usará el 'grid.GetCell()' original para muros/vacío.
        // Guardamos 'simGrid' por si acaso, pero 'walls' es más útil.
        GridCellType[,] finalGridLayout = new GridCellType[grid.width, grid.height];
         for (int x = 0; x < grid.width; x++)
            for (int y = 0; y < grid.height; y++)
                finalGridLayout[x, y] = grid.GetCell(new Vector2Int(x, y));

        return new LevelState(finalGridLayout, finalHoles, goalPos, walls, rocksPos, playerPos);
    }


    // --- Helpers (Todos permanecen igual) ---
    private bool IsRockAtPosition(Vector2Int pos, List<Vector2Int> rocksPos)
    {
        return rocksPos.FindIndex(r => r == pos) != -1;
    }

    private bool IsBorder(Vector2Int pos)
    {
        return pos.x == 0 || pos.y == 0 || pos.x == grid.width - 1 || pos.y == grid.height - 1;
    }

    private bool IsCornerPosition(GridCellType[,] simGrid, Vector2Int pos)
    {
        System.Func<Vector2Int, bool> IsWallOrOut = (Vector2Int p) =>
        {
            if (!grid.IsInsideGrid(p)) return true;
            return simGrid[p.x, p.y] == GridCellType.Wall;
        };
        bool vert = IsWallOrOut(pos + Vector2Int.up) || IsWallOrOut(pos + Vector2Int.down);
        bool hor = IsWallOrOut(pos + Vector2Int.left) || IsWallOrOut(pos + Vector2Int.right);
        return vert && hor;
    }

    private int MinDistanceToHoles(Vector2Int pos, List<Vector2Int> holes)
    {
        int min = int.MaxValue;
        foreach (var h in holes)
        {
            int d = Mathf.Abs(h.x - pos.x) + Mathf.Abs(h.y - pos.y);
            if (d < min) min = d;
        }
        return min == int.MaxValue ? 0 : min;
    }

    private bool CanPlayerReach(Vector2Int start, Vector2Int target, GridCellType[,] simGrid, List<Vector2Int> rocksPos)
    {
        if (start == target) return true;
        int w = grid.width, h = grid.height;
        bool[,] visited = new bool[w, h];
        Queue<Vector2Int> q = new Queue<Vector2Int>();
        q.Enqueue(start); visited[start.x, start.y] = true;
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            foreach (var d in dirs)
            {
                Vector2Int nxt = cur + d;
                if (nxt.x < 0 || nxt.y < 0 || nxt.x >= w || nxt.y >= h) continue;
                if (visited[nxt.x, nxt.y]) continue;
                if (simGrid[nxt.x, nxt.y] == GridCellType.Wall) continue;
                if (IsRockAtPosition(nxt, rocksPos)) continue;
                if (nxt == target) return true;
                visited[nxt.x, nxt.y] = true; q.Enqueue(nxt);
            }
        }
        return false;
    }
}