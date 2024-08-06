using UnityEngine;
using UnityEngine.UI;

public class TableData : MonoBehaviour
{
    public ScrollRect scrollView;
    public RectTransform contentRect;
    public GameObject rowPrefab;

    private void Start()
    {
        // Ensure the ScrollRect component is assigned
        if (scrollView == null)
        {
            Debug.LogError("ScrollRect not assigned to TableData script!");
            return;
        }

        // Get the content RectTransform
        contentRect = scrollView.content;

        // Ensure Content Size Fitter is attached and configured
        ContentSizeFitter contentSizeFitter = contentRect.GetComponent<ContentSizeFitter>();
        if (contentSizeFitter == null)
        {
            contentSizeFitter = contentRect.gameObject.AddComponent<ContentSizeFitter>();
        }
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Ensure Vertical Layout Group is attached and configured
        VerticalLayoutGroup verticalLayoutGroup = contentRect.GetComponent<VerticalLayoutGroup>();
        if (verticalLayoutGroup == null)
        {
            verticalLayoutGroup = contentRect.gameObject.AddComponent<VerticalLayoutGroup>();
        }
        verticalLayoutGroup.childForceExpandHeight = false;
        verticalLayoutGroup.childControlHeight = true;
    }

}