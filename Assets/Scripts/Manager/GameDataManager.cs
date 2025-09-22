using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;
public class GameDataManager : MonoBehaviour
{
    // --- Singleton Pattern ---
    public static GameDataManager Instance { get; private set; }

    // --- Public Data ---
    public SaveData currentSessionData; // The data for this specific playthrough

    // --- Private ---
    private SaveFile allSaves; // Holds the list of ALL sessions
    private string saveFilePath;

    void Awake()
    {
        // Implement the Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // --- Use your path logic to set a SINGLE file path ---
        InitializeSavePath();

        // Load all previous sessions from the file
        LoadAllSessions();

    }

    // --- THIS IS YOUR PROVIDED METHOD, MODIFIED TO POINT TO ONE FILE ---
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

        Debug.Log($"Save data will be stored at: {saveFilePath}");
    }

    private void LoadAllSessions()
    {
        if (File.Exists(saveFilePath))
        {
            string json = File.ReadAllText(saveFilePath);
            allSaves = JsonUtility.FromJson<SaveFile>(json);
            Debug.Log($"Loaded {allSaves.allSessions.Count} previous game sessions.");
        }
        else
        {
            // If no save file exists, create a new one
            allSaves = new SaveFile();
            Debug.Log("No save file found. Creating new save file container.");
        }
    }

    public void BeginNewSession()
    {
        // Create a new SaveData object for this playthrough
        currentSessionData = new SaveData
        {
            playerName = "Player",
            levelsPassed = 0,
            totalSteps = 0
        };
        
        // Add this new session to our master list
        allSaves.allSessions.Add(currentSessionData);
        Debug.Log("--- New Game Session Started and added to the list ---");
    }
    // This single method saves the ENTIRE list back to the file
    public void SaveGame()
    {
        string json = JsonUtility.ToJson(allSaves, true);
        File.WriteAllText(saveFilePath, json);
        Debug.Log("All game sessions saved.");
    }

    // --- Gameplay Integration Methods ---
    // These methods now modify the 'currentSessionData' and then trigger a save of the whole file.

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

    // This method is now much simpler!
    public List<SaveData> GetAllSaveData()
    {
        return allSaves.allSessions;
    }
    
    public bool PlayerNameExists(string nameToCheck)
    {
        if (allSaves == null || allSaves.allSessions == null) return false;

        // .Any() is a powerful LINQ method that checks if any element in a list meets a condition.
        // StringComparison.OrdinalIgnoreCase makes the check case-insensitive ("Player" == "player").
        return allSaves.allSessions.Any(session => session.playerName.Equals(nameToCheck, System.StringComparison.OrdinalIgnoreCase));
    }
}