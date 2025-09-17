using UnityEngine;
using System.Collections.Generic;

// Enum to represent the four cardinal directions. Much cleaner than using numbers or vectors.
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
    public Vector2Int currentPosition; // The player's logical grid position
    public Direction currentDirection; // The direction the player is facing

    [Header("Movement Settings")]
    public float rotationSpeed = 180f; // Degrees per second for turning
    
    // Private reference to the board manager
    private BoardManager boardManager;

    // Dictionaries to map directions to vectors and rotations for easy lookups
    private Dictionary<Direction, Vector2Int> directionVectors;
    private Dictionary<Direction, Quaternion> directionRotations;

    void Awake()
    {
        // Initialize the lookup dictionaries
        directionVectors = new Dictionary<Direction, Vector2Int>
        {
            { Direction.North, Vector2Int.up },
            { Direction.East, Vector2Int.right },
            { Direction.South, Vector2Int.down },
            { Direction.West, Vector2Int.left }
        };

        directionRotations = new Dictionary<Direction, Quaternion>
        {
            { Direction.North, Quaternion.Euler(0, 0, 0) },
            { Direction.East, Quaternion.Euler(0, 0, -90) },
            { Direction.South, Quaternion.Euler(0, 0, 180) },
            { Direction.West, Quaternion.Euler(0, 0, 90) }
        };
    }

    // This function will be called by the BoardManager to set up the player
    public void Initialize(BoardManager manager, Vector2Int startPosition)
    {
        boardManager = manager;
        currentPosition = startPosition;
        currentDirection = Direction.North; // Default starting direction

        // Instantly snap to the correct visual position and rotation
        transform.position = boardManager.GridToWorldPosition(currentPosition);
        transform.rotation = directionRotations[currentDirection];
    }
    
    // --- COMMAND METHODS ---
    // These are the public methods your command execution system will call.

    public void TurnLeft()
    {
        currentDirection = (Direction)(((int)currentDirection + 3) % 4); // A little math to cycle enum
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
        
        // Ask the board manager if the target tile is walkable
        TileData targetTile = boardManager.GetTileAtPosition(targetPosition);
        
        if (targetTile != null && targetTile.tileTypeEnum != TileType.Wall)
        {
            // Valid move! Update logical position.
            currentPosition = targetPosition;
            UpdateVisuals();
            Debug.Log("Moved Forward to: " + currentPosition);
            
            // Here you would add logic for landing on special tiles (switches, end, etc.)
        }
        else
        {
            // Invalid move (wall or out of bounds)
            Debug.Log("Move failed. Blocked by wall or edge.");
        }
    }

    private void UpdateVisuals()
    {
        // For now, this is instant. Later you can add smooth animation (coroutines).
        transform.position = boardManager.GridToWorldPosition(currentPosition);
        transform.rotation = directionRotations[currentDirection];
    }

    // --- TEST CODE ---
    // Remove or comment this out when you implement your card/command system.
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            MoveForward();
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            TurnLeft();
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            TurnRight();
        }
    }
}