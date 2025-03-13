using UnityEngine;
using UnityEngine.Events;

public class ARSwipeUI : MonoBehaviour
{
    [Header("UI Elements")]
    public RectTransform[] mainPanels;
    public RectTransform[] pointerGraphics;
    public RectTransform[] indicatorDots;

    [Header("Settings")]
    private Camera ARCamera;
    public float transitionSpeed = 5f;
    public float worldSpaceSpacing = 0.5f;
    public float minSwipeDistance = 50f;
    public float swipeSensitivity = 1f;
    public float lastPanelDisplayTime = 0.5f;

    [System.Serializable] public class LastImageEvent : UnityEvent { }
    public LastImageEvent onLastImageSwiped;

    private int currentIndex = 0;
    private Vector2 touchStart;
    private bool isDragging = false;
    private float currentDragOffset = 0f;
    private Vector3[] initialPositions;

    void Start()
    {
        EnsureCenter();
        InitializeState();
        StoreInitialPositions();
    }

    private void EnsureCenter()
    {
        transform.position = Vector3.zero;
        transform.localPosition = Vector3.zero;

        foreach (var panel in mainPanels)
        {
            panel.anchoredPosition = Vector2.zero;
        }
    }

    private void InitializeState()
    {
        ARCamera = GameObject.FindObjectOfType<UnityEngine.XR.ARFoundation.ARCameraManager>()?.GetComponent<Camera>();

        for (int i = 0; i < mainPanels.Length; i++)
        {
            mainPanels[i].gameObject.SetActive(i == 0);
            mainPanels[i].anchoredPosition = Vector2.zero;
        }

        UpdateVisuals();
    }

    private void StoreInitialPositions()
    {
        initialPositions = new Vector3[pointerGraphics.Length];
        for (int i = 0; i < pointerGraphics.Length; i++)
        {
            initialPositions[i] = pointerGraphics[i].position;
        }
    }

    void Update()
    {
#if UNITY_EDITOR
        HandleEditorInput();
#else
        HandleMobileInput();
#endif

        // Check for swipe beyond last panel
        if (currentIndex == mainPanels.Length - 1 && isDragging)
        {
            float dragDelta = (Input.mousePosition.x - touchStart.x) * swipeSensitivity;
            if (dragDelta < -minSwipeDistance)
            {
                isDragging = false;
                onLastImageSwiped?.Invoke();
            }
        }
    }

    private void HandleEditorInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            touchStart = Input.mousePosition;
            isDragging = true;
        }
        else if (Input.GetMouseButton(0) && isDragging)
        {
            HandleDrag(Input.mousePosition);
        }
        else if (Input.GetMouseButtonUp(0) && isDragging)
        {
            EndDrag(Input.mousePosition);
        }
    }

    private void HandleMobileInput()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    touchStart = touch.position;
                    isDragging = true;
                    break;

                case TouchPhase.Moved:
                    if (isDragging)
                    {
                        HandleDrag(touch.position);
                    }
                    break;

                case TouchPhase.Ended:
                    if (isDragging)
                    {
                        EndDrag(touch.position);
                    }
                    break;
            }
        }
    }

    private void HandleDrag(Vector2 currentPosition)
    {
        float dragDelta = (currentPosition.x - touchStart.x) * swipeSensitivity;
        currentDragOffset = dragDelta / Screen.width * worldSpaceSpacing;

        for (int i = 0; i < pointerGraphics.Length; i++)
        {
            float baseOffset = (i - currentIndex) * worldSpaceSpacing;
            Vector3 newPos = initialPositions[i] + new Vector3(baseOffset + currentDragOffset, 0, 0);
            pointerGraphics[i].position = newPos;
        }

        if (mainPanels[currentIndex].gameObject.activeSelf)
        {
            float xOffset = currentDragOffset * 1000;
            mainPanels[currentIndex].anchoredPosition = new Vector2(xOffset, 0);
        }
    }

    private void EndDrag(Vector2 endPosition)
    {
        if (!isDragging) return;

        isDragging = false;
        float dragDistance = endPosition.x - touchStart.x;

        if (Mathf.Abs(dragDistance) > minSwipeDistance)
        {
            int direction = dragDistance > 0 ? -1 : 1;
            int newIndex = Mathf.Clamp(currentIndex + direction, 0, pointerGraphics.Length - 1);

            if (newIndex != currentIndex)
            {
                SelectIndex(newIndex);
            }
            else
            {
                SnapToPosition();
            }
        }
        else
        {
            SnapToPosition();
        }

        currentDragOffset = 0f;
    }

    private void SelectIndex(int newIndex)
    {
        if (newIndex == currentIndex) return;

        // Activate new panel before transition
        mainPanels[newIndex].gameObject.SetActive(true);
        StartCoroutine(TransitionPanels(currentIndex, newIndex));

        currentIndex = newIndex;
        UpdateVisuals();
        StartCoroutine(TransitionPointers());
    }

    private System.Collections.IEnumerator TransitionPanels(int oldIndex, int newIndex)
    {
        float elapsed = 0;
        Vector2 startPos = mainPanels[oldIndex].anchoredPosition;
        Vector2 endPos = new Vector2(-1000 * Mathf.Sign(newIndex - oldIndex), 0);
        Vector2 newStartPos = new Vector2(1000 * Mathf.Sign(newIndex - oldIndex), 0);
        Vector2 newEndPos = Vector2.zero;

        mainPanels[newIndex].anchoredPosition = newStartPos;

        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * transitionSpeed;
            float t = Mathf.Clamp01(elapsed);

            mainPanels[oldIndex].anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            mainPanels[newIndex].anchoredPosition = Vector2.Lerp(newStartPos, newEndPos, t);

            yield return null;
        }

        mainPanels[oldIndex].gameObject.SetActive(false);
        mainPanels[oldIndex].anchoredPosition = Vector2.zero;
        mainPanels[newIndex].anchoredPosition = Vector2.zero;
    }

    private System.Collections.IEnumerator TransitionPointers()
    {
        float elapsed = 0;
        Vector3[] startPositions = new Vector3[pointerGraphics.Length];
        Vector3[] endPositions = new Vector3[pointerGraphics.Length];

        for (int i = 0; i < pointerGraphics.Length; i++)
        {
            startPositions[i] = pointerGraphics[i].position;
            float xOffset = (i - currentIndex) * worldSpaceSpacing;
            endPositions[i] = new Vector3(xOffset, initialPositions[i].y, initialPositions[i].z);
        }

        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * transitionSpeed;
            float t = Mathf.Clamp01(elapsed);

            for (int i = 0; i < pointerGraphics.Length; i++)
            {
                pointerGraphics[i].position = Vector3.Lerp(startPositions[i], endPositions[i], t);
            }

            yield return null;
        }

        for (int i = 0; i < pointerGraphics.Length; i++)
        {
            pointerGraphics[i].position = endPositions[i];
        }
    }

    private void SnapToPosition()
    {
        StartCoroutine(SnapPanelToCenter());
    }

    private System.Collections.IEnumerator SnapPanelToCenter()
    {
        float elapsed = 0;
        Vector2 startPos = mainPanels[currentIndex].anchoredPosition;

        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * transitionSpeed;
            mainPanels[currentIndex].anchoredPosition = Vector2.Lerp(startPos, Vector2.zero, elapsed);
            yield return null;
        }

        mainPanels[currentIndex].anchoredPosition = Vector2.zero;
    }

    private void UpdateVisuals()
    {
        for (int i = 0; i < pointerGraphics.Length; i++)
        {
            var canvasGroup = pointerGraphics[i].GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = pointerGraphics[i].gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = i == currentIndex ? 1f : 0.5f;
        }

        for (int i = 0; i < indicatorDots.Length; i++)
        {
            var canvasGroup = indicatorDots[i].GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = indicatorDots[i].gameObject.AddComponent<CanvasGroup>();
            int correspondingPointerIndex = Mathf.RoundToInt((float)i / (indicatorDots.Length - 1) * (pointerGraphics.Length - 1));
            canvasGroup.alpha = (correspondingPointerIndex == currentIndex) ? 1f : 0.5f;
        }
    }
}