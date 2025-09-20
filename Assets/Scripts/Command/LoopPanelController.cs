using UnityEngine;
using TMPro; // For the InputField
using System.Collections.Generic;
using UnityEngine.UI;

public class LoopPanelController : MonoBehaviour
{
    [Header("References")]
    public TMP_InputField repeatInput;
    public DraggableCommand loopCardIcon;
    public Image loopIconImage;
    public Transform subSlotsParent;

    private List<CommandSlot> subCommandSlots = new List<CommandSlot>();
    void Awake()
    {
        repeatInput.text = "0"; 
        // --- NEW: Automatically find the command slots ---
        if (subSlotsParent != null)
        {
            subSlotsParent.GetComponentsInChildren<CommandSlot>(subCommandSlots);
        }
        else
        {
            Debug.LogError("SubSlotsParent is not assigned in the LoopPanelController!", this.gameObject);
        }
    }
    public void Initialize(int id, Sprite iconSprite)
    {
        // Set the logical ID for the draggable card
        loopCardIcon.loopID = id;

        // Set the visual icon sprite
        if (loopIconImage != null)
        {
            loopIconImage.sprite = iconSprite;
        }
    }

    // This method reads the UI and returns the complete loop data
    public Command GetLoopCommand()
    {
        // 1. Parse the repeat count from the input field
        int repeats = 1;
        if (!string.IsNullOrEmpty(repeatInput.text))
        {
            int.TryParse(repeatInput.text, out repeats);
            if (repeats <= 0) repeats = 1; // Ensure at least one repetition
        }

        // 2. Read the sub-commands from the slots
        var subCommands = new List<Command>();
        foreach (var slot in subCommandSlots)
        {
            if (slot.transform.childCount > 0)
            {
                var card = slot.transform.GetChild(0).GetComponent<DraggableCommand>();
                if (card != null)
                {
                    // Note: We don't support nested loops for now, so we just add simple commands.
                    subCommands.Add(new Command { Type = card.commandType });
                }
            }
        }

        // 3. Assemble and return the final Command object
        return new Command
        {
            Type = CommandType.Loop,
            RepeatCount = repeats,
            SubCommands = subCommands
        };
    }

    public void InputVaidation()
    {
        if (repeatInput.text.Length > 2)
        {
            repeatInput.text = repeatInput.text.Remove(2, repeatInput.text.Length - 2);
        }
    }
    
    public void ClearSubSlots()
    {
        repeatInput.text = "0"; 
        // This logic is the same as clearing the main slots, but for our sub-slots.
        foreach (CommandSlot slot in subCommandSlots)
        {
            if (slot.transform.childCount > 0)
            {
                Destroy(slot.transform.GetChild(0).gameObject);
            }
        }
    }
}