using System.Collections.Generic;

[System.Serializable]
public class LevelData
{
    public int width;
    public int height;
    
    // We use a 1D list because it's easier to serialize to JSON than a 2D array.
    // We'll convert it to a 2D array in our game logic.
    public List<TileData> tiles;

    public LevelData()
    {
        tiles = new List<TileData>();
    }
}