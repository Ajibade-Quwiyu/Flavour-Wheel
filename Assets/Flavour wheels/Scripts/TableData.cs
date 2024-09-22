using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TableData : MonoBehaviour
{
    public ScrollRect scrollRectA;
    public ScrollRect scrollRectB;
    public Button transitionButton;
    public float maxScale = 1.0f;
    public float minScale = 0.4f;
    public float transitionSpeed = 5f;

    private RectTransform contentA;
    private RectTransform contentB;
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
        contentA = scrollRectA.content;
        contentB = scrollRectB.content;

        screenWidth = GetComponent<RectTransform>().rect.width;

        SetupScrollRect(rectA, contentA, 0);
        SetupScrollRect(rectB, contentB, screenWidth);

        rectA.localScale = Vector3.one * maxScale;
        rectB.localScale = Vector3.one * minScale;
    }

    private void SetupScrollRect(RectTransform rectTransform, RectTransform content, float xPosition)
    {
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(1, 1);
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.anchoredPosition = new Vector2(xPosition, 0);

        EnsureContentSizeFitter(content);
        EnsureVerticalLayoutGroup(content);
    }

    private void EnsureContentSizeFitter(RectTransform content)
    {
        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        }
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private void EnsureVerticalLayoutGroup(RectTransform content)
    {
        VerticalLayoutGroup group = content.GetComponent<VerticalLayoutGroup>();
        if (group == null)
        {
            group = content.gameObject.AddComponent<VerticalLayoutGroup>();
        }
        group.childForceExpandHeight = false;
        group.childControlHeight = true;
    }

    private void OnTransitionButtonClick()
    {
        if (isTransitioning)
        {
            // Stop the current transition if it's still running
            if (transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine);
            }
        }

        // Start a new transition
        transitionCoroutine = StartCoroutine(TransitionScrollRects());
    }

    private IEnumerator TransitionScrollRects()
    {
        isTransitioning = true;

        RectTransform fromRect = isAActive ? rectA : rectB;
        RectTransform toRect = isAActive ? rectB : rectA;

        Vector2 fromStartPosition = fromRect.anchoredPosition;
        Vector2 toStartPosition = toRect.anchoredPosition;

        Vector2 fromEndPosition = new Vector2(isAActive ? -screenWidth : screenWidth, 0);
        Vector2 toEndPosition = Vector2.zero;

        float elapsedTime = 0f;
        while (elapsedTime < 1f)
        {
            elapsedTime += Time.deltaTime * transitionSpeed;
            float t = Mathf.SmoothStep(0f, 1f, elapsedTime);

            fromRect.anchoredPosition = Vector2.Lerp(fromStartPosition, fromEndPosition, t);
            toRect.anchoredPosition = Vector2.Lerp(toStartPosition, toEndPosition, t);

            fromRect.localScale = Vector3.Lerp(Vector3.one * maxScale, Vector3.one * minScale, t);
            toRect.localScale = Vector3.Lerp(Vector3.one * minScale, Vector3.one * maxScale, t);

            yield return null;
        }

        fromRect.anchoredPosition = fromEndPosition;
        toRect.anchoredPosition = toEndPosition;

        fromRect.localScale = Vector3.one * minScale;
        toRect.localScale = Vector3.one * maxScale;

        isAActive = !isAActive;
        isTransitioning = false;
        transitionCoroutine = null; // Clear the coroutine reference when done
    }
}
