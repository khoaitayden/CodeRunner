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
        string playerName = playerNameInput.text;

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
            SceneManager.LoadScene(gameplaySceneName);
        }
    }
}