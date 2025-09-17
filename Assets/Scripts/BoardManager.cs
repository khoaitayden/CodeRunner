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

public class BoardManager : MonoBehaviour
{
    [Header("Level Data")]
    public TextAsset levelJson;

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
        LoadAndGenerateBoard();
    }

    public void LoadAndGenerateBoard()
    {
        if (levelJson == null)
        {
            Debug.LogError("No level JSON file assigned!");
            return;
        }

        LevelData loadedLevel = JsonUtility.FromJson<LevelData>(levelJson.text);
        boardData = new TileData[loadedLevel.width, loadedLevel.height];
        Vector2Int startPosition = Vector2Int.zero;
        bool startFound = false;

        // Pass 1: Initialize all tiles and their default states
        foreach (var tile in loadedLevel.tiles)
        {
            if (System.Enum.TryParse(tile.type, true, out TileType parsedType))
            {
                tile.tileTypeEnum = parsedType;
            }
            else
            {
                Debug.LogWarning($"Unknown tile type '{tile.type}' in JSON. Defaulting to Air.");
                tile.tileTypeEnum = TileType.Air;
            }

            boardData[tile.position.x, tile.position.y] = tile;

            if (tile.tileTypeEnum == TileType.Bridge) tile.isActive = tile.isBridgeInitiallyActive;
            if (tile.tileTypeEnum == TileType.Switch) tile.isOn = false; // Switches always start OFF
            if (tile.tileTypeEnum == TileType.Start)
            {
                startPosition = tile.position;
                startFound = true;
            }
        }

        // Pass 2: Synchronize all bridges with their controlling switches' initial states
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

        if (startFound) { SpawnPlayer(startPosition); }
        else { Debug.LogError("No 'Start' tile found in level data!"); }
    }

    // --- Public Interaction Methods ---

    public void PlayerLandedOnTile(Vector2Int position)
    {
        TileData tile = GetTileAtPosition(position);
        if (tile != null && tile.tileTypeEnum == TileType.Switch)
        {
            ToggleSwitch(tile);
        }
    }

    public bool IsTileWalkable(Vector2Int position)
    {
        TileData tile = GetTileAtPosition(position);
        if (tile == null) return false; // Out of bounds

        switch (tile.tileTypeEnum)
        {
            case TileType.Wall:
            case TileType.Air:
                return false;
            case TileType.Bridge:
                return tile.isActive; // Only walkable if active
            default:
                return true; // Floor, Start, End, Switch are all walkable
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
        playerInstance.GetComponent<PlayerController>().Initialize(this, gridPosition);
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
            case TileType.Floor: case TileType.Wall: case TileType.Air: case TileType.Start: case TileType.End:
                return baseLayer;
            case TileType.Switch: case TileType.Bridge:
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
}