using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
    
    [Header("Settings")]
    public int mainSlotCount = 12;
    [Header("Loop UI")]
    public GameObject loopPanelPrefab;
    public Transform loopSectionParent;
    public int numberOfLoops = 3;
    public List<Sprite> loopIconSprites; // --- ADD THIS LIST ---
    
    private List<LoopPanelController> loopPanels = new List<LoopPanelController>();
    private List<CommandSlot> mainCommandSlots = new List<CommandSlot>();

    void Start()
    {
        SetupUI();
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
        List<Command> commandSequence = ReadCommandSequence();
        
        if (boardManager.PlayerInstance != null)
        {
            boardManager.PlayerInstance.RunCommandSequence(commandSequence);
        }
        else
        {
            Debug.LogError("Player instance not found! Cannot run sequence.");
        }
    }
    
    private void OnResetClicked()
    {
        ClearCommandSlots();
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
    public void ClearCommandSlots()
    {
        Debug.Log("Clearing all main command slots.");
        foreach (CommandSlot slot in mainCommandSlots)
        {
            if (slot.transform.childCount > 0)
            {
                Destroy(slot.transform.GetChild(0).gameObject);
            }
        }
    }

    /// <summary>
    /// Public method that can be called by UnityEvents (e.g., from PlayerController) to enable/disable the run button.
    /// </summary>
    public void SetRunButtonInteractable(bool isInteractable)
    {
        runButton.interactable = isInteractable;
    }
}