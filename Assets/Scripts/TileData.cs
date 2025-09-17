using UnityEngine;

public enum TileType
{
    Floor,
    Wall,
    Air,
    Start,
    End,
    Switch,
    Bridge
}

[System.Serializable]
public class TileData
{
    // This will hold the string from the JSON (e.g., "Wall")
    public string type; 

    // This will hold the actual enum value after we parse the string.
    // [System.NonSerialized] tells Unity's JSON utility to ignore this field.
    [System.NonSerialized]
    public TileType tileTypeEnum; 

    public Vector2Int position;

    // --- Special data for interactable tiles ---
    public int switchId = 0;
    public int controlledBySwitchId = 0;
    public bool isBridgeInitiallyActive = true;
}