using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class MainMenuController : MonoBehaviour
{
    public static bool ShowHighScoresOnLoad = false;

    [Header("Main Menu Elements")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button highScoreButton;
    [SerializeField] private Button quitButton;

    [Header("High Score Elements")]
    [SerializeField] private GameObject highScorePanel;
    [SerializeField] private Button backButton;
    [SerializeField] private GameObject scoreEntryPrefab;
    [SerializeField] private Transform scoreContentParent;

    [Header("Scene Settings")]
    [SerializeField] private string gameplaySceneName = "GameplayScene";

    [Header("Validation Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color errorColor; // A lighter red

    private Image inputFieldImage;

    void Start()
    {
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.BeginNewSession();
        }
        // Get the background image component from the input field for color changing
        inputFieldImage = playerNameInput.GetComponent<Image>();

        if (GameDataManager.Instance != null)
        {
            playerNameInput.text = GameDataManager.Instance.currentSessionData.playerName;
        }


        if (inputFieldImage == null)
        {
            Debug.LogError("Player Name InputField is missing an Image component!");
        }

        // --- Hook up all button and input field listeners ---
        startGameButton.onClick.AddListener(StartGame);
        highScoreButton.onClick.AddListener(ShowHighScores);
        backButton.onClick.AddListener(ShowMainMenu);
        quitButton.onClick.AddListener(CloseGame);

        // Add a listener that calls ValidateName every time the text changes
        playerNameInput.onValueChanged.AddListener(ValidateName);

        // --- Initial Panel and Data Setup ---
        if (ShowHighScoresOnLoad)
        {
            ShowHighScores();
            ShowHighScoresOnLoad = false; // Reset the flag
        }
        else
        {
            ShowMainMenu();
        }

        // Run initial validation on the default name or loaded name
        ValidateName(playerNameInput.text);
    }


    /// <summary>
    /// Activates the main menu panel and deactivates the high score panel.
    /// </summary>
    private void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        highScorePanel.SetActive(false);
    }

    /// <summary>
    /// Activates the high score panel, deactivates the main menu, and populates the scores.
    /// </summary>
    private void ShowHighScores()
    {
        mainMenuPanel.SetActive(false);
        highScorePanel.SetActive(true);
        PopulateHighScores();
    }


    /// <summary>
    /// Validates the current player name in real-time. Turns the input field red
    /// and disables the start button if the name is empty or already exists.
    /// </summary>
    private void ValidateName(string currentName)
    {
        if (GameDataManager.Instance == null) return;

        // First, check for the most severe error: a duplicate name.
        if (GameDataManager.Instance.PlayerNameExists(currentName))
        {
            // Name is a duplicate: Show error color and disable the button.
            if (inputFieldImage != null) inputFieldImage.color = errorColor;
            startGameButton.interactable = false;
        }
        // Next, check if the name is empty.
        else if (string.IsNullOrWhiteSpace(currentName))
        {
            // Name is empty: Use normal color, but disable the button.
            if (inputFieldImage != null) inputFieldImage.color = normalColor;
            startGameButton.interactable = false;
        }
        // If neither of the above is true, the name is valid.
        else
        {
            // Name is valid: Use normal color and enable the button.
            if (inputFieldImage != null) inputFieldImage.color = normalColor;
            startGameButton.interactable = true;
        }
    }
    public void StartGame()
    {
        // The validation logic ensures the button is only clickable with a valid name.
        string playerName = playerNameInput.text;

        // As a final safety check, we'll still ensure the name isn't empty before proceeding,
        // though this should be guaranteed by the button's interactable state.
        if (string.IsNullOrWhiteSpace(playerName))
        {
            Debug.LogError("StartGame was called with an empty name. This should not happen.");
            return;
        }

        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.SetPlayerName(playerName);
        }

        if (TransitionManager.Instance != null)
        {
            TransitionManager.Instance.TransitionToScene(gameplaySceneName);
        }
        else
        {
            Debug.LogWarning("TransitionManager not found. Loading scene directly.");
            SceneManager.LoadScene(gameplaySceneName);
        }
    }

    private void PopulateHighScores()
    {
        if (GameDataManager.Instance == null) return;

        // Clear any old entries to prevent duplicates
        foreach (Transform child in scoreContentParent)
        {
            // A simple way to avoid destroying a potential header row
            if (child.GetComponent<ScoreEntryUI>() != null)
            {
                Destroy(child.gameObject);
            }
        }

        List<SaveData> allScores = GameDataManager.Instance.GetAllSaveData();

        // Sort the data: by most levels passed (descending), then by fewest steps (ascending)
        List<SaveData> sortedScores = allScores
            .OrderByDescending(s => s.levelsPassed)
            .ThenBy(s => s.totalSteps)
            .ToList();

        // Create a UI entry for each score
        for (int i = 0; i < sortedScores.Count; i++)
        {
            GameObject entryGO = Instantiate(scoreEntryPrefab, scoreContentParent);
            ScoreEntryUI entryUI = entryGO.GetComponent<ScoreEntryUI>();

            if (entryUI != null)
            {
                entryUI.Initialize(i + 1, sortedScores[i]);
            }
        }
    }

    private void CloseGame()
    {
# if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}