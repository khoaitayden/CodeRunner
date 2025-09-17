using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System; // Required for System.Enum

/// <summary>
/// A small helper class to link a TileType enum to a TileBase asset in the Inspector.
/// This makes it easy to configure which sprite corresponds to which tile type.
/// </summary>
[System.Serializable]
public class TileMapping
{
    public TileType type;
    public TileBase tileAsset;
}

/// <summary>
/// The main controller for the game board. It handles loading level data from JSON,
/// generating the visual tilemaps, and spawning the player character.
/// It acts as the central point of authority for the state of the game world.
/// </summary>
public class BoardManager : MonoBehaviour
{
    [Header("Level Data")]
    [Tooltip("The JSON file defining the level layout.")]
    public TextAsset levelJson;

    [Header("Object Prefabs")]
    [Tooltip("The prefab for the player character.")]
    public GameObject playerPrefab;

    [Header("Tilemap References")]
    [Tooltip("The tilemap for drawing base layers like floors and walls.")]
    public Tilemap baseLayer;
    [Tooltip("The tilemap for drawing interactable objects like switches and bridges.")]
    public Tilemap interactableLayer;

    [Header("Tile Assets")]
    [Tooltip("A list that maps each TileType enum to its corresponding visual Tile asset.")]
    public List<TileMapping> tileMappings;

    // The internal "brain" of the board. This 2D array holds the logical data for every tile.
    private TileData[,] boardData;
    // A dictionary for fast lookups of Tile assets based on TileType. Populated from tileMappings.
    private Dictionary<TileType, TileBase> tileAssetDictionary;

    void Awake()
    {
        // Convert the inspector list of mappings into a dictionary for fast, O(1) lookups.
        // This is much more efficient than searching through the list every time we draw a tile.
        tileAssetDictionary = new Dictionary<TileType, TileBase>();
        foreach (var mapping in tileMappings)
        {
            if (!tileAssetDictionary.ContainsKey(mapping.type))
            {
                tileAssetDictionary.Add(mapping.type, mapping.tileAsset);
            }
        }
    }

    void Start()
    {
        LoadAndGenerateBoard();
    }

    /// <summary>
    /// The main function that orchestrates the entire level setup process.
    /// </summary>
    public void LoadAndGenerateBoard()
    {
        if (levelJson == null)
        {
            Debug.LogError("FATAL: No Level JSON file has been assigned in the BoardManager Inspector!");
            return;
        }

        // --- 1. Load and Parse Data from JSON ---
        LevelData loadedLevel = JsonUtility.FromJson<LevelData>(levelJson.text);

        // --- 2. Create the Logical Board and Process Tile Data ---
        boardData = new TileData[loadedLevel.width, loadedLevel.height];
        Vector2Int startPosition = Vector2Int.zero;
        bool startFound = false;

        foreach (var tile in loadedLevel.tiles)
        {
            // Robustly parse the string type from JSON into our TileType enum.
            // This prevents errors and allows for typos in the JSON to be caught.
            if (Enum.TryParse(tile.type, true, out TileType parsedType)) // 'true' ignores case
            {
                tile.tileTypeEnum = parsedType;
            }
            else
            {
                Debug.LogWarning($"Unknown tile type '{tile.type}' in JSON at position {tile.position}. Defaulting to Air.");
                tile.tileTypeEnum = TileType.Air;
            }
            
            boardData[tile.position.x, tile.position.y] = tile;

            // While we're iterating, find the player's start position.
            if (tile.tileTypeEnum == TileType.Start)
            {
                startPosition = tile.position;
                startFound = true;
            }
        }

        // --- 3. Generate the Visual Board from Logical Data ---
        ClearBoardVisuals();
        for (int x = 0; x < loadedLevel.width; x++)
        {
            for (int y = 0; y < loadedLevel.height; y++)
            {
                TileData data = boardData[x, y];
                if (data == null) continue; // Skip empty spots in the grid

                Tilemap targetMap = GetTilemapForType(data.tileTypeEnum);
                
                // Use the safer TryGetValue to avoid errors if a mapping is missing.
                if (tileAssetDictionary.TryGetValue(data.tileTypeEnum, out TileBase tileAsset))
                {
                     if (targetMap != null && tileAsset != null)
                     {
                        // The Tilemap system uses Vector3Int for coordinates.
                        targetMap.SetTile(new Vector3Int(x, y, 0), tileAsset);
                     }
                }
            }
        }
        
        // --- 4. Spawn the Player ---
        if (startFound)
        {
            SpawnPlayer(startPosition);
        }
        else
        {
            Debug.LogError("No 'Start' tile found in level data! Cannot spawn player.");
        }
    }

    /// <summary>
    /// Instantiates the player prefab at a given grid position and initializes it.
    /// </summary>
    private void SpawnPlayer(Vector2Int gridPosition)
    {
        if (playerPrefab == null)
        {
            Debug.LogError("Player Prefab is not assigned in the BoardManager!");
            return;
        }
        
        Vector3 worldPos = GridToWorldPosition(gridPosition);
        GameObject playerInstance = Instantiate(playerPrefab, worldPos, Quaternion.identity);
        playerInstance.name = "Player"; // Clean up the hierarchy
        PlayerController playerController = playerInstance.GetComponent<PlayerController>();

        // This is a crucial step: we give the player a reference to this BoardManager
        // and tell it where it starts.
        playerController.Initialize(this, gridPosition);
    }
    
    // --- PUBLIC API METHODS ---
    // These methods are designed to be called by other scripts (like PlayerController).

    /// <summary>
    /// Converts a logical grid coordinate (e.g., [2, 3]) to a world space position.
    /// </summary>
    public Vector3 GridToWorldPosition(Vector2Int gridPosition)
    {
        return baseLayer.GetCellCenterWorld(new Vector3Int(gridPosition.x, gridPosition.y, 0));
    }
    
    /// <summary>
    /// Retrieves the logical tile data for a specific grid coordinate.
    /// Returns null if the coordinate is out of bounds.
    /// </summary>
    public TileData GetTileAtPosition(Vector2Int position)
    {
        // Bounds checking to prevent errors from requests outside the grid.
        if (position.x >= 0 && position.x < boardData.GetLength(0) &&
            position.y >= 0 && position.y < boardData.GetLength(1))
        {
            return boardData[position.x, position.y];
        }
        return null; // The requested position is outside the board.
    }

    // --- PRIVATE HELPER METHODS ---

    /// <summary>
    /// Determines which Tilemap a tile should be drawn on based on its type.
    /// </summary>
    private Tilemap GetTilemapForType(TileType type)
    {
        switch (type)
        {
            case TileType.Floor:
            case TileType.Wall:
            case TileType.Air:
            case TileType.Start:
            case TileType.End:
                return baseLayer;

            case TileType.Switch:
            case TileType.Bridge:
                return interactableLayer;
                
            default:
                Debug.LogWarning("No tilemap found for type: " + type);
                return null;
        }
    }

    /// <summary>
    /// Clears all tiles from the visual tilemaps before drawing a new level.
    /// </summary>
    private void ClearBoardVisuals()
    {
        baseLayer.ClearAllTiles();
        interactableLayer.ClearAllTiles();
    }
}