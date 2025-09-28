using System.Collections.Generic;

[System.Serializable]
public class SaveData
{
    public string playerName;
    public int levelsPassed;
    public int totalSteps;
}

[System.Serializable]
public class SaveFile
{
    public List<SaveData> allSessions;

    public SaveFile()
    {
        allSessions = new List<SaveData>();
    }
}