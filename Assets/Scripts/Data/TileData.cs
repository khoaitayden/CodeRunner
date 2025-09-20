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
    Bridge,
    WeakFloor 
}

[System.Serializable]
public class TileData
{
    public string type;

    [System.NonSerialized]
    public TileType tileTypeEnum;

    public Vector2Int position;

    public int switchId = 0; 
    public int controlledBySwitchId = 0; 
    public bool isBridgeInitiallyActive = true;

    public bool activateOnSwitchOn = true;
    public int initialSteps = 0;

    [System.NonSerialized] 
    public int stepsRemaining = 0;

    [System.NonSerialized] public bool isOn = false;    
    [System.NonSerialized] public bool isActive = false; 
    
}