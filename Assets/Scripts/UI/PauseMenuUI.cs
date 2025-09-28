using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject visualPanel;
    [SerializeField] private GameObject settingsPanel; 

    [Header("Buttons")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button settingsButton; 
    [SerializeField] private Button endButton; 

    [Header("Scene Settings")]
    [SerializeField] private string mainMenuSceneName = "MainMenuScene";

    private bool isPaused = false;

    void Start()
    {
        visualPanel.SetActive(false);
        Time.timeScale = 1f;

        continueButton.onClick.AddListener(ContinueGame);
        settingsButton.onClick.AddListener(OpenSettings);
        endButton.onClick.AddListener(EndGameSession);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    private void TogglePause()
    {
        if (TransitionManager.Instance != null && TransitionManager.Instance.isTransitioning) return;

        isPaused = !isPaused;
        visualPanel.SetActive(isPaused); // Toggle the VISUAL panel

        Time.timeScale = isPaused ? 0f : 1f;
    }

    // --- Button Methods (no changes needed here) ---

    public void ContinueGame() { TogglePause(); }
    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
    }
    
    public void EndGameSession()
    {
        Time.timeScale = 1f;

        MainMenuController.ShowHighScoresOnLoad = true;

        if (TransitionManager.Instance != null)
        {
            TransitionManager.Instance.TransitionToScene(mainMenuSceneName);
        }
        else
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}