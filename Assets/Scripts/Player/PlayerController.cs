using UnityEngine;
using System.Collections; // Required for Coroutines
using System.Collections.Generic;
using UnityEngine.Events; // Required for UnityEvent

public enum Direction { Up, Right, Down, Left }

public class PlayerController : MonoBehaviour
{
    [Header("Player State")]
    [SerializeField] private Vector2Int currentPosition;
    [SerializeField] private Direction currentDirection;
    [SerializeField] private Direction previousDirection;
    [SerializeField] private Sprite upSprite;
    [SerializeField] private Sprite rightSprite;
    [SerializeField] private Sprite downSprite;
    [SerializeField] private Sprite leftSprite;
    [SerializeField] private float moveDelay;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip fallSound;
    [SerializeField] private AudioClip hitWallSound;
    [SerializeField] private AudioClip moveSound;
    [SerializeField] private AudioClip turnSound;
    [HideInInspector] public int moveCount = 0;

    [Header("Events")]
    public UnityEvent OnSequenceStart;
    public UnityEvent OnSequenceComplete;
    public UnityEvent OnSequenceFail;
    public UnityEvent<int> OnStepTaken;
    private SpriteRenderer spriteRenderer;
    private BoardManager boardManager;
    private Dictionary<Direction, Vector2Int> directionVectors;
    private bool isExecuting = false;
    private Coroutine executionCoroutine;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
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
        executionCoroutine = StartCoroutine(ExecuteSequenceCoroutine(commands));
    }

    private IEnumerator ExecuteSequenceCoroutine(List<Command> commands)
    {
        isExecuting = true;
        OnSequenceStart?.Invoke();
        Debug.Log("--- SEQUENCE START ---");

        yield return StartCoroutine(ExecuteCommands(commands));

        isExecuting = false;
        CheckFinalPosition();
    }

    private IEnumerator ExecuteCommands(List<Command> commandsToExecute)
    {
        foreach (Command command in commandsToExecute)
        {
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
                SFXManager.Instance.PlayRandomPitchSoundEffect(moveSound, 0.8f, 1.5f);
                break;
            case MoveResult.Blocked:
                SFXManager.Instance.PlayRandomPitchSoundEffect(hitWallSound, 0.5f, 1.5f);
                StartCoroutine(HitWallAnimation());
                Debug.Log("Move failed. Blocked by a wall");
                break;
            case MoveResult.Fall:
                StartCoroutine(FallAnimation());
                Debug.Log("Move failed. Fell off the edge, into air, or onto an inactive bridge");
                HaltExecution();
                OnSequenceFail?.Invoke();
                TransitionManager.Instance.PlayTransition(() => boardManager.RestartLevel());
                SFXManager.Instance.PlayRandomPitchSoundEffect(fallSound, 0.8f, 1.2f);
                break;
        }
    }

    public void TurnLeft()
    {
        SFXManager.Instance.PlayRandomPitchSoundEffect(turnSound, 0.8f, 1.5f);
        currentDirection = (Direction)(((int)currentDirection + 3) % 4);
        UpdateVisuals();
    }

    public void TurnRight()
    {
        SFXManager.Instance.PlayRandomPitchSoundEffect(turnSound, 0.8f, 1.5f);
        currentDirection = (Direction)(((int)currentDirection + 1) % 4);
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        transform.position = new Vector3(boardManager.GridToWorldPosition(currentPosition).x, boardManager.GridToWorldPosition(currentPosition).y + 0.3f, transform.position.z);

        switch (currentDirection)
        {
            case Direction.Up:
                spriteRenderer.sprite = upSprite;
                break;
            case Direction.Right:
                spriteRenderer.sprite = rightSprite;
                break;
            case Direction.Down:
                spriteRenderer.sprite = downSprite;
                break;
            case Direction.Left:
                spriteRenderer.sprite = leftSprite;
                break;
        }
    }
    private IEnumerator HitWallAnimation()
    {
        float duration = moveDelay;
        float elapsed = 0f;

        Vector3 originalPosition = transform.localPosition;
        Vector3 originalScale = transform.localScale;
        Vector3 forwardOffset = transform.forward * 0.1f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float phase = elapsed / duration;
            float triangle = 1f - Mathf.Abs(2f * (phase - 0.5f));
            float jiggleAmount = 0.08f * Mathf.Sin(phase * Mathf.PI * 4f);

            Vector3 offset = forwardOffset * triangle + transform.right * jiggleAmount;
            transform.localPosition = originalPosition + offset;

            float pulse = 1f + 0.1f * triangle;
            transform.localScale = originalScale * pulse;

            yield return null;
        }

        transform.localPosition = originalPosition;
        transform.localScale = originalScale;
    }

    private IEnumerator FallAnimation()
    {
        float duration = moveDelay;
        float elapsed = 0f;

        Vector3 originalPosition = transform.position;
        Vector3 originalScale = transform.localScale;

        // Step 1: Step "up" into the void — just once, at start
        Vector3 steppedUpPosition = originalPosition + Vector3.up * 0.2f;
        transform.position = steppedUpPosition;

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        Color originalColor = spriteRenderer.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            float squashY = Mathf.Lerp(1f, 0.3f, t);

            float shrinkX = Mathf.Lerp(1f, 0.5f, t);

            transform.localScale = new Vector3(shrinkX, squashY, 1f);

            Color fadedColor = originalColor;
            fadedColor.a = Mathf.Lerp(1f, 0f, t);
            spriteRenderer.color = fadedColor;

            yield return null;
        }
        spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
    }
    
}
