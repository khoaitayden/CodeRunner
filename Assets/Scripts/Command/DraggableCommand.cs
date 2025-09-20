using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public enum CommandType
{
    None,
    MoveForward,
    TurnLeft,
    TurnRight,
    Loop
}
public class DraggableCommand : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public CommandType commandType;
    [Tooltip("Is this an infinite source card in the palette?")]
    public bool isSourceCard = false;
    public int loopID = 0;

    private Transform originalParent;
    private CanvasGroup canvasGroup;
    private LayoutElement layoutElement; // To prevent the card from resizing when detached

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        // --- ADD THIS SAFETY CHECK ---
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        layoutElement = GetComponent<LayoutElement>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;

        if (isSourceCard)
        {
            // Create a copy that will be the actual dragged object
            GameObject newCard = Instantiate(gameObject, transform.root);
            newCard.GetComponent<DraggableCommand>().isSourceCard = false;
            
            // The NEW card is the one being dragged.
            eventData.pointerDrag = newCard;
            return;
        }

        // --- Logic for cards already in a slot ---
        
        // Prevent the card from resizing by temporarily ignoring the layout group
        if (layoutElement) layoutElement.ignoreLayout = true;
        
        // Detach and move to top
        transform.SetParent(transform.root);

        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.6f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isSourceCard) return;

        // --- Logic for the dragged copy or a card moved from a slot ---

        // 1. Raycast to find a valid drop slot.
        CommandSlot dropSlot = null;
        var raycastResults = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, raycastResults);
        foreach (var result in raycastResults)
        {
            // Check for a CommandSlot
            var slot = result.gameObject.GetComponent<CommandSlot>();
            if (slot != null && slot.transform.childCount == 0)
            {
                dropSlot = slot;
                break; // Found a valid, empty slot
            }
        }

        // 2. Decide what to do.
        if (dropSlot != null)
        {
            // --- CASE A: Successful Drop on an empty CommandSlot ---
            transform.SetParent(dropSlot.transform);
            transform.localPosition = Vector3.zero;
        }
        else
        {
            // --- CASE B: Invalid Drop (not on an empty CommandSlot) ---
            // The card should be destroyed. This handles both dropping on the background
            // AND dropping back onto the source palette.
            Destroy(gameObject);
        }
        
        // 3. Cleanup.
        if (layoutElement) layoutElement.ignoreLayout = false;
        
        if (this != null) // Safety check in case the object was destroyed
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1.0f;
        }
    }
}
