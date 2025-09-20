using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("System References")]
    public BoardManager boardManager;

    [Header("UI Prefabs")]
    public GameObject commandSlotPrefab;
    public GameObject moveForwardCardPrefab;
    public GameObject turnLeftCardPrefab;
    public GameObject turnRightCardPrefab;

    [Header("UI Parents")]
    public Transform sequenceGridParent;
    public Transform availableCommandsParent;

    [Header("Controls")]
    public Button runButton;
    public Button resetButton;

    [Header("Gameplay UI")]
    public TextMeshProUGUI stepCountText;
    public TextMeshProUGUI currentLevelText;
    public TMP_InputField playerNameInput;

    [Header("Settings")]
    public int mainSlotCount = 12;
    [Header("Loop UI")]
    public GameObject loopPanelPrefab;
    public Transform loopSectionParent;
    public int numberOfLoops = 3;
    private PlayerController currentPlayer;
    public List<Sprite> loopIconSprites;
    private List<LoopPanelController> loopPanels = new List<LoopPanelController>();
    private List<CommandSlot> mainCommandSlots = new List<CommandSlot>();
    private int historicalStepTotal = 0;

    void Start()
    {
        SetupUI();

        // The GameDataManager now handles creating new data automatically.
        // We just need to read it.
        if (GameDataManager.Instance != null)
        {
            // Display the default data for this new session
            playerNameInput.text = GameDataManager.Instance.currentSessionData.playerName;
            UpdateTotalStepCount(); // This will show "Total Steps: 0"
        }

        playerNameInput.onEndEdit.AddListener(OnPlayerNameChanged);
    }

    void OnDestroy()
    {
        if (currentPlayer != null)
        {
            currentPlayer.OnStepTaken.RemoveListener(UpdateStepDisplay);
        }
    }
    private void SetupUI()
    {
        // 1. Create the main command slots in the grid
        for (int i = 0; i < mainSlotCount; i++)
        {
            GameObject slotGO = Instantiate(commandSlotPrefab, sequenceGridParent);
            mainCommandSlots.Add(slotGO.GetComponent<CommandSlot>());
        }

        // 2. Create the loop panels
        for (int i = 0; i < numberOfLoops; i++)
        {
            // Safety check to prevent errors if you don't have enough sprites
            if (i >= loopIconSprites.Count)
            {
                Debug.LogError($"Not enough loop icon sprites assigned in UIManager! Expected {numberOfLoops}, but only found {loopIconSprites.Count}.");
                break;
            }

            GameObject loopGO = Instantiate(loopPanelPrefab, loopSectionParent);
            var controller = loopGO.GetComponent<LoopPanelController>();

            // --- CHANGE THIS LINE ---
            // controller.Initialize(i + 1);
            controller.Initialize(i + 1, loopIconSprites[i]); // Pass the sprite

            loopPanels.Add(controller);
        }

        // 3. Create the initial available command cards in the palette
        Instantiate(moveForwardCardPrefab, availableCommandsParent);
        Instantiate(turnLeftCardPrefab, availableCommandsParent);
        Instantiate(turnRightCardPrefab, availableCommandsParent);

        // 4. Hook up the button click events
        runButton.onClick.AddListener(OnRunClicked);
        resetButton.onClick.AddListener(OnResetClicked);
    }

    private void OnRunClicked()
    {
        // --- SUBSCRIBE TO THE PLAYER'S EVENTS ---
        if (boardManager.PlayerInstance != null)
        {
            // If the player has changed (e.g., new level), re-subscribe
            if (currentPlayer != boardManager.PlayerInstance)
            {
                if (currentPlayer != null)
                    currentPlayer.OnStepTaken.RemoveListener(UpdateStepDisplay);

                currentPlayer = boardManager.PlayerInstance;
                currentPlayer.OnStepTaken.AddListener(UpdateStepDisplay);
            }

            // Now run the sequence
            List<Command> commandSequence = ReadCommandSequence();
            currentPlayer.RunCommandSequence(commandSequence);
        }
    }
    public void UpdateStepDisplay(int currentAttemptSteps)
    {
        if (stepCountText != null)
        {
            int displayTotal = historicalStepTotal + currentAttemptSteps;
            stepCountText.text = "Steps: " + displayTotal;
        }
    }

    // This is called by the Reset button in the UI
    private void OnResetClicked()
    {
        ClearAllCommandSlots();
    }

    /// <summary>
    /// Reads the entire visual program from the UI and translates it into a list of Command objects.
    /// </summary>
    private List<Command> ReadCommandSequence()
    {
        var sequence = new List<Command>();
        foreach (CommandSlot slot in mainCommandSlots)
        {
            if (slot.transform.childCount > 0)
            {
                DraggableCommand card = slot.transform.GetChild(0).GetComponent<DraggableCommand>();
                if (card != null)
                {
                    if (card.commandType == CommandType.Loop)
                    {
                        // It's a loop card. Find its corresponding panel and get the data from it.
                        LoopPanelController targetPanel = loopPanels.Find(p => p.loopCardIcon.loopID == card.loopID);
                        if (targetPanel != null)
                        {
                            sequence.Add(targetPanel.GetLoopCommand());
                        }
                        else
                        {
                            Debug.LogWarning($"Could not find Loop Panel with ID: {card.loopID}");
                        }
                    }
                    else
                    {
                        // It's a simple command.
                        sequence.Add(new Command { Type = card.commandType });
                    }
                }
            }
        }
        return sequence;
    }

    /// <summary>
    /// Clears all cards from the main sequence grid, but leaves loop panels and the palette untouched.
    /// </summary>
    public void ClearAllCommandSlots()
    {
        // 1. Clear the main command slots (This part is unchanged)
        Debug.Log("Clearing all MAIN command slots.");
        foreach (CommandSlot slot in mainCommandSlots)
        {
            if (slot.transform.childCount > 0)
            {
                Destroy(slot.transform.GetChild(0).gameObject);
            }
        }

        // --- 2. NEW: Clear all the loop sub-slots ---
        Debug.Log("Clearing all LOOP command slots.");
        foreach (LoopPanelController panel in loopPanels)
        {
            // Tell each loop panel to clear itself.
            panel.ClearSubSlots();
        }
    }

    /// <summary>
    /// Public method that can be called by UnityEvents (e.g., from PlayerController) to enable/disable the run button.
    /// </summary>
    public void SetRunButtonInteractable(bool isInteractable)
    {
        runButton.interactable = isInteractable;
    }

    public void UpdateCurrentLevelText(int level)
    {
        currentLevelText.text = "Level: " + level;
    }

    private void OnPlayerNameChanged(string newName)
    {
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.SetPlayerName(newName);
        }
    }

    public void UpdateTotalStepCount()
    {
        if (stepCountText != null && GameDataManager.Instance != null)
        {
            stepCountText.text = "Steps: " + GameDataManager.Instance.currentSessionData.totalSteps;
        }
    }
}