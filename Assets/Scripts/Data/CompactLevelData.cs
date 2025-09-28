using System.Collections.Generic;

[System.Serializable]
public class TileDefinition
{
    public string key; 
    public string type; 
    public int switchId = 0;
    public int controlledBySwitchId = 0;
    public bool isBridgeInitiallyActive = true;
    public bool activateOnSwitchOn = true;
    public int initialSteps = 0;
}

[System.Serializable]
public class CompactLevelData
{
    public List<string> layout;
    public List<TileDefinition> definitions;
    public string startDirection; 
}