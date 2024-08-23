using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Collections;
using System.Linq;

public class SwipeUI : MonoBehaviour, IDragHandler, IEndDragHandler
{
    public RectTransform[] mainPanels, pointerGraphics, indicatorDots;
    public float swipeThreshold = 0.2f, transitionSpeed = 5f, scaleTransitionSpeed = 10f;
    private int currentMainPanelIndex, currentPointerIndex;
    private bool isSwiping;
    private Vector2 startPosition;
    private Vector3 activeScale = new Vector3(1.2f, 1.2f, 1.2f), inactiveScale = new Vector3(.75f, .75f, .75f);
    [System.Serializable] public class LastImageSwipedEvent : UnityEvent { }
    [SerializeField] public LastImageSwipedEvent onLastImageSwiped;
    private RectTransform canvasRectTransform;

    private void Start()
    {
        canvasRectTransform = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
        EnsureCanvasGroupComponents();
        UpdatePanelPositions();
        UpdatePointerGraphics();
        UpdateIndicatorDots();
        UpdateActiveMainPanelSiblingIndex();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isSwiping && eventData.pressPosition.y <= Screen.height / 4)
        {
            startPosition = eventData.position;
            isSwiping = true;
        }
        if (!isSwiping) return;

        float normalizedDelta = (eventData.position - startPosition).x / canvasRectTransform.rect.width;
        foreach (var graphic in pointerGraphics)
            graphic.anchoredPosition += new Vector2(normalizedDelta * graphic.rect.width, 0);
        startPosition = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isSwiping) return;
        isSwiping = false;

        float distance = (eventData.pressPosition.x - eventData.position.x) / canvasRectTransform.rect.width;
        if (Mathf.Abs(distance) >= swipeThreshold)
        {
            if (distance > 0)
            {
                if (currentPointerIndex < pointerGraphics.Length - 1)
                    currentPointerIndex++;
                else
                {
                    StartCoroutine(HandleLastImageSwipe());
                    return;
                }
            }
            else if (currentPointerIndex > 0)
                currentPointerIndex--;

            UpdateMainPanel();
        }
        UpdatePointerGraphics();
        UpdateIndicatorDots();
        ResetPointerPositions();
    }

    private IEnumerator HandleLastImageSwipe()
    {
        UpdateMainPanel();
        UpdatePointerGraphics();
        UpdateIndicatorDots();
        ResetPointerPositions();
        yield return new WaitForSeconds(1 / transitionSpeed);
        onLastImageSwiped?.Invoke();
    }

    private void UpdateIndicatorDots()
    {
        for (int i = 0; i < indicatorDots.Length; i++)
        {
            var canvasGroup = indicatorDots[i].GetComponent<CanvasGroup>() ?? indicatorDots[i].gameObject.AddComponent<CanvasGroup>();
            int correspondingPointerIndex = Mathf.RoundToInt((float)i / (indicatorDots.Length - 1) * (pointerGraphics.Length - 1));
            canvasGroup.alpha = (correspondingPointerIndex == currentPointerIndex) ? 1f : 0.5f;
        }
    }
    private void UpdatePanelPositions()
    {
        for (int i = 0; i < mainPanels.Length; i++)
        {
            float targetX = i == currentMainPanelIndex ? 0 : (i < currentMainPanelIndex ? -1 : 1);
            Vector2 targetPosition = new Vector2(targetX * canvasRectTransform.rect.width, 0);
            StartCoroutine(MovePanel(mainPanels[i], targetPosition));
        }
        UpdateActiveMainPanelSiblingIndex();
    }

    private void UpdatePointerGraphics()
    {
        for (int i = 0; i < pointerGraphics.Length; i++)
        {
            bool isActive = (i >= currentPointerIndex - 1 && i <= currentPointerIndex + 1);
            pointerGraphics[i].gameObject.SetActive(isActive);
            if (isActive)
            {
                pointerGraphics[i].GetComponent<CanvasGroup>().alpha = i == currentPointerIndex ? 1f : 0.5f;
                StartCoroutine(ScalePointerGraphic(pointerGraphics[i], i == currentPointerIndex ? activeScale : inactiveScale));
            }
        }
    }

    private IEnumerator ScalePointerGraphic(RectTransform graphic, Vector3 targetScale)
    {
        Vector3 startScale = graphic.localScale;
        float elapsedTime = 0f;
        while (elapsedTime < 1f / scaleTransitionSpeed)
        {
            graphic.localScale = Vector3.Lerp(startScale, targetScale, elapsedTime * scaleTransitionSpeed);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        graphic.localScale = targetScale;
    }

    private void UpdateMainPanel()
    {
        currentMainPanelIndex = currentPointerIndex;
        UpdatePanelPositions();
    }

    private void ResetPointerPositions()
    {
        float pointerWidth = canvasRectTransform.rect.width / 3;
        for (int i = 0; i < pointerGraphics.Length; i++)
            StartCoroutine(MovePanel(pointerGraphics[i], new Vector2((i - currentPointerIndex) * pointerWidth, 0)));
    }
    private void EnsureCanvasGroupComponents()
    {
        foreach (RectTransform pointer in pointerGraphics.Concat(indicatorDots))
            pointer.gameObject.AddComponent<CanvasGroup>();
    }

    private IEnumerator MovePanel(RectTransform panel, Vector2 targetPosition)
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

    private void UpdateActiveMainPanelSiblingIndex()
    {
        if (mainPanels.Length > 0 && currentMainPanelIndex >= 0 && currentMainPanelIndex < mainPanels.Length)
            mainPanels[currentMainPanelIndex].SetAsLastSibling();
    }
}