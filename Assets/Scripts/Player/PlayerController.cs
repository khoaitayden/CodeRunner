using UnityEngine;
using System.Collections; // Required for Coroutines
using System.Collections.Generic;
using UnityEngine.Events; // Required for UnityEvent

public enum Direction { Up, Right, Down, Left }

public class PlayerController : MonoBehaviour
{
    [Header("Player State")]
    public Vector2Int currentPosition;
    public Direction currentDirection;
    public Direction previousDirection;
    public Animator animator;

    [Header("Execution Settings")]
    [Tooltip("The delay in seconds between each command execution.")]
    public float moveDelay;
    [HideInInspector] public int moveCount = 0;
    [Header("Events")]
    public UnityEvent OnSequenceStart;
    public UnityEvent OnSequenceComplete; // Fired on success
    public UnityEvent OnSequenceFail;     // Fired on failure (e.g., finishing in the wrong spot)
    public UnityEvent<int> OnStepTaken;

    private BoardManager boardManager;
    private Dictionary<Direction, Vector2Int> directionVectors;
    private bool isExecuting = false;
    private Coroutine executionCoroutine;

    void Awake()
    {
        directionVectors = new Dictionary<Direction, Vector2Int>
        {
            { Direction.Up,    Vector2Int.up },
            { Direction.Right, Vector2Int.right },
            { Direction.Down,  Vector2Int.down },
            { Direction.Left,  Vector2Int.left }
        };
    }

    public void Initialize(BoardManager manager, Vector2Int startPosition, Direction startingDirection)
    {
        boardManager = manager;
        currentPosition = startPosition;
        currentDirection = startingDirection;
        
        previousDirection = currentDirection;
        transform.position = boardManager.GridToWorldPosition(currentPosition);
        UpdateVisuals();
        moveCount = 0;
    }

    public void RunCommandSequence(List<Command> commands)
    {
        if (isExecuting) return;
        // --- STORE A REFERENCE TO THE COROUTINE ---
        executionCoroutine = StartCoroutine(ExecuteSequenceCoroutine(commands));
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
        }
        else
        {
            Debug.Log("--- SEQUENCE FAILED (Not on End tile) ---");
            
            // --- SIMPLIFIED LOGIC ---
            // The fall logic is now the same as the "run out of commands" logic.
            // We can simulate a "Fall" to reuse the code from MoveForward's Fall case.
            // Or, more cleanly, just handle it directly here.
            HaltExecution();
            OnSequenceFail?.Invoke();
            TransitionManager.Instance.PlayTransition(() => boardManager.RestartLevel());
        }
    }


    public void HaltExecution()
    {
        isExecuting = false;
        if (executionCoroutine != null)
        {
            StopCoroutine(executionCoroutine);
            executionCoroutine = null;
        }
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
                Debug.Log("Moved Forward to: "
                    + currentPosition);
                boardManager.PlayerLandedOnTile(currentPosition);
                break;
            case MoveResult.Blocked:
                Debug.Log("Move failed. Blocked by a wall.");
                break;
            case MoveResult.Fall:
                // --- THIS IS THE FIX ---
                Debug.Log("Move failed. Fell off the edge, into air, or onto an inactive bridge.");
                
                // 1. Immediately stop all running coroutines on THIS player instance.
                HaltExecution(); 
                
                // 2. Fire the failure event. This will tell the UI to stop the button animation.
                OnSequenceFail?.Invoke(); 
                
                // 3. Trigger the transition and restart.
                TransitionManager.Instance.PlayTransition(() => boardManager.RestartLevel());
                
                // OLD CODE: boardManager.RestartLevel(); // DELETE THIS LINE
                break;
        }
    }

    public void TurnLeft()
    {
        // Up(0)->Left(3), Right(1)->Up(0), Down(2)->Right(1), Left(3)->Down(2)
        currentDirection = (Direction)(((int)currentDirection + 3) % 4); 
        UpdateVisuals();
    }

    public void TurnRight()
    {
        // Up(0)->Right(1), Right(1)->Down(2), etc.
        currentDirection = (Direction)(((int)currentDirection + 1) % 4); 
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        transform.position = new Vector3(boardManager.GridToWorldPosition(currentPosition).x, boardManager.GridToWorldPosition(currentPosition).y + 0.3f, transform.position.z);

        animator.SetInteger("FacingDirection", (int)currentDirection);
    }



}