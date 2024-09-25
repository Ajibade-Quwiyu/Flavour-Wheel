using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class TableArrangement : MonoBehaviour
{
    public List<ScrollRect> scrollRects;

    private List<RectTransform> contents = new List<RectTransform>();
    private List<RectTransform> rects = new List<RectTransform>();
    private float screenWidth;

    private void Start()
    {
        InitializeScrollRects();
    }

    private void InitializeScrollRects()
    {
        if (scrollRects == null || scrollRects.Count == 0)
        {
            Debug.LogError("ScrollRects are not assigned!");
            return;
        }

        screenWidth = GetComponent<RectTransform>().rect.width;

        // Setup each scroll rect
        for (int i = 0; i < scrollRects.Count; i++)
        {
            RectTransform rect = scrollRects[i].GetComponent<RectTransform>();
            RectTransform content = scrollRects[i].content;
            rects.Add(rect);
            contents.Add(content);

            SetupScrollRect(rect, content, i * screenWidth); // Arrange them on the screen
        }
    }

    private void SetupScrollRect(RectTransform rectTransform, RectTransform content, float xPosition)
    {
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(1, 1);
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.anchoredPosition = new Vector2(xPosition, 0);

        EnsureContentSizeFitter(content);
    }

    private void EnsureContentSizeFitter(RectTransform content)
    {
        if (content == null) return;

        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        }
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        VerticalLayoutGroup group = content.GetComponent<VerticalLayoutGroup>();
        if (group == null)
        {
            group = content.gameObject.AddComponent<VerticalLayoutGroup>();
        }
        group.childForceExpandHeight = false;
        group.childControlHeight = true;
    }
}
