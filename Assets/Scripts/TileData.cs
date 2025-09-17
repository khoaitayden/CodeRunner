using UnityEngine;

// An enum to define all possible tile types.
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
    [System.NonSerialized]
    public TileType tileTypeEnum;

    public Vector2Int position;

    // --- Special data for interactable tiles ---
    public int switchId = 0; // For a Switch, this is its unique ID.
    public int controlledBySwitchId = 0; // For a Bridge, this is the ID of the Switch that controls it.
    public bool isBridgeInitiallyActive = true;

    // Determines if a bridge is active when its switch is ON (true) or OFF (false).
    public bool activateOnSwitchOn = true;

    // --- Runtime state variables ---
    // These are ignored by the JSON serializer and are managed by the BoardManager at runtime.
    [System.NonSerialized] public bool isOn = false;     // For Switches: current on/off state.
    [System.NonSerialized] public bool isActive = false; // For Bridges: current active/inactive state.
    
}