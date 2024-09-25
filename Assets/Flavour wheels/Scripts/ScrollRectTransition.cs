using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ScrollRectTransition : MonoBehaviour
{
    public ScrollRect scrollRectA;
    public ScrollRect scrollRectB;
    public Button transitionButton;
    public float maxScale = 1.0f;
    public float minScale = 0.4f;
    public float transitionSpeed = 5f;

    private RectTransform rectA;
    private RectTransform rectB;
    private bool isAActive = true;
    private bool isTransitioning = false;
    private float screenWidth;
    private Coroutine transitionCoroutine = null;

    private void Start()
    {
        InitializeScrollRects();
        if (transitionButton != null)
        {
            transitionButton.onClick.AddListener(OnTransitionButtonClick);
        }
        else
        {
            Debug.LogWarning("Transition button is not assigned!");
        }
    }

    private void InitializeScrollRects()
    {
        if (scrollRectA == null || scrollRectB == null)
        {
            Debug.LogError("ScrollRects are not assigned!");
            return;
        }

        rectA = scrollRectA.GetComponent<RectTransform>();
        rectB = scrollRectB.GetComponent<RectTransform>();

        screenWidth = GetComponent<RectTransform>().rect.width;

        // Set the initial scale for rectA and rectB
        rectA.localScale = Vector3.one * maxScale;  // Active initially
        rectB.localScale = Vector3.one * minScale;  // Inactive initially

        // Set the anchored positions
        rectA.anchoredPosition = Vector2.zero;  // Active, so at the center
        rectB.anchoredPosition = new Vector2(screenWidth, 0);  // Inactive, off the screen
    }

    private void OnTransitionButtonClick()
    {
        if (isTransitioning)
        {
            if (transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine);
            }
        }

        // Start a new transition coroutine
        transitionCoroutine = StartCoroutine(TransitionScrollRects());
    }

    private IEnumerator TransitionScrollRects()
    {
        isTransitioning = true;

        // Determine the active and inactive RectTransforms
        RectTransform fromRect = isAActive ? rectA : rectB;
        RectTransform toRect = isAActive ? rectB : rectA;

        // Get start positions and calculate end positions
        Vector2 fromStartPosition = fromRect.anchoredPosition;
        Vector2 toStartPosition = toRect.anchoredPosition;

        Vector2 fromEndPosition = new Vector2(isAActive ? -screenWidth : screenWidth, 0);  // Move off-screen
        Vector2 toEndPosition = Vector2.zero;  // Move to center of the screen

        float elapsedTime = 0f;

        // Perform the transition smoothly over time
        while (elapsedTime < 1f)
        {
            elapsedTime += Time.deltaTime * transitionSpeed;
            float t = Mathf.SmoothStep(0f, 1f, elapsedTime);  // SmoothStep for easing

            // Update the anchored positions
            fromRect.anchoredPosition = Vector2.Lerp(fromStartPosition, fromEndPosition, t);
            toRect.anchoredPosition = Vector2.Lerp(toStartPosition, toEndPosition, t);

            // Update the scales
            fromRect.localScale = Vector3.Lerp(Vector3.one * maxScale, Vector3.one * minScale, t);
            toRect.localScale = Vector3.Lerp(Vector3.one * minScale, Vector3.one * maxScale, t);

            yield return null;  // Wait until the next frame
        }

        // Ensure the final position and scale are correctly set
        fromRect.anchoredPosition = fromEndPosition;
        toRect.anchoredPosition = toEndPosition;

        fromRect.localScale = Vector3.one * minScale;  // Make the previous rect inactive
        toRect.localScale = Vector3.one * maxScale;  // Make the new rect active

        // Toggle the active rect
        isAActive = !isAActive;

        isTransitioning = false;
        transitionCoroutine = null;  // Clear the coroutine reference
    }
}
