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
    public List<TileBase> weakFloorTiles; 

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
            // The parser now guarantees the 'type' string is valid.
            tile.tileTypeEnum = (TileType)System.Enum.Parse(typeof(TileType), tile.type, true);

            // Set initial state for all tile types that need it
            switch (tile.tileTypeEnum)
            {
                case TileType.Start:
                    startPosition = tile.position;
                    startFound = true;
                    break;
                case TileType.Bridge:
                    tile.isActive = tile.isBridgeInitiallyActive;
                    break;
                case TileType.Switch:
                    tile.isOn = false;
                    break;
                case TileType.WeakFloor:
                    tile.stepsRemaining = tile.initialSteps;
                    break;
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

        case TileType.WeakFloor:
            if (tile.stepsRemaining > 0)
            {
                tile.stepsRemaining--;
                Debug.Log($"Stepped on Weak Floor at {tile.position}. Steps remaining: {tile.stepsRemaining}");

                if (tile.stepsRemaining == 0)
                {
                    // The floor breaks!
                    Debug.Log("Weak Floor broke!");
                    tile.tileTypeEnum = TileType.Air;
                    UpdateTileVisual(tile); // Update the visual to show the hole

                    // --- CRITICAL FIX ---
                    // The player is now on an Air tile, so they must fall.
                    RestartLevel(); 
                    return; // Use return to exit the function immediately after a restart.
                }
                
                // If it didn't break, just update the visual.
                UpdateTileVisual(tile);
            }
            break;

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
            
            // NEW CASE
            case TileType.WeakFloor:
                // It's always a success to step ONTO a weak floor.
                // The breaking happens *after* you land.
                return MoveResult.Success;

            case TileType.Bridge:
                return tile.isActive ? MoveResult.Success : MoveResult.Fall;

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
        Vector3Int tilePosition = new Vector3Int(tile.position.x, tile.position.y, 0);

        // --- CRITICAL FIX ---
        // First, clear the tile from ALL layers to prevent ghost images.
        baseLayer.SetTile(tilePosition, null);
        interactableLayer.SetTile(tilePosition, null);

        // Now, determine the new tile asset and the correct layer to draw on.
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
            case TileType.WeakFloor:
                if (weakFloorTiles == null || weakFloorTiles.Count == 0) {
                    Debug.LogError("WeakFloorTiles list not set up!");
                    break;
                }
                int spriteIndex = Mathf.Clamp(tile.stepsRemaining, 0, weakFloorTiles.Count - 1);
                tileAsset = weakFloorTiles[spriteIndex];
                break;
            // The Air type is now handled correctly by the default case
            default:
                tileAssetDictionary.TryGetValue(tile.tileTypeEnum, out tileAsset);
                break;
        }

        // Finally, draw the new tile on the correct layer.
        // If tileAsset is null (like for Air), this correctly erases the tile.
        if (targetMap != null)
        {
            targetMap.SetTile(tilePosition, tileAsset);
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
            case TileType.WeakFloor: 
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
        if (compactData.definitions != null) {
            foreach (var def in compactData.definitions) {
                definitions[def.key] = def;
            }
        }

        var levelData = new LevelData();
        if (compactData.layout == null || compactData.layout.Count == 0) {
            levelData.height = 0; levelData.width = 0; return levelData;
        }

        levelData.height = compactData.layout.Count;
        // We will calculate the true grid width dynamically
        int maxGridWidth = 0;

        for (int y = 0; y < levelData.height; y++)
        {
            string row = compactData.layout[levelData.height - 1 - y];
            
            int gridX = 0; // The logical X-position on our game board
            int stringX = 0; // The character index in the layout string

            while (stringX < row.Length)
            {
                string matchedKey = null;
                foreach (var key in definitions.Keys) {
                    if (row.Substring(stringX).StartsWith(key)) {
                        if (matchedKey == null || key.Length > matchedKey.Length) {
                            matchedKey = key;
                        }
                    }
                }

                if (matchedKey != null)
                {
                    // We found a multi-character key like "W3".
                    // It fills ONE grid space.
                    var def = definitions[matchedKey];
                    TileData tile = new TileData { 
                        position = new Vector2Int(gridX, y),
                        type = def.type,
                        // ... copy all other properties from def ...
                        switchId = def.switchId,
                        controlledBySwitchId = def.controlledBySwitchId,
                        isBridgeInitiallyActive = def.isBridgeInitiallyActive,
                        activateOnSwitchOn = def.activateOnSwitchOn,
                        initialSteps = def.initialSteps
                    };
                    levelData.tiles.Add(tile);
                    
                    // Advance the grid counter by ONE.
                    gridX++; 
                    // Advance the string counter by the key's length.
                    stringX += matchedKey.Length; 
                }
                else
                {
                    // We found a single character. It also fills ONE grid space.
                    char symbol = row[stringX];
                    string tileType = null;
                    switch (symbol) {
                        case '.': tileType = "Floor"; break;
                        case '#': tileType = "Wall"; break;
                        case 'S': tileType = "Start"; break;
                        case 'E': tileType = "End"; break;
                        case ' ': tileType = "Air"; break;
                    }

                    if (tileType != null) {
                        if (tileType != "Air") {
                            levelData.tiles.Add(new TileData { 
                                position = new Vector2Int(gridX, y),
                                type = tileType 
                            });
                        }
                    } else {
                        Debug.LogWarning($"Unrecognized symbol '{symbol}' at string index {stringX} for grid pos ({gridX},{y}).");
                    }
                    
                    // Advance BOTH counters by ONE.
                    gridX++;
                    stringX++;
                }
                
                // Keep track of the widest row to set the board dimensions correctly
                if (gridX > maxGridWidth) {
                    maxGridWidth = gridX;
                }
            }
        }
        
        levelData.width = maxGridWidth;
        return levelData;
    }
}