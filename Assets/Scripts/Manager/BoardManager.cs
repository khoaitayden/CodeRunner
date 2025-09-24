using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
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
    [SerializeField] private List<TextAsset> levelFiles;
    private int currentLevelIndex = 0;


    [Header("Object Prefabs")]
    [SerializeField] private GameObject playerPrefab;

    [Header("Tilemap References")]
    [SerializeField] private Tilemap baseLayer;
    [SerializeField] private Tilemap interactableLayer;

    [Header("Tile Assets (Simple)")]
    [SerializeField] private List<TileMapping> tileMappings;

    [Header("Tile Assets (Stateful)")]
    [SerializeField] private TileBase switchOnTile;
    [SerializeField] private TileBase switchOffTile;
    [SerializeField] private TileBase bridgeActiveTile;
    [SerializeField] private TileBase bridgeInactiveTile;
    [SerializeField] private List<TileBase> weakFloorTiles;

    [Header("Camera Control")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private RectTransform gameViewPanel;
    [SerializeField] private float boardPadding = 1.1f;
    [Header("System References")]
    [SerializeField] private GameplayUIManager gameplayUIManager;
    [Header("System References")]

    // Public property to access the current player instance safely
    public PlayerController PlayerInstance { get; private set; }

    private TileData[,] boardData;
    private Dictionary<TileType, TileBase> tileAssetDictionary;

    void Awake()
    {
        // Convert the list of mappings into a dictionary for fast lookups.
        tileAssetDictionary = new Dictionary<TileType, TileBase>();
        foreach (var mapping in tileMappings)
        {
            tileAssetDictionary[mapping.type] = mapping.tileAsset;
        }       
        LoadLevel(0);
    }
    
    public void LoadLevel(int levelIndex)
    {
        Debug.Log($"Loading Level {levelIndex}...");
        gameplayUIManager.UpdateCurrentLevelText(levelIndex + 1);
        if (levelIndex != currentLevelIndex)
        {
            if (gameplayUIManager != null)
            {
                gameplayUIManager.ClearAllCommandSlots();
            }
        }
        // --- 1. Cleanup previous level ---
        if (PlayerInstance != null)
        {
            PlayerInstance.HaltExecution();
            Destroy(PlayerInstance.gameObject);
        }
        ClearBoardVisuals();

        // --- 2. Set up for the new level ---
        currentLevelIndex = levelIndex;

        TextAsset levelAsset = levelFiles[currentLevelIndex];
        if (levelAsset == null)
        {
            Debug.LogError($"Level file at index {currentLevelIndex} is not assigned!");
            return;
        }

        // --- 3. Build the new level ---
        BuildBoardFromData(levelAsset);
    }

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
            tile.tileTypeEnum = (TileType)System.Enum.Parse(typeof(TileType), tile.type, true);

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

        DrawEntireBoard();

        if (startFound)
        {
            SpawnPlayer(startPosition, loadedLevel.startDirection); 
        }
        else
        {
            Debug.LogError("No 'Start' tile found!");
        }
        Debug.Log("Level loaded successfully.");
        FinalizeLevelSetup();
    }

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
                        Debug.Log("Weak Floor broke!");
                        tile.tileTypeEnum = TileType.Air;
                        UpdateTileVisual(tile);
                        RestartLevel();
                        return;
                    }
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

        if (tile == null)
        {
            return MoveResult.Fall;
        }

        switch (tile.tileTypeEnum)
        {
            case TileType.Wall:
                return MoveResult.Blocked;
            case TileType.Air:
                return MoveResult.Fall;
            case TileType.WeakFloor:
                return MoveResult.Success;
            case TileType.Bridge:
                return tile.isActive ? MoveResult.Success : MoveResult.Fall;
            default:
                return MoveResult.Success;
        }
    }

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

    private void SpawnPlayer(Vector2Int gridPosition, Direction startDirection)
    {
        if (playerPrefab == null) { Debug.LogError("Player Prefab is not assigned!"); return; }
        Vector3 worldPos = GridToWorldPosition(gridPosition);
        GameObject playerInstance = Instantiate(playerPrefab, worldPos, Quaternion.identity);
        PlayerInstance = playerInstance.GetComponent<PlayerController>();
        PlayerInstance.Initialize(this, gridPosition, startDirection);
        PlayerLandedOnTile(gridPosition);
        if (gameplayUIManager != null)
        {
            gameplayUIManager.ConnectToPlayer(PlayerInstance);
        }
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
        baseLayer.SetTile(tilePosition, null);
        interactableLayer.SetTile(tilePosition, null);

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
                if (weakFloorTiles == null || weakFloorTiles.Count == 0) { Debug.LogError("WeakFloorTiles list not set up!"); break; }
                int spriteIndex = Mathf.Clamp(tile.stepsRemaining, 0, weakFloorTiles.Count - 1);
                tileAsset = weakFloorTiles[spriteIndex];
                break;
            default:
                tileAssetDictionary.TryGetValue(tile.tileTypeEnum, out tileAsset);
                break;
        }

        if (targetMap != null)
        {
            targetMap.SetTile(tilePosition, tileAsset);
        }
    }

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
            case TileType.Floor: case TileType.Wall: case TileType.Air: case TileType.Start: case TileType.End:
                return baseLayer;
            case TileType.Switch: case TileType.Bridge: case TileType.WeakFloor:
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
        if (GameDataManager.Instance != null && PlayerInstance != null)
        {
            int levelNumberPassed = currentLevelIndex + 1;
            int stepsTaken = PlayerInstance.moveCount;

            GameDataManager.Instance.UpdateProgress(levelNumberPassed, stepsTaken);

            int nextLevelIndex = currentLevelIndex + 1;

            if (nextLevelIndex >= levelFiles.Count)
            {
                // Player has finished the last level.
                Debug.Log("CONGRATULATIONS! You've completed all levels!");

                // 1. Set the flag to show the high scores.
                MainMenuController.ShowHighScoresOnLoad = true;

                // 2. Transition directly to the main menu.
                TransitionManager.Instance.TransitionToScene("MainMenuScene");
            }
            else
            {
                // There are more levels to play. Transition to the next one.
                TransitionManager.Instance.PlayTransition(() => LoadLevel(nextLevelIndex));
            }
        }
        TransitionManager.Instance.PlayTransition(() => LoadLevel(currentLevelIndex + 1));
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
        if (!string.IsNullOrEmpty(compactData.startDirection))
        {
            // Try to parse the string into our Direction enum. The 'true' makes it case-insensitive.
            if (System.Enum.TryParse(compactData.startDirection, true, out Direction parsedDirection))
            {
                levelData.startDirection = parsedDirection;
            }
            else
            {
                Debug.LogWarning($"Unknown startDirection '{compactData.startDirection}' in JSON. Defaulting to Up.");
                levelData.startDirection = Direction.Up; // Default fallback
            }
        }
        if (compactData.layout == null || compactData.layout.Count == 0)
        {
            levelData.height = 0; levelData.width = 0; return levelData;
        }

        levelData.height = compactData.layout.Count;
        int maxGridWidth = 0;

        for (int y = 0; y < levelData.height; y++)
        {
            string row = compactData.layout[levelData.height - 1 - y];
            int gridX = 0;
            int stringX = 0;

            while (stringX < row.Length)
            {
                string matchedKey = null;
                foreach (var key in definitions.Keys)
                {
                    if (row.Substring(stringX).StartsWith(key))
                    {
                        if (matchedKey == null || key.Length > matchedKey.Length)
                        {
                            matchedKey = key;
                        }
                    }
                }

                if (matchedKey != null)
                {
                    var def = definitions[matchedKey];
                    levelData.tiles.Add(new TileData {
                        position = new Vector2Int(gridX, y), type = def.type,
                        switchId = def.switchId, controlledBySwitchId = def.controlledBySwitchId,
                        isBridgeInitiallyActive = def.isBridgeInitiallyActive,
                        activateOnSwitchOn = def.activateOnSwitchOn, initialSteps = def.initialSteps
                    });
                    gridX++;
                    stringX += matchedKey.Length;
                }
                else
                {
                    char symbol = row[stringX];
                    string tileType = null;
                    switch (symbol)
                    {
                        case '.': tileType = "Floor"; break; case '#': tileType = "Wall"; break;
                        case 'S': tileType = "Start"; break; case 'E': tileType = "End"; break;
                        case ' ': tileType = "Air"; break;
                    }

                    if (tileType != null)
                    {
                        if (tileType != "Air")
                        {
                            levelData.tiles.Add(new TileData { position = new Vector2Int(gridX, y), type = tileType });
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Unrecognized symbol '{symbol}' at string index {stringX} for grid pos ({gridX},{y}).");
                    }
                    gridX++;
                    stringX++;
                }

                if (gridX > maxGridWidth)
                {
                    maxGridWidth = gridX;
                }
            }
        }
        levelData.width = maxGridWidth;
        return levelData;
    }

    private void CenterCameraOnBoard()
    {
        if (mainCamera == null || gameViewPanel == null)
        {
            Debug.LogError("Main Camera or Game View Panel reference is not set in BoardManager!");
            return;
        }
        Debug.Log("Centering camera on board...");
        float boardWidth = boardData.GetLength(0);
        float boardHeight = boardData.GetLength(1);
        float panelWidthPixels = gameViewPanel.rect.width;
        float panelHeightPixels = gameViewPanel.rect.height;

        if (panelWidthPixels <= 0 || panelHeightPixels <= 0) return;

        float boardAspect = boardWidth / boardHeight;
        float panelAspect = panelWidthPixels / panelHeightPixels;

        if (boardAspect > panelAspect)
        {
            mainCamera.orthographicSize = (boardWidth / panelAspect / 2f) * boardPadding;
        }
        else
        {
            mainCamera.orthographicSize = (boardHeight / 2f) * boardPadding;
        }

        // --- THE ONLY CORRECTION IS HERE ---
        // This -0.5f offset centers the camera on the middle of the tiles, not the grid lines.
        Vector3 boardCenter = new Vector3(boardWidth / 2f, boardHeight / 2f , -10);

        Vector3 panelCenterScreen = gameViewPanel.position;
        Vector3 screenCenterScreen = new Vector3(Screen.width / 2f, Screen.height / 2f, 0);
        Vector3 screenOffset = panelCenterScreen - screenCenterScreen;

        float worldUnitsPerPixel = (mainCamera.orthographicSize * 2) / Screen.height;
        Vector3 worldOffset = new Vector3(screenOffset.x * worldUnitsPerPixel, screenOffset.y * worldUnitsPerPixel, 0);

        mainCamera.transform.position = boardCenter - worldOffset;
    }
    
    private void FinalizeLevelSetup()
    {
        
        Debug.Log("Finalizing level setup and centering camera...");
        CenterCameraOnBoard();
    }
}