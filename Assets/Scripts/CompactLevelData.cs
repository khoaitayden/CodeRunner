using System.Collections.Generic;

// Represents a single definition (e.g., "SW1" or "B1")
[System.Serializable]
public class TileDefinition
{
    public string key; // e.g., "SW1"
    public string type; // e.g., "Switch"
    public int switchId = 0;
    public int controlledBySwitchId = 0;
    public bool isBridgeInitiallyActive = true;
    public bool activateOnSwitchOn = true;
}

// Represents the entire compact JSON file structure
[System.Serializable]
public class CompactLevelData
{
    public List<string> layout;
    public List<TileDefinition> definitions;
}