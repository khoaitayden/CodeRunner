using System.Collections.Generic;

[System.Serializable]
public class LevelData
{
    public int width;
    public int height;
    public List<TileData> tiles;
    public Direction startDirection = Direction.Up; // <-- ADD THIS LINE, with a default value

    public LevelData()
    {
        tiles = new List<TileData>();
    }
}