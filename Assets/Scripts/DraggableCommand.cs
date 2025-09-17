using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public enum CommandType
{
    None, 
    MoveForward,
    TurnLeft,
    TurnRight
}
public class DraggableCommand : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public CommandType commandType;
    [Tooltip("Is this an infinite source card in the palette?")]
    public bool isSourceCard = false;

    private Transform originalParent;
    private CanvasGroup canvasGroup;
    private GameObject placeholder; // A temporary object to hold our place in a layout group

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;

        if (isSourceCard)
        {
            // --- SOURCE CARD LOGIC ---
            // Create a copy, then THIS script instance will continue to operate on the copy.
            GameObject newCard = Instantiate(gameObject, originalParent);
            newCard.GetComponent<DraggableCommand>().isSourceCard = false;
            
            // The NEW card is the one being dragged. We trick the event system by
            // making it the target of all subsequent drag events for this pointer.
            eventData.pointerDrag = newCard;
            return; // The original source card does nothing further.
        }

        // --- SLOTTED CARD LOGIC ---
        // Create a placeholder to keep the UI layout from shifting
        placeholder = new GameObject("CardPlaceholder");
        placeholder.transform.SetParent(transform.parent, false);
        placeholder.transform.SetSiblingIndex(transform.GetSiblingIndex());

        // Detach the card and move it to the top to be rendered over everything
        transform.SetParent(transform.root);

        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.6f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // This is now only called on the copy or the card from the slot
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isSourceCard) return;

        // --- NEW, SIMPLIFIED LOGIC ---

        // 1. Raycast to find a valid drop slot.
        CommandSlot dropSlot = null;
        var raycastResults = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, raycastResults);
        foreach (var result in raycastResults)
        {
            dropSlot = result.gameObject.GetComponent<CommandSlot>();
            if (dropSlot != null && dropSlot.transform.childCount == 0)
            {
                // Found a valid, empty slot. Break the loop.
                break;
            }
            // If the slot is not empty, keep searching.
            dropSlot = null;
        }

        // 2. Decide what to do based on the result.
        if (dropSlot != null)
        {
            // --- CASE A: Successful Drop ---
            // We found a valid, empty slot to drop the card into.
            transform.SetParent(dropSlot.transform);
            transform.localPosition = Vector3.zero; // Snap to center
        }
        else
        {
            // --- CASE B: Invalid Drop ---
            // No valid slot was found under the cursor.
            // This means the card should either snap back or be destroyed.

            // If the original parent was a valid slot, the player is trying to remove it. DESTROY.
            if (originalParent.GetComponent<CommandSlot>() != null)
            {
                Debug.Log("Card dropped on invalid area. Deleting.");
                Destroy(gameObject);
            }
            else
            {
                // This case should theoretically not happen for non-source cards,
                // but as a fallback, snap it back to its original position.
                transform.SetParent(originalParent);
                transform.SetSiblingIndex(placeholder.transform.GetSiblingIndex());
            }
        }

        // 3. Cleanup is always performed.
        if (placeholder != null)
        {
            Destroy(placeholder);
        }
        
        // Only restore visuals if the card wasn't destroyed.
        if (this != null) 
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1.0f;
        }
    }
}