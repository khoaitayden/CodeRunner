using UnityEngine;
using System.Collections.Generic;

// Enum to represent the four cardinal directions.
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

    private BoardManager boardManager;
    private Dictionary<Direction, Vector2Int> directionVectors;
    private Dictionary<Direction, Quaternion> directionRotations;

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

    // --- COMMAND METHODS ---

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

        if (boardManager.IsTileWalkable(targetPosition))
        {
            currentPosition = targetPosition;
            UpdateVisuals();
            Debug.Log("Moved Forward to: " + currentPosition);

            // Notify the board manager that we have landed on a new tile.
            boardManager.PlayerLandedOnTile(currentPosition);
        }
        else
        {
            Debug.Log("Move failed. Blocked by wall, inactive bridge, or edge.");
        }
    }

    private void UpdateVisuals()
    {
        // This is an instant snap. Can be replaced with a Coroutine for smooth movement.
        transform.position = boardManager.GridToWorldPosition(currentPosition);
        transform.rotation = directionRotations[currentDirection];
    }

    // --- TEST CODE (Remove when command system is implemented) ---
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.W)) MoveForward();
        if (Input.GetKeyDown(KeyCode.A)) TurnLeft();
        if (Input.GetKeyDown(KeyCode.D)) TurnRight();
    }
}