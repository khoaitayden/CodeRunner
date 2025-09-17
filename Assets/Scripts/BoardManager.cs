using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

// Helper class to link a TileType enum to a TileBase asset in the Inspector for simple tiles.
[System.Serializable]
public class TileMapping
{
    public TileType type;
    public TileBase tileAsset;
}

public enum MoveResult
{
    Success, // The tile is safe to walk on
    Blocked, // A wall, player cannot move
    Fall     // An edge, air, or inactive bridge; triggers a reset
}

public class BoardManager : MonoBehaviour
{
    [Header("Level Data")]
    public List<TextAsset> levelFiles; // ...with this list.
    private int currentLevelIndex = 0;

    [Header("Object Prefabs")]
    public GameObject playerPrefab;

    [Header("Tilemap References")]
    public Tilemap baseLayer;
    public Tilemap interactableLayer;

    [Header("Tile Assets (Simple)")]
    public List<TileMapping> tileMappings; // For Floor, Wall, Start, End, Air

    [Header("Tile Assets (Stateful)")]
    public TileBase switchOnTile;
    public TileBase switchOffTile;
    public TileBase bridgeActiveTile;   // The walkable bridge
    public TileBase bridgeInactiveTile; // The non-walkable bridge

    // The internal "brain" of the board.

    private TileData[,] boardData;
    private Dictionary<TileType, TileBase> tileAssetDictionary;
    private PlayerController playerControllerInstance;

    void Awake()
    {
        // Convert the list of mappings into a dictionary for fast lookups.
        tileAssetDictionary = new Dictionary<TileType, TileBase>();
        foreach (var mapping in tileMappings)
        {
            tileAssetDictionary[mapping.type] = mapping.tileAsset;
        }
    }

    void Start()
    {
        // Start the game by loading the first level
        LoadLevel(0);
    }

    // This is our new public "controller" method.
    public void LoadLevel(int levelIndex)
    {
        // --- 1. Cleanup previous level ---
        if (playerControllerInstance != null)
        {
            Destroy(playerControllerInstance.gameObject);
        }
        ClearBoardVisuals();

        // --- 2. Set up for the new level ---
        currentLevelIndex = levelIndex;
        if (currentLevelIndex >= levelFiles.Count)
        {
            Debug.Log("CONGRATULATIONS! You've completed all levels!");
            // Here you could show a victory screen, etc.
            return;
        }

        TextAsset levelAsset = levelFiles[currentLevelIndex];
        if (levelAsset == null)
        {
            Debug.LogError($"Level file at index {currentLevelIndex} is not assigned!");
            return;
        }

        // --- 3. Build the new level ---
        BuildBoardFromData(levelAsset);
    }

    // This method now contains the core logic that was in LoadAndGenerateBoard()
    private void BuildBoardFromData(TextAsset levelAsset)
    {
        CompactLevelData compactLevel = JsonUtility.FromJson<CompactLevelData>(levelAsset.text);
        LevelData loadedLevel = ParseCompactLevelData(compactLevel);
        boardData = new TileData[loadedLevel.width, loadedLevel.height];

        Vector2Int startPosition = Vector2Int.zero;
        bool startFound = false;

        // Pass 1: Initialize tiles
        foreach (var tile in loadedLevel.tiles)
        {
            // First, try to parse the tile type string into an enum
            if (!System.Enum.TryParse(tile.type, true, out TileType parsedType))
            {
                // If parsing fails, log a warning and skip this tile.
                Debug.LogWarning($"Unknown tile type '{tile.type}' in JSON at position {tile.position}. Skipping tile.");
                continue; // Go to the next tile in the loop
            }

            // If we get here, parsing was successful. Assign the enum.
            tile.tileTypeEnum = parsedType;

            // Now, perform all the logic checks using the confirmed tile.tileTypeEnum

            // Check if this is the start tile
            if (tile.tileTypeEnum == TileType.Start)
            {
                startPosition = tile.position;
                startFound = true;
                // Let's add a log to be 100% sure it's working
                Debug.Log($"Start tile found at {startPosition}!");
            }

            // Set initial state for bridges
            if (tile.tileTypeEnum == TileType.Bridge)
            {
                tile.isActive = tile.isBridgeInitiallyActive;
            }

            // Set initial state for switches
            if (tile.tileTypeEnum == TileType.Switch)
            {
                tile.isOn = false;
            }

            // Finally, place the processed tile data into our 2D grid array
            boardData[tile.position.x, tile.position.y] = tile;
        }

        // Pass 2: Synchronize bridges
        for (int x = 0; x < loadedLevel.width; x++)
        {
            for (int y = 0; y < loadedLevel.height; y++)
            {
                if (boardData[x, y] != null && boardData[x, y].tileTypeEnum == TileType.Switch)
                {
                    UpdateBridgesForSwitch(boardData[x, y]);
                }
            }
        }

        // Generate Visuals & Spawn Player
        DrawEntireBoard();

        if (startFound) { SpawnPlayer(startPosition); }
        else { Debug.LogError("No 'Start' tile found!"); }
    }

    // --- Public Interaction Methods ---

    public void PlayerLandedOnTile(Vector2Int position)
    {
        TileData tile = GetTileAtPosition(position);
        if (tile == null) return;

        switch (tile.tileTypeEnum)
        {
            case TileType.Switch:
                ToggleSwitch(tile);
                break;

            // The Air case is no longer needed here!

            case TileType.End:
                Debug.Log("Player reached the end! Loading next level.");
                LoadNextLevel();
                break;
        }
    }


    public MoveResult CheckMove(Vector2Int position)
    {
        TileData tile = GetTileAtPosition(position);

        // Case 1: Out of bounds (off the edge)
        if (tile == null)
        {
            return MoveResult.Fall;
        }

        // Case 2: Check the tile type
        switch (tile.tileTypeEnum)
        {
            case TileType.Wall:
                return MoveResult.Blocked;

            case TileType.Air:
                return MoveResult.Fall;

            case TileType.Bridge:
                // A bridge is only a success if it's active, otherwise it's a fall
                return tile.isActive ? MoveResult.Success : MoveResult.Fall;

            // Default case covers Floor, Start, End, Switch
            default:
                return MoveResult.Success;
        }
    }

    // --- Private State Management ---

    private void ToggleSwitch(TileData switchTile)
    {
        switchTile.isOn = !switchTile.isOn;
        Debug.Log($"Switch {switchTile.switchId} at {switchTile.position} turned {(switchTile.isOn ? "ON" : "OFF")}.");
        UpdateTileVisual(switchTile);
        UpdateBridgesForSwitch(switchTile);
    }

    private void UpdateBridgesForSwitch(TileData switchTile)
    {
        for (int x = 0; x < boardData.GetLength(0); x++)
        {
            for (int y = 0; y < boardData.GetLength(1); y++)
            {
                TileData potentialBridge = boardData[x, y];
                if (potentialBridge != null && potentialBridge.tileTypeEnum == TileType.Bridge && potentialBridge.controlledBySwitchId == switchTile.switchId)
                {
                    potentialBridge.isActive = (switchTile.isOn == potentialBridge.activateOnSwitchOn);
                    UpdateTileVisual(potentialBridge);
                }
            }
        }
    }

    // --- Visual and Spawning Methods ---

    private void SpawnPlayer(Vector2Int gridPosition)
    {
        if (playerPrefab == null) { Debug.LogError("Player Prefab is not assigned!"); return; }
        Vector3 worldPos = GridToWorldPosition(gridPosition);
        GameObject playerInstance = Instantiate(playerPrefab, worldPos, Quaternion.identity);
        playerControllerInstance = playerInstance.GetComponent<PlayerController>();
        playerControllerInstance.Initialize(this, gridPosition);
        PlayerLandedOnTile(gridPosition);
    }

    private void DrawEntireBoard()
    {
        ClearBoardVisuals();
        for (int x = 0; x < boardData.GetLength(0); x++)
        {
            for (int y = 0; y < boardData.GetLength(1); y++)
            {
                if (boardData[x, y] != null)
                {
                    UpdateTileVisual(boardData[x, y]);
                }
            }
        }
    }

    private void UpdateTileVisual(TileData tile)
    {
        Tilemap targetMap = GetTilemapForType(tile.tileTypeEnum);
        TileBase tileAsset = null;

        switch (tile.tileTypeEnum)
        {
            case TileType.Switch:
                tileAsset = tile.isOn ? switchOnTile : switchOffTile;
                break;
            case TileType.Bridge:
                tileAsset = tile.isActive ? bridgeActiveTile : bridgeInactiveTile;
                break;
            default:
                tileAssetDictionary.TryGetValue(tile.tileTypeEnum, out tileAsset);
                break;
        }

        if (targetMap != null)
        {
            targetMap.SetTile(new Vector3Int(tile.position.x, tile.position.y, 0), tileAsset);
        }
    }

    // --- Utility and Helper Methods ---

    public TileData GetTileAtPosition(Vector2Int position)
    {
        if (position.x >= 0 && position.x < boardData.GetLength(0) && position.y >= 0 && position.y < boardData.GetLength(1))
        {
            return boardData[position.x, position.y];
        }
        return null;
    }

    public Vector3 GridToWorldPosition(Vector2Int gridPosition)
    {
        return baseLayer.GetCellCenterWorld(new Vector3Int(gridPosition.x, gridPosition.y, 0));
    }

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
                return null;
        }
    }

    private void ClearBoardVisuals()
    {
        baseLayer.ClearAllTiles();
        interactableLayer.ClearAllTiles();
    }

    public void RestartLevel()
    {
        Debug.Log("Restarting current level...");
        LoadLevel(currentLevelIndex);
    }

    public void LoadNextLevel()
    {
        Debug.Log("Loading next level...");
        LoadLevel(currentLevelIndex + 1);
    }
    
    private LevelData ParseCompactLevelData(CompactLevelData compactData)
    {
        var definitions = new Dictionary<string, TileDefinition>();
        if (compactData.definitions != null)
        {
            foreach (var def in compactData.definitions)
            {
                definitions[def.key] = def;
            }
        }

        var levelData = new LevelData();
        if (compactData.layout == null || compactData.layout.Count == 0)
        {
            levelData.height = 0;
            levelData.width = 0;
            return levelData;
        }

        levelData.height = compactData.layout.Count;
        levelData.width = compactData.layout[0].Length;

        // We read it "backwards" (y=0 is the bottom row) to match Unity's coordinate system.
        for (int y = 0; y < levelData.height; y++)
        {
            string row = compactData.layout[levelData.height - 1 - y];
            for (int x = 0; x < levelData.width; x++)
            {
                // --- NEW, IMPROVED PARSING LOGIC ---

                // 1. Try to match a multi-character key first.
                // We search for the longest possible key starting at this position.
                string matchedKey = null;
                foreach (var key in definitions.Keys)
                {
                    if (row.Substring(x).StartsWith(key))
                    {
                        // If we find a match, check if it's the longest one we've found so far
                        if (matchedKey == null || key.Length > matchedKey.Length)
                        {
                            matchedKey = key;
                        }
                    }
                }

                TileData tile = new TileData { position = new Vector2Int(x, y) };

                if (matchedKey != null)
                {
                    // We found a defined key like "SW1" or "B1"
                    var def = definitions[matchedKey];
                    tile.type = def.type;
                    tile.switchId = def.switchId;
                    tile.controlledBySwitchId = def.controlledBySwitchId;
                    tile.isBridgeInitiallyActive = def.isBridgeInitiallyActive;
                    tile.activateOnSwitchOn = def.activateOnSwitchOn;

                    levelData.tiles.Add(tile);

                    // IMPORTANT: Skip the characters we just read as part of the key
                    x += matchedKey.Length - 1;
                }
                else
                {
                    // 2. If no multi-character key was found, fall back to single-character symbols.
                    char symbol = row[x];
                    bool isSymbolRecognized = true;

                    switch (symbol)
                    {
                        case '.': tile.type = "Floor"; break;
                        case '#': tile.type = "Wall"; break;
                        case 'S': tile.type = "Start"; break;
                        case 'E': tile.type = "End"; break;
                        case ' ': tile.type = "Air"; break;
                        default:
                            // Unrecognized single symbol
                            isSymbolRecognized = false;
                            Debug.LogWarning($"Unrecognized symbol '{symbol}' at ({x},{y}). Treating as Air.");
                            break;
                    }
                    
                    // Only add a tile if it's not air from an unrecognized symbol
                    if (isSymbolRecognized && tile.type != "Air")
                    {
                        levelData.tiles.Add(tile);
                    }
                }
            }
        }
        return levelData;
    }
}