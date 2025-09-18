using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("System References")]
    public BoardManager boardManager; // << ADD THIS

    [Header("UI Prefabs & Parents")]
    public GameObject commandSlotPrefab;
    public GameObject moveForwardCardPrefab;
    public GameObject turnLeftCardPrefab;
    public GameObject turnRightCardPrefab;

    public Transform sequenceGridParent;
    public Transform availableCommandsParent;

    [Header("Controls")]
    public Button runButton;
    public Button resetButton;

    private List<CommandSlot> commandSlots = new List<CommandSlot>();

    void Start()
    {
        SetupUI();
    }

    private void SetupUI()
    {
        for (int i = 0; i < 12; i++)
        {
            GameObject slotGO = Instantiate(commandSlotPrefab, sequenceGridParent);
            commandSlots.Add(slotGO.GetComponent<CommandSlot>());
        }

        Instantiate(moveForwardCardPrefab, availableCommandsParent);
        Instantiate(turnLeftCardPrefab, availableCommandsParent);
        Instantiate(turnRightCardPrefab, availableCommandsParent);

        runButton.onClick.AddListener(OnRunClicked);
        resetButton.onClick.AddListener(OnResetClicked);
    }

    private void OnRunClicked()
    {
        List<CommandType> commandSequence = ReadCommandSequence();

        if (commandSequence.Count == 0)
        {
            Debug.Log("Command sequence is empty.");
            return;
        }

        // --- CHANGE: Find the player through the BoardManager ---
        if (boardManager != null && boardManager.PlayerInstance != null)
        {
            boardManager.PlayerInstance.RunCommandSequence(commandSequence);
        }
        else
        {
            Debug.LogError("BoardManager or Player instance not found! Cannot run sequence.");
        }
    }

    private List<CommandType> ReadCommandSequence()
    {
        List<CommandType> sequence = new List<CommandType>();
        foreach (CommandSlot slot in commandSlots)
        {
            if (slot.transform.childCount > 0)
            {
                DraggableCommand card = slot.transform.GetChild(0).GetComponent<DraggableCommand>();
                if (card != null)
                {
                    sequence.Add(card.commandType);
                }
            }
        }
        return sequence;
    }

    // --- NEW: Public method to be called by UnityEvents ---
    public void SetRunButtonInteractable(bool isInteractable)
    {
        if (runButton != null)
        {
            runButton.interactable = isInteractable;
        }
    }
    public void ClearCommandSlots()
    {
        Debug.Log("Clearing all command slots.");
        foreach (CommandSlot slot in commandSlots)
        {
            // Check if the slot has a card in it (a child object)
            if (slot.transform.childCount > 0)
            {
                // Destroy the card GameObject
                Destroy(slot.transform.GetChild(0).gameObject);
            }
        }
    }

    private void OnResetClicked()
    {
        ClearCommandSlots();
    }
}