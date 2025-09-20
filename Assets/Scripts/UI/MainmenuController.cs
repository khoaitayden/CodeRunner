using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // Required for changing scenes
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private Button startGameButton;

    [Header("Scene Settings")]
    [SerializeField] private string gameplaySceneName = "GameplayScene"; // The name of your game scene

    void Start()
    {
        // Set the input field to the last saved name, if any
        if (GameDataManager.Instance != null)
        {
            playerNameInput.text = GameDataManager.Instance.currentSessionData.playerName;
        }

        // Add a listener to the button's click event
        startGameButton.onClick.AddListener(StartGame);
    }

    public void StartGame()
    {
        // 1. Get the player name from the input field
        string playerName = playerNameInput.text;

        // 2. Provide a default name if the input is empty
        if (string.IsNullOrWhiteSpace(playerName))
        {
            playerName = "Player";
        }

        // 3. Save the name using our persistent GameDataManager
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.SetPlayerName(playerName);
        }

        // 4. Use the TransitionManager to load the gameplay scene
        if (TransitionManager.Instance != null)
        {
            // This plays the fade-to-black, then loads the scene, then fades back in.
            TransitionManager.Instance.PlayTransition(() => SceneManager.LoadScene(gameplaySceneName));
        }
        else
        {
            // Fallback if the transition manager isn't in the scene
            SceneManager.LoadScene(gameplaySceneName);
            
        }
    }
}