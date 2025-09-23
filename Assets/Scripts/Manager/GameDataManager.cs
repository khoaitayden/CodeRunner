using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;
public class GameDataManager : MonoBehaviour
{

    public static GameDataManager Instance { get; private set; }

    public SaveData currentSessionData; 


    private SaveFile allSaves; 
    private string saveFilePath;

    void Awake()
    {

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeSavePath();

        LoadAllSessions();

    }

    private void InitializeSavePath()
    {
        string fileName = "AllPlayerScores.json";
#if UNITY_EDITOR
        string assetsPath = Application.dataPath;
        string saveFolder = Path.Combine(assetsPath, "Data", "PlayerSaves");
        Directory.CreateDirectory(saveFolder);
        saveFilePath = Path.Combine(saveFolder, fileName);
#else
            saveFilePath = Path.Combine(Application.persistentDataPath, fileName);
#endif

    }

    private void LoadAllSessions()
    {
        if (File.Exists(saveFilePath))
        {
            string json = File.ReadAllText(saveFilePath);
            allSaves = JsonUtility.FromJson<SaveFile>(json);
        }
        else
        {
            allSaves = new SaveFile();
        }
    }

    public void BeginNewSession()
    {
        currentSessionData = new SaveData
        {
            playerName = "Player",
            levelsPassed = 0,
            totalSteps = 0
        };
        
        allSaves.allSessions.Add(currentSessionData);
    }
    // This single method saves the ENTIRE list back to the file
    public void SaveGame()
    {
        string json = JsonUtility.ToJson(allSaves, true);
        File.WriteAllText(saveFilePath, json);
    }


    public void SetPlayerName(string newName)
    {
        currentSessionData.playerName = newName;
        SaveGame();
    }



    public void UpdateProgress(int newLevelPassed, int stepsThisLevel)
    {
        if (newLevelPassed > currentSessionData.levelsPassed)
        {
            currentSessionData.levelsPassed = newLevelPassed;
        }
        currentSessionData.totalSteps += stepsThisLevel;
        SaveGame();
    }

    public List<SaveData> GetAllSaveData()
    {
        return allSaves.allSessions;
    }
    
    public bool PlayerNameExists(string nameToCheck)
    {
        if (allSaves == null || allSaves.allSessions == null) return false;

        return allSaves.allSessions.Any(session => session.playerName.Equals(nameToCheck, System.StringComparison.OrdinalIgnoreCase));
    }
}