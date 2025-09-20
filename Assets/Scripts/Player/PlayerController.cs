using UnityEngine;
using System.Collections; // Required for Coroutines
using System.Collections.Generic;
using UnityEngine.Events; // Required for UnityEvent

public enum Direction { Down, Left, Up, Right}

public class PlayerController : MonoBehaviour
{
    [Header("Player State")]
    public Vector2Int currentPosition;
    public Direction currentDirection;
    [Tooltip("Sprites for each direction: Up, Right, Down, Left")]
    public List<Sprite> directionSprites; 

    
    [Header("Execution Settings")]
    [Tooltip("The delay in seconds between each command execution.")]
    public float moveDelay = 0.5f;
    [HideInInspector] public int moveCount = 0;
    [Header("Events")]
    public UnityEvent OnSequenceStart;
    public UnityEvent OnSequenceComplete; // Fired on success
    public UnityEvent OnSequenceFail;     // Fired on failure (e.g., finishing in the wrong spot)

    private BoardManager boardManager;
    private Dictionary<Direction, Vector2Int> directionVectors;
    private Dictionary<Direction, Quaternion> directionRotations;
    private bool isExecuting = false;
    public UnityEvent<int> OnStepTaken;

    void Awake()
    {
        directionVectors = new Dictionary<Direction, Vector2Int>
        {
            { Direction.Up, Vector2Int.up },
            { Direction.Right,  Vector2Int.right },
            { Direction.Down, Vector2Int.down },
            { Direction.Left,  Vector2Int.left }
        };

        directionRotations = new Dictionary<Direction, Quaternion>
        {
            { Direction.Up, Quaternion.Euler(0, 0, 0) },
            { Direction.Right,  Quaternion.Euler(0, 0, -90) },
            { Direction.Down, Quaternion.Euler(0, 0, 180) },
            { Direction.Left,  Quaternion.Euler(0, 0, 90) }
        };
    }

    public void Initialize(BoardManager manager, Vector2Int startPosition)
    {
        boardManager = manager;
        currentPosition = startPosition;
        currentDirection = Direction.Up;
        transform.position = boardManager.GridToWorldPosition(currentPosition);
        transform.rotation = directionRotations[currentDirection];
    }

    public void RunCommandSequence(List<Command> commands)
    {
        if (isExecuting)
        {
            Debug.LogWarning("Already executing a sequence!");
            return;
        }
        moveCount = 0;
        OnStepTaken?.Invoke(moveCount);
        StartCoroutine(ExecuteSequenceCoroutine(commands));
    }

    private IEnumerator ExecuteSequenceCoroutine(List<Command> commands)
    {
        isExecuting = true;
        OnSequenceStart?.Invoke();
        Debug.Log("--- SEQUENCE START ---");

        // Delegate the actual execution to our recursive helper coroutine
        yield return StartCoroutine(ExecuteCommands(commands));
        
        // This code runs only after the entire sequence is complete
        isExecuting = false;
        CheckFinalPosition();
    }

    /// <summary>
    /// A recursive coroutine that executes a list of commands, including loops.
    /// </summary>
    private IEnumerator ExecuteCommands(List<Command> commandsToExecute)
    {
        foreach (Command command in commandsToExecute)
        {
            // If a previous command caused a level reset, stop everything.
            if (!isExecuting) yield break;

            if (command.Type == CommandType.Loop)
            {
                Debug.Log($"--- Starting Loop (x{command.RepeatCount}) ---");
                for (int i = 0; i < command.RepeatCount; i++)
                {
                    yield return StartCoroutine(ExecuteCommands(command.SubCommands));
                    if (!isExecuting) yield break; // Stop immediately if a sub-command failed
                }
                Debug.Log("--- Ending Loop ---");
            }
            else
            {
                moveCount++;
                OnStepTaken?.Invoke(moveCount);
                ExecuteSimpleCommand(command.Type);
                yield return new WaitForSeconds(moveDelay);
            }
        }
    }
    
    private void ExecuteSimpleCommand(CommandType type)
    {
        switch (type)
        {
            case CommandType.MoveForward: MoveForward(); break;
            case CommandType.TurnLeft: TurnLeft(); break;
            case CommandType.TurnRight: TurnRight(); break;
        }
    }

    private void CheckFinalPosition()
    {
        TileData finalTile = boardManager.GetTileAtPosition(currentPosition);
        if (finalTile != null && finalTile.tileTypeEnum == TileType.End)
        {
            Debug.Log("--- SEQUENCE COMPLETE (Success) ---");
            OnSequenceComplete?.Invoke();
            // The BoardManager's PlayerLandedOnTile will handle level progression.
        }
        else
        {
            Debug.Log("--- SEQUENCE FAILED (Not on End tile) ---");
            OnSequenceFail?.Invoke();
            StartCoroutine(DelayedRestart());
        }
    }

    private IEnumerator DelayedRestart()
    {
        yield return new WaitForSeconds(1.5f);
        if (boardManager != null) boardManager.RestartLevel();
    }

    /// <summary>
    /// Called by the BoardManager during a level reset to stop any running coroutines.
    /// </summary>
    public void HaltExecution()
    {
        isExecuting = false;
        StopAllCoroutines();
    }

    // --- Core Action Methods ---

    public void MoveForward()
    {
        Vector2Int targetPosition = currentPosition + directionVectors[currentDirection];
        MoveResult result = boardManager.CheckMove(targetPosition);

        switch (result)
        {
            case MoveResult.Success:
                currentPosition = targetPosition;
                UpdateVisuals();
                Debug.Log("Moved Forward to: " + currentPosition);
                boardManager.PlayerLandedOnTile(currentPosition);
                break;
            case MoveResult.Blocked:
                Debug.Log("Move failed. Blocked by a wall.");
                break;
            case MoveResult.Fall:
                Debug.Log("Move failed. Fell off the edge, into air, or onto an inactive bridge.");
                boardManager.RestartLevel();
                break;
        }
    }

    public void TurnLeft()
    {
        currentDirection = (Direction)(((int)currentDirection + 3) % 4);
        UpdateVisuals();
        Debug.Log("Turned Left. New direction: " + currentDirection);
    }

    public void TurnRight()
    {
        currentDirection = (Direction)(((int)currentDirection + 1) % 4);
        UpdateVisuals();
        Debug.Log("Turned Right. New direction: " + currentDirection);
    }

    private void UpdateVisuals()
    {
        transform.position = new Vector3(boardManager.GridToWorldPosition(currentPosition).x, boardManager.GridToWorldPosition(currentPosition).y + 0.3f, transform.position.z);
        if (directionSprites != null && directionSprites.Count == 4)
        {
            GetComponent<SpriteRenderer>().sprite = directionSprites[(int)currentDirection];
        }
    }
}