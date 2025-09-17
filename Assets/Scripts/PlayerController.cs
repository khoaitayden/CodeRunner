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
        
        // Ask the board manager for the result of the attempted move
        MoveResult result = boardManager.CheckMove(targetPosition);

        switch (result)
        {
            case MoveResult.Success:
                // Valid move! Update logical position and visuals.
                currentPosition = targetPosition;
                UpdateVisuals();
                Debug.Log("Moved Forward to: " + currentPosition);
                
                // Notify the board manager that we have landed on a new tile.
                boardManager.PlayerLandedOnTile(currentPosition);
                break;

            case MoveResult.Blocked:
                // The move is blocked by a wall. Do nothing but give feedback.
                Debug.Log("Move failed. Blocked by a wall.");
                // Here you could play a "bump" sound or animation.
                break;

            case MoveResult.Fall:
                // The player tried to move to a hazard tile. Trigger a reset.
                Debug.Log("Move failed. Fell off the edge, into air, or onto an inactive bridge.");
                
                // Optionally, you can play a "fall" animation here before restarting.
                // For now, we restart immediately.
                boardManager.RestartLevel();
                break;
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