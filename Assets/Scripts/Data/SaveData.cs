using System.Collections.Generic;

[System.Serializable]
public class SaveData
{
    public string playerName;
    public int levelsPassed;
    public int totalSteps;
}

// --- ADD THIS NEW CONTAINER CLASS ---
// This class will be the top-level object in our JSON file.
[System.Serializable]
public class SaveFile
{
    public List<SaveData> allSessions;

    public SaveFile()
    {
        allSessions = new List<SaveData>();
    }
}