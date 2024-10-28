using UnityEngine;
using UnityEngine.Events;

public class ARClickUI : MonoBehaviour
{
    [Header("UI Elements")]
    public RectTransform[] mainPanels;
    public RectTransform[] pointerGraphics;
    public RectTransform[] indicatorDots;

    [Header("Settings")]
    public Camera ARCamera;
    public float transitionSpeed = 5f;
    public float worldSpaceSpacing = 0.5f;

    [Header("Visual Settings")]
    public Vector3 activeScale = new Vector3(1.2f, 1.2f, 1.2f);
    public Vector3 inactiveScale = new Vector3(.75f, .75f, .75f);

    [System.Serializable] public class LastImageEvent : UnityEvent { }
    public LastImageEvent onLastImageSelected;

    private int currentIndex = 0;
    private BoxCollider2D[] pointerColliders;

    void Start()
    {
        if (ARCamera == null)
        {
            ARCamera = GameObject.FindObjectOfType<UnityEngine.XR.ARFoundation.ARCameraManager>()?.GetComponent<Camera>();
            if (ARCamera == null)
            {
                Debug.LogError("AR Camera not found!");
                return;
            }
        }

        InitializeState();
        SetupColliders();
        PositionElements();
    }

    private void InitializeState()
    {
        for (int i = 0; i < mainPanels.Length; i++)
        {
            mainPanels[i].gameObject.SetActive(i == 0);
        }
        UpdateVisuals();
    }

    private void SetupColliders()
    {
        foreach (var graphic in pointerGraphics)
        {
            var existingColliders = graphic.GetComponents<BoxCollider2D>();
            foreach (var col in existingColliders)
            {
                Destroy(col);
            }
        }

        pointerColliders = new BoxCollider2D[pointerGraphics.Length];
        for (int i = 0; i < pointerGraphics.Length; i++)
        {
            RectTransform rectTransform = pointerGraphics[i];

            BoxCollider2D collider = pointerGraphics[i].gameObject.AddComponent<BoxCollider2D>();
            pointerColliders[i] = collider;

            // Set size based on RectTransform
            collider.size = rectTransform.rect.size;
            collider.offset = Vector2.zero;
            collider.isTrigger = false;

            // Add Rigidbody2D to ensure collider works properly
            Rigidbody2D rb = pointerGraphics[i].gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.simulated = true;

            // Set layer to UI
            pointerGraphics[i].gameObject.layer = LayerMask.NameToLayer("UI");
        }
    }

    void Update()
    {
#if UNITY_EDITOR
            // Editor input handling
            if (Input.GetMouseButtonDown(0))
            {
                HandleRaycast(Input.mousePosition);
            }
#else
        // Mobile input handling
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            HandleRaycast(Input.GetTouch(0).position);
        }
#endif
    }

    private void HandleRaycast(Vector2 screenPosition)
    {
        Ray ray = ARCamera.ScreenPointToRay(screenPosition);
        RaycastHit2D[] hits = Physics2D.GetRayIntersectionAll(ray);

        if (hits.Length > 0)
        {
            foreach (RaycastHit2D hit in hits)
            {
                for (int i = 0; i < pointerGraphics.Length; i++)
                {
                    if (hit.collider.gameObject == pointerGraphics[i].gameObject)
                    {
                        SelectIndex(i);
                        return;
                    }
                }
            }
        }
        else
        {
            Debug.Log("No hits detected. Screen Position: " + screenPosition);
        }
    }

    private void SelectIndex(int newIndex)
    {
        if (newIndex == currentIndex) return;

        // Enable new panel before starting transition
        mainPanels[newIndex].gameObject.SetActive(true);

        if (newIndex == pointerGraphics.Length - 1)
        {
            onLastImageSelected?.Invoke();
        }

        currentIndex = newIndex;
        UpdateVisuals();
        PositionElements();
    }

    private void PositionElements()
    {
        // Position pointer graphics (moving selected to x=0)
        for (int i = 0; i < pointerGraphics.Length; i++)
        {
            float xOffset = (i - currentIndex) * worldSpaceSpacing;
            Vector3 currentPos = pointerGraphics[i].position;
            Vector3 targetPosition = new Vector3(xOffset, currentPos.y, currentPos.z);
            StartCoroutine(MoveToPosition(pointerGraphics[i], targetPosition));
        }

        // Position main panels
        for (int i = 0; i < mainPanels.Length; i++)
        {
            if (mainPanels[i].gameObject.activeSelf)
            {
                float xOffset = (i - currentIndex) * worldSpaceSpacing;
                Vector3 currentPos = mainPanels[i].position;
                Vector3 targetPosition = new Vector3(xOffset, currentPos.y, currentPos.z);
                StartCoroutine(MoveToPosition(mainPanels[i], targetPosition));

                if (i != currentIndex)
                {
                    // Wait for panel to move out before disabling
                    StartCoroutine(DisablePanelAfterTransition(mainPanels[i], targetPosition));
                }
            }
        }
    }

    private System.Collections.IEnumerator DisablePanelAfterTransition(RectTransform panel, Vector3 targetPosition)
    {
        // Wait for panel to reach target position
        float elapsed = 0;
        Vector3 startPosition = panel.position;

        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * transitionSpeed;
            float newX = Mathf.Lerp(startPosition.x, targetPosition.x, elapsed);
            panel.position = new Vector3(newX, panel.position.y, panel.position.z);
            yield return null;
        }

        // Ensure final position before disabling
        panel.position = new Vector3(targetPosition.x, panel.position.y, panel.position.z);
        panel.gameObject.SetActive(false);
    }

    private System.Collections.IEnumerator MoveToPosition(RectTransform rect, Vector3 targetPosition)
    {
        Vector3 startPosition = rect.position;
        float elapsed = 0;

        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * transitionSpeed;
            float newX = Mathf.Lerp(startPosition.x, targetPosition.x, elapsed);
            rect.position = new Vector3(newX, rect.position.y, rect.position.z);
            yield return null;
        }

        rect.position = new Vector3(targetPosition.x, rect.position.y, rect.position.z);
    }

    private void UpdateVisuals()
    {
        // Update pointer graphics
        for (int i = 0; i < pointerGraphics.Length; i++)
        {
            var canvasGroup = pointerGraphics[i].GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = pointerGraphics[i].gameObject.AddComponent<CanvasGroup>();

            canvasGroup.alpha = i == currentIndex ? 1f : 0.5f;
            StartCoroutine(ScaleTo(pointerGraphics[i], i == currentIndex ? activeScale : inactiveScale));
        }

        // Update indicator dots
        for (int i = 0; i < indicatorDots.Length; i++)
        {
            var canvasGroup = indicatorDots[i].GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = indicatorDots[i].gameObject.AddComponent<CanvasGroup>();

            int correspondingPointerIndex = Mathf.RoundToInt((float)i / (indicatorDots.Length - 1) * (pointerGraphics.Length - 1));
            canvasGroup.alpha = (correspondingPointerIndex == currentIndex) ? 1f : 0.5f;
        }
    }

    private System.Collections.IEnumerator ScaleTo(RectTransform rect, Vector3 targetScale)
    {
        Vector3 startScale = rect.localScale;
        float elapsed = 0;

        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * transitionSpeed;
            rect.localScale = Vector3.Lerp(startScale, targetScale, elapsed);
            yield return null;
        }

        rect.localScale = targetScale;
    }
}