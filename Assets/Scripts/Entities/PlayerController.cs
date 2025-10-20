using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveDelay = 0.15f;
    private bool canMove = true;
    private bool levelCompleted = false;

    private GridManager grid;
    private Vector2Int gridPos;
    private bool initialized = false;

    public Vector2Int GridPos { set { gridPos = value; } }

    public bool CanMove
    {
        get { return canMove; }
        set { canMove = value; }
    }

    private void Start()
    {
        grid = FindFirstObjectByType<GridManager>();
        gridPos = new Vector2Int(grid.playerPos.x, grid.playerPos.y);
        transform.position = grid.GridToWorld(gridPos);
        grid.SetCell(gridPos, GridCellType.Player);


        // Si no se inicializó previamente (via InitializePosition), usar (0,0) por defecto.
        if (!initialized)
        {
            gridPos = new Vector2Int(0, 0);
            transform.position = grid.GridToWorld(gridPos);
            grid.SetCell(gridPos, GridCellType.Player);
            initialized = true;
        }
    }

    private void Update()
    {
        // Si estamos sobre la meta y se desbloquea mientras estamos ah�, completamos el nivel
        if (!levelCompleted && grid != null && grid.GetCell(gridPos) == GridCellType.Goal && grid.IsGoalUnlocked())
        {
            levelCompleted = true;
            StartCoroutine(CompleteLevelRoutine());
            return;
        }

        if (!canMove) return;

        Vector2Int dir = Vector2Int.zero;

        if (Input.GetKey(KeyCode.W)) dir = Vector2Int.up;
        if (Input.GetKey(KeyCode.S)) dir = Vector2Int.down;
        if (Input.GetKey(KeyCode.A)) dir = Vector2Int.left;
        if (Input.GetKey(KeyCode.D)) dir = Vector2Int.right;

        if (dir != Vector2Int.zero)
            StartCoroutine(Move(dir));
    }

    private System.Collections.IEnumerator Move(Vector2Int dir)
    {
        canMove = false;

        Vector2Int newPos = gridPos + dir;

        if (grid.IsInsideGrid(newPos))
        {
            GridCellType targetCell = grid.GetCell(newPos);

            switch (targetCell)
            {
                case GridCellType.Empty:
                    MovePlayerTo(newPos);
                    break;

                case GridCellType.Goal:
                    MovePlayerTo(newPos);

                    if (grid.IsGoalUnlocked())
                    {
                        // Marcar completado y ejecutar la secuencia de victoria
                        levelCompleted = true;
                        yield return new WaitForSeconds(0.1f);
                        LevelCompleted();
                        yield break; // solo termina si el nivel se complet�
                    }
                    else
                    {
                        Debug.Log("La meta est� bloqueada, hay agujeros sin cubrir");
                        // no hacemos yield break, la coroutine continuar�
                    }
                    break; // permitir que canMove se resetee al final


                case GridCellType.Wall:
                    // No se puede mover a trav�s de paredes
                    break;

                case GridCellType.Hole:
                    // Jugador cae en agujero ? reiniciar nivel
                    grid.SetCell(gridPos, GridCellType.Empty);
                    yield return new WaitForSeconds(0.1f); // peque�o delay para animaci�n
                    grid.ClearLevel();
                    yield break;

                case GridCellType.Rock:
                    Vector2Int rockNewPos = newPos + dir;
                    Rock rock = FindRockAtPosition(newPos);

                    if (rock != null && grid.IsInsideGrid(rockNewPos))
                    {
                        GridCellType rockTarget = grid.GetCell(rockNewPos);

                        if (rockTarget == GridCellType.Empty)
                        {
                            // Empuja roca a celda vac�a
                            MoveRockTo(rock, rockNewPos);
                            MovePlayerTo(newPos);
                        }
                        else if (rockTarget == GridCellType.Hole)
                        {
                            // Roca cae en agujero
                            // Roca cae en agujero: marcar el agujero como cubierto por la roca y eliminar el objeto
                            grid.SetCell(rock.gridPos, GridCellType.Empty);
                            grid.SetCell(rockNewPos, GridCellType.Rock); // el agujero queda ahora "cubierto" por una roca
                            Destroy(rock.gameObject);
                            MovePlayerTo(newPos);
                        }
                        else if (rockTarget == GridCellType.Wall)
                        {
                            // No se puede empujar la roca contra una pared: no mover nada
                            break;
                        }
                    }
                    break;
            }
        }

        yield return new WaitForSeconds(moveDelay);
        canMove = true;
    }

    private void MovePlayerTo(Vector2Int newPos)
    {
        // Restaurar la celda anterior: si era la meta, dejarla como Goal, si no, poner Empty
        if (gridPos == grid.goalPos)
            grid.SetCell(gridPos, GridCellType.Goal);
        else
            grid.SetCell(gridPos, GridCellType.Empty);

        gridPos = newPos;

        // Si la nueva posici�n es la meta, no sobreescribirla (mantener Goal)
        if (grid.GetCell(gridPos) != GridCellType.Goal)
            grid.SetCell(gridPos, GridCellType.Player);

        transform.position = grid.GridToWorld(gridPos);
    }

    private void MoveRockTo(Rock rock, Vector2Int newPos)
    {
        grid.SetCell(rock.gridPos, GridCellType.Empty);
        rock.gridPos = newPos;
        grid.SetCell(rock.gridPos, GridCellType.Rock);
        rock.transform.position = grid.GridToWorld(newPos);
    }

    public Rock FindRockAtPosition(Vector2Int pos)
    {
        Rock[] rocks = Object.FindObjectsByType<Rock>(FindObjectsSortMode.None);
        foreach (Rock r in rocks)
        {
            if (r.gridPos == pos)
                return r;
        }
        return null;
    }

    public void InitializePosition(Vector2Int startPos)
    {
        // Asegurar referencia al GridManager (puede que InitializePosition se llame antes de Start)
        if (grid == null)
            grid = FindFirstObjectByType<GridManager>();

        gridPos = startPos;

        if (grid != null)
        {
            transform.position = grid.GridToWorld(gridPos);
            // Si el startPos es la meta, mantener la celda como Goal en lugar de Player
            if (grid.GetCell(gridPos) != GridCellType.Goal)
                grid.SetCell(gridPos, GridCellType.Player);
        }
        else
        {
            // Fallback: si no hay GridManager, solo posicionar en world (0,0)
            transform.position = Vector3.zero;
        }

        canMove = true;
        initialized = true;
    }

    private void LevelCompleted()
    {
        Debug.Log("�Nivel completado!");
        // Aqu� puedes cargar el siguiente nivel o mostrar pantalla de victoria
    }

    private System.Collections.IEnumerator CompleteLevelRoutine()
    {
        // Peque�o delay para permitir animaciones o sonidos
        yield return new WaitForSeconds(0.1f);
        LevelCompleted();
    }
}
