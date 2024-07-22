using UnityEngine;
using UnityEngine.EventSystems;

public class RotationAndZoomController : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IScrollHandler
{
    private Vector2 lastDragPosition;
    private RectTransform rectTransform;
    private float initialPinchDistance;
    private Vector3 initialScale;

    [Tooltip("Rotation sensitivity factor")]
    public float rotationSensitivity = 0.1f;

    [Tooltip("Maximum zoom scale")]
    public float maxZoom = 2.0f;

    [Tooltip("Minimum zoom scale")]
    public float minZoom = 0.5f;

    [Tooltip("Zoom sensitivity factor")]
    public float zoomSensitivity = 0.1f;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        lastDragPosition = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 currentDragPosition = eventData.position;
        Vector2 dragDifference = currentDragPosition - lastDragPosition;

        float rotationAngle = dragDifference.x * rotationSensitivity;
        rectTransform.Rotate(Vector3.forward, -rotationAngle);

        lastDragPosition = currentDragPosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // You can add any functionality needed when drag ends
    }

    public void OnScroll(PointerEventData eventData)
    {
        float scrollDelta = eventData.scrollDelta.y * zoomSensitivity;
        float currentScale = rectTransform.localScale.x;
        float newScale = Mathf.Clamp(currentScale + scrollDelta, minZoom, maxZoom);

        rectTransform.localScale = new Vector3(newScale, newScale, newScale);
    }

    void Update()
    {
        // Handle pinch to zoom for mobile devices
        if (Input.touchCount == 2)
        {
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            if (touchZero.phase == TouchPhase.Began || touchOne.phase == TouchPhase.Began)
            {
                initialPinchDistance = Vector2.Distance(touchZero.position, touchOne.position);
                initialScale = rectTransform.localScale;
            }
            else if (touchZero.phase == TouchPhase.Moved || touchOne.phase == TouchPhase.Moved)
            {
                float currentPinchDistance = Vector2.Distance(touchZero.position, touchOne.position);
                float scaleFactor = currentPinchDistance / initialPinchDistance;

                float newScale = Mathf.Clamp(initialScale.x * scaleFactor, minZoom, maxZoom);
                rectTransform.localScale = new Vector3(newScale, newScale, newScale);
            }
        }
    }
}
