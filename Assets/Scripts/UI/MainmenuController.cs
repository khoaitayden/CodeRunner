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
    [SerializeField] private GameObject settingsPanel;

    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button settingsButton;
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
    [SerializeField] private Color errorColor; 

    private Image inputFieldImage;

    void Start()
    {
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.BeginNewSession();
        }
        inputFieldImage = playerNameInput.GetComponent<Image>();

        if (GameDataManager.Instance != null)
        {
            playerNameInput.text = GameDataManager.Instance.currentSessionData.playerName;
        }


        if (inputFieldImage == null)
        {
            Debug.LogError("Player Name InputField is missing an Image component!");
        }

        startGameButton.onClick.AddListener(StartGame);
        highScoreButton.onClick.AddListener(ShowHighScores);
        settingsButton.onClick.AddListener(OpenSettings);
        backButton.onClick.AddListener(ShowMainMenu);
        quitButton.onClick.AddListener(CloseGame);

        playerNameInput.onValueChanged.AddListener(ValidateName);

        if (ShowHighScoresOnLoad)
        {
            ShowHighScores();
            ShowHighScoresOnLoad = false; 
        }
        else
        {
            ShowMainMenu();
        }
        MusicManager.Instance?.StartPlaylist(true);
        ValidateName(playerNameInput.text);
    }


    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
    }
    private void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        highScorePanel.SetActive(false);
    }

    private void ShowHighScores()
    {
        mainMenuPanel.SetActive(false);
        highScorePanel.SetActive(true);
        PopulateHighScores();
    }

    private void ValidateName(string currentName)
    {
        if (GameDataManager.Instance == null) return;

        if (GameDataManager.Instance.PlayerNameExists(currentName))
        {

            if (inputFieldImage != null) inputFieldImage.color = errorColor;
            startGameButton.interactable = false;
        }
        else if (string.IsNullOrWhiteSpace(currentName))
        {
            if (inputFieldImage != null) inputFieldImage.color = normalColor;
            startGameButton.interactable = false;
        }
        else
        {
            if (inputFieldImage != null) inputFieldImage.color = normalColor;
            startGameButton.interactable = true;
        }
    }
    public void StartGame()
    {
        string playerName = playerNameInput.text;
        MusicManager.Instance?.StartPlaylist(false);
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

        foreach (Transform child in scoreContentParent)
        {
            if (child.GetComponent<ScoreEntryUI>() != null)
            {
                Destroy(child.gameObject);
            }
        }

        List<SaveData> allScores = GameDataManager.Instance.GetAllSaveData();

        List<SaveData> sortedScores = allScores
            .OrderByDescending(s => s.levelsPassed)
            .ThenBy(s => s.totalSteps)
            .ToList();

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