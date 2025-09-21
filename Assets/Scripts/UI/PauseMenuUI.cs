using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject visualPanel; // Reference to the child panel

    [Header("Buttons")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button settingsButton; // Good to add for completeness
    [SerializeField] private Button endButton;

    [Header("Scene Settings")]
    [SerializeField] private string mainMenuSceneName = "MainMenuScene";

    private bool isPaused = false;

    void Start()
    {
        // Ensure the VISUAL panel is hidden at the start.
        // This parent controller object will remain active.
        visualPanel.SetActive(false);
        Time.timeScale = 1f;

        // Hook up button listeners
        continueButton.onClick.AddListener(ContinueGame);
        settingsButton.onClick.AddListener(OpenSettings);
        endButton.onClick.AddListener(EndGameSession);
    }

    void Update()
    {
        // This Update will now run every frame because this GameObject is always active.
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
    public void OpenSettings() { Debug.Log("Settings Button Clicked!"); }
    public void EndGameSession()
    {
        Time.timeScale = 1f;
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