using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TableData : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    public ScrollRect[] scrollViews;
    public RectTransform[] contentRects;
    public float swipeThreshold = 0.2f; // Percentage of the screen width to trigger a swipe
    public float transitionSpeed = 5f;  // Speed of the transition between views
    public float minScale = 0.4f;       // Minimum scale during transition
    public float maxScale = 1.0f;       // Maximum scale during transition

    private int currentIndex = 0;
    private Vector3 startPosition;
    private bool isTransitioning = false;

    private void Start()
    {
        // Ensure the arrays are the same length
        if (scrollViews.Length != contentRects.Length)
        {
            Debug.LogError("ScrollViews and ContentRects arrays must be of the same length!");
            return;
        }

        // Initialize scrollViews and contentRects
        for (int i = 0; i < scrollViews.Length; i++)
        {
            // Ensure the ScrollRect component is assigned
            if (scrollViews[i] == null)
            {
                Debug.LogError($"ScrollRect at index {i} is not assigned!");
                continue;
            }

            // Get the content RectTransform
            contentRects[i] = scrollViews[i].content;

            // Ensure Content Size Fitter is attached and configured
            ContentSizeFitter contentSizeFitter = contentRects[i].GetComponent<ContentSizeFitter>();
            if (contentSizeFitter == null)
            {
                contentSizeFitter = contentRects[i].gameObject.AddComponent<ContentSizeFitter>();
            }
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Ensure Vertical Layout Group is attached and configured
            VerticalLayoutGroup verticalLayoutGroup = contentRects[i].GetComponent<VerticalLayoutGroup>();
            if (verticalLayoutGroup == null)
            {
                verticalLayoutGroup = contentRects[i].gameObject.AddComponent<VerticalLayoutGroup>();
            }
            verticalLayoutGroup.childForceExpandHeight = false;
            verticalLayoutGroup.childControlHeight = true;

            // Set initial scale
            scrollViews[i].transform.localScale = (i == currentIndex) ? Vector3.one * maxScale : Vector3.one * minScale;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        startPosition = eventData.pressPosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        float swipeDistance = eventData.position.x - startPosition.x;
        float screenWidth = Screen.width;

        if (Mathf.Abs(swipeDistance) > screenWidth * swipeThreshold)
        {
            if (swipeDistance > 0)
            {
                // Swipe right
                ShowPreviousScrollView();
            }
            else
            {
                // Swipe left
                ShowNextScrollView();
            }
        }
    }

    private void ShowPreviousScrollView()
    {
        if (currentIndex > 0)
        {
            int targetIndex = currentIndex - 1;
            StartCoroutine(SmoothTransitionTo(targetIndex));
        }
    }

    private void ShowNextScrollView()
    {
        if (currentIndex < scrollViews.Length - 1)
        {
            int targetIndex = currentIndex + 1;
            StartCoroutine(SmoothTransitionTo(targetIndex));
        }
    }

    private System.Collections.IEnumerator SmoothTransitionTo(int targetIndex)
    {
        if (isTransitioning)
            yield break;

        isTransitioning = true;

        ScrollRect currentScrollView = scrollViews[currentIndex];
        ScrollRect targetScrollView = scrollViews[targetIndex];

        float elapsedTime = 0f;
        Vector3 startScaleCurrent = Vector3.one * maxScale;
        Vector3 endScaleCurrent = Vector3.one * minScale;
        Vector3 startScaleTarget = Vector3.one * minScale;
        Vector3 endScaleTarget = Vector3.one * maxScale;

        while (elapsedTime < 1f)
        {
            elapsedTime += Time.deltaTime * transitionSpeed;
            float t = Mathf.SmoothStep(0f, 1f, elapsedTime);

            currentScrollView.transform.localScale = Vector3.Lerp(startScaleCurrent, endScaleCurrent, t);
            targetScrollView.transform.localScale = Vector3.Lerp(startScaleTarget, endScaleTarget, t);

            yield return null;
        }

        currentScrollView.transform.localScale = endScaleCurrent;
        targetScrollView.transform.localScale = endScaleTarget;

        currentIndex = targetIndex;
        isTransitioning = false;
    }
}
