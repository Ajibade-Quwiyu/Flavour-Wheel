using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Collections;

public class SwipeUI : MonoBehaviour, IDragHandler, IEndDragHandler
{
    public RectTransform[] mainPanels;
    public RectTransform[] pointerGraphics;
    public RectTransform[] indicatorDots;
    public float swipeThreshold = 0.2f;
    public float transitionSpeed = 5f;
    public float scaleTransitionSpeed = 10f;

    private int currentMainPanelIndex = 0;
    private int currentPointerIndex = 0;
    private bool isSwiping = false;
    private Vector2 startPosition;

    private Vector3 activeScale = new Vector3(1.2f, 1.2f, 1.2f);
    private Vector3 inactiveScale = Vector3.one;

    [System.Serializable]
    public class LastImageSwipedEvent : UnityEvent { }
    [SerializeField]
    public LastImageSwipedEvent onLastImageSwiped;

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
        if (!isSwiping)
        {
            if (eventData.pressPosition.y <= Screen.height / 4)
            {
                startPosition = eventData.position;
                isSwiping = true;
            }
            else
            {
                return;
            }
        }

        Vector2 delta = eventData.position - startPosition;
        float normalizedDelta = delta.x / canvasRectTransform.rect.width;

        for (int i = 0; i < pointerGraphics.Length; i++)
        {
            pointerGraphics[i].anchoredPosition += new Vector2(normalizedDelta * pointerGraphics[i].rect.width, 0);
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
        float distance = (eventData.pressPosition.x - eventData.position.x) / canvasRectTransform.rect.width;
        if (Mathf.Abs(distance) >= swipeThreshold)
        {
            if (distance > 0 && currentPointerIndex < pointerGraphics.Length - 1)
            {
                currentPointerIndex++;
            }
            else if (distance > 0 && currentPointerIndex == pointerGraphics.Length - 1)
            {
                StartCoroutine(HandleLastImageSwipe());
                return;
            }
            else if (distance < 0 && currentPointerIndex > 0)
            {
                currentPointerIndex--;
            }
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
            CanvasGroup canvasGroup = indicatorDots[i].GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = indicatorDots[i].gameObject.AddComponent<CanvasGroup>();
            }

            int correspondingPointerIndex = Mathf.RoundToInt((float)i / (indicatorDots.Length - 1) * (pointerGraphics.Length - 1));
            canvasGroup.alpha = (correspondingPointerIndex == currentPointerIndex) ? 1f : 0.5f;
        }
    }

    private void UpdatePanelPositions()
    {
        for (int i = 0; i < mainPanels.Length; i++)
        {
            Vector2 targetPosition;
            if (i == currentMainPanelIndex)
            {
                targetPosition = Vector2.zero;
            }
            else
            {
                float targetX = (i < currentMainPanelIndex) ? -1 : 1;
                targetPosition = new Vector2(targetX * canvasRectTransform.rect.width, 0);
            }
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
                CanvasGroup canvasGroup = pointerGraphics[i].GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = i == currentPointerIndex ? 1f : 0.5f;
                }

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
        {
            Vector2 targetPosition = new Vector2((i - currentPointerIndex) * pointerWidth, 0);
            StartCoroutine(MovePanel(pointerGraphics[i], targetPosition));
        }
    }

    private void EnsureCanvasGroupComponents()
    {
        foreach (RectTransform pointer in pointerGraphics)
        {
            if (pointer.GetComponent<CanvasGroup>() == null)
            {
                pointer.gameObject.AddComponent<CanvasGroup>();
            }
        }

        foreach (RectTransform dot in indicatorDots)
        {
            if (dot.GetComponent<CanvasGroup>() == null)
            {
                dot.gameObject.AddComponent<CanvasGroup>();
            }
        }
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
        {
            RectTransform activePanel = mainPanels[currentMainPanelIndex];
            Transform parentTransform = activePanel.parent;

            if (parentTransform != null)
            {
                // Set the active panel as the last sibling
                activePanel.SetAsLastSibling();
            }
        }
    }
}