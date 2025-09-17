using UnityEngine;
using System.Collections; // Required for Coroutines
using System.Collections.Generic;
using UnityEngine.Events; // Required for UnityEvent

public enum Direction
{
    North,
    East,
    South,
    West
}

public class PlayerController : MonoBehaviour
{
    [Header("Player State")]
    public Vector2Int currentPosition;
    public Direction currentDirection;

    [Header("Execution Settings")]
    [Tooltip("The delay in seconds between each command execution.")]
    public float moveDelay = 0.5f;

    [Header("Events")]
    public UnityEvent OnSequenceStart;
    public UnityEvent OnSequenceComplete;

    private BoardManager boardManager;
    private Dictionary<Direction, Vector2Int> directionVectors;
    private Dictionary<Direction, Quaternion> directionRotations;
    private bool isExecuting = false;

    void Awake()
    {
        directionVectors = new Dictionary<Direction, Vector2Int>
        {
            { Direction.North, Vector2Int.up },
            { Direction.East,  Vector2Int.right },
            { Direction.South, Vector2Int.down },
            { Direction.West,  Vector2Int.left }
        };

        directionRotations = new Dictionary<Direction, Quaternion>
        {
            { Direction.North, Quaternion.Euler(0, 0, 0) },
            { Direction.East,  Quaternion.Euler(0, 0, -90) },
            { Direction.South, Quaternion.Euler(0, 0, 180) },
            { Direction.West,  Quaternion.Euler(0, 0, 90) }
        };
    }

    public void Initialize(BoardManager manager, Vector2Int startPosition)
    {
        boardManager = manager;
        currentPosition = startPosition;
        currentDirection = Direction.North; // Default starting direction

        transform.position = boardManager.GridToWorldPosition(currentPosition);
        transform.rotation = directionRotations[currentDirection];
    }

    // --- NEW: The main entry point for running the sequence ---
    public void RunCommandSequence(List<CommandType> commands)
    {
        if (isExecuting)
        {
            Debug.LogWarning("Already executing a sequence!");
            return;
        }
        StartCoroutine(ExecuteSequenceCoroutine(commands));
    }

    private IEnumerator ExecuteSequenceCoroutine(List<CommandType> commands)
    {
        isExecuting = true;
        OnSequenceStart?.Invoke(); // Fire the "started" event
        Debug.Log("--- SEQUENCE START ---");

        foreach (CommandType command in commands)
        {
            // Execute the current command
            switch (command)
            {
                case CommandType.MoveForward:
                    MoveForward();
                    break;
                case CommandType.TurnLeft:
                    TurnLeft();
                    break;
                case CommandType.TurnRight:
                    TurnRight();
                    break;
            }

            // If a move resulted in a fall, isExecuting will have been set to false by HaltExecution.
            if (!isExecuting)
            {
                Debug.Log("--- SEQUENCE HALTED (Level Reset) ---");
                OnSequenceComplete?.Invoke(); // Re-enable UI
                yield break;
            }

            // Wait for the specified delay before the next command
            yield return new WaitForSeconds(moveDelay);
        }

        Debug.Log("--- SEQUENCE COMPLETE ---");
        isExecuting = false;
        OnSequenceComplete?.Invoke(); // Fire the "completed" event
    }

    // This method is called by the BoardManager when a level is reset to stop the sequence
    public void HaltExecution()
    {
        isExecuting = false;
        StopAllCoroutines();
    }


    // --- COMMAND METHODS (Functionally unchanged, but now called by the coroutine) ---

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

    private void UpdateVisuals()
    {
        transform.position = boardManager.GridToWorldPosition(currentPosition);
        transform.rotation = directionRotations[currentDirection];
    }
}