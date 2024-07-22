using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class SwipeUI : MonoBehaviour, IDragHandler, IEndDragHandler
{
    public RectTransform[] mainPanels; // Array of main UI panels
    public RectTransform[] pointerGraphics; // Array of pointer graphics
    public float swipeThreshold = 0.2f; // Minimum swipe distance to trigger change
    public float transitionSpeed = 5f; // Speed of the transition between panels

    private int currentMainPanelIndex = 0;
    private int currentPointerIndex = 0;
    private bool isSwiping = false;
    private Vector2 startPosition;

    [System.Serializable]
    public class LastImageSwipedEvent : UnityEvent { }
    [SerializeField]
    public LastImageSwipedEvent onLastImageSwiped;

    private void Start()
    {
        EnsureCanvasGroupComponents();
        UpdatePanelPositions();
        UpdatePointerGraphics();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isSwiping)
        {
            // Check if the swipe started in the bottom quarter of the screen
            if (eventData.pressPosition.y <= Screen.height / 4)
            {
                startPosition = eventData.pressPosition;
                isSwiping = true;
            }
            else
            {
                return;
            }
        }

        Vector2 delta = eventData.position - startPosition;
        for (int i = 0; i < pointerGraphics.Length; i++)
        {
            pointerGraphics[i].anchoredPosition += new Vector2(delta.x, 0);
        }

        startPosition = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isSwiping)
        {
            return;
        }

        isSwiping = false;
        float distance = eventData.pressPosition.x - eventData.position.x;
        if (Mathf.Abs(distance) >= swipeThreshold * Screen.width)
        {
            if (distance > 0 && currentPointerIndex < pointerGraphics.Length - 1)
            {
                currentPointerIndex++;
            }
            else if (distance > 0 && currentPointerIndex == pointerGraphics.Length - 1)
            {
                // Last panel swiped to the right
                onLastImageSwiped?.Invoke();
            }
            else if (distance < 0 && currentPointerIndex > 0)
            {
                currentPointerIndex--;
            }
            UpdateMainPanel();
        }
        UpdatePointerGraphics();
        ResetPointerPositions();
    }

    private bool IsSwipeValid(PointerEventData eventData)
    {
        // Add your logic to determine if the swipe is valid
        return true;
    }

    private void UpdatePanelPositions()
    {
        // Ensure the main panels are set correctly based on the currentMainPanelIndex
        for (int i = 0; i < mainPanels.Length; i++)
        {
            if (i == currentMainPanelIndex)
            {
                StartCoroutine(MovePanel(mainPanels[i], new Vector2(0, mainPanels[i].anchoredPosition.y)));
            }
            else
            {
                float targetX = (i < currentMainPanelIndex) ? -Screen.width : Screen.width;
                StartCoroutine(MovePanel(mainPanels[i], new Vector2(targetX, mainPanels[i].anchoredPosition.y)));
            }
        }
    }

    private void UpdatePointerGraphics()
    {
        // Update the pointer graphics to show only 3 at a time
        for (int i = 0; i < pointerGraphics.Length; i++)
        {
            pointerGraphics[i].gameObject.SetActive(i >= currentPointerIndex - 1 && i <= currentPointerIndex + 1);
            CanvasGroup canvasGroup = pointerGraphics[i].GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = i == currentPointerIndex ? 1f : 0.5f; // Active one is fully visible, others are grayed out
            }
        }
    }

    private void UpdateMainPanel()
    {
        // Update the main panel based on the active pointer
        currentMainPanelIndex = currentPointerIndex;
        UpdatePanelPositions();
    }

    private void ResetPointerPositions()
    {
        // Reset the positions of the pointer graphics to their original positions
        for (int i = 0; i < pointerGraphics.Length; i++)
        {
            pointerGraphics[i].anchoredPosition = new Vector2((i - currentPointerIndex) * Screen.width / 3, pointerGraphics[i].anchoredPosition.y);
        }
    }

    private void EnsureCanvasGroupComponents()
    {
        // Ensure each pointer graphic has a CanvasGroup component
        foreach (RectTransform pointer in pointerGraphics)
        {
            if (pointer.GetComponent<CanvasGroup>() == null)
            {
                pointer.gameObject.AddComponent<CanvasGroup>();
            }
        }
    }

    private System.Collections.IEnumerator MovePanel(RectTransform panel, Vector2 targetPosition)
    {
        Vector2 initialPosition = panel.anchoredPosition;
        float elapsedTime = 0;
        while (elapsedTime < 1 / transitionSpeed)
        {
            panel.anchoredPosition = Vector2.Lerp(initialPosition, targetPosition, elapsedTime * transitionSpeed);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        panel.anchoredPosition = targetPosition;
    }
}
