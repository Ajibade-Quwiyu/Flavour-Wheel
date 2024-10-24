using UnityEngine;
using UnityEngine.EventSystems;

public class ZoomAndPan : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    RectTransform imageRect;
    public RectTransform maskRect;

    private Vector2 originalSize;
    private Vector2 originalPosition;
    private Vector2 lastMousePosition;

    public float zoomSpeed = 0.01f;
    public float panSpeed = 1f;
    public float minZoom = 1f;
    public float maxZoom = 3f;

    public string targetTag = "ZoomTarget";

    private Vector2 touchStartPosition;
    private float lastPinchDistance = 0f;
    private Vector2 lastPinchCenter;

    void Start()
    {
        imageRect = this.GetComponent<RectTransform>();
        originalSize = imageRect.sizeDelta;
        originalPosition = imageRect.anchoredPosition;
    }

    void Update()
    {
        if (!this.gameObject.CompareTag(targetTag)) return;

        if (Input.touchCount == 2)
        {
            HandlePinchZoom();
        }
        else if (Input.GetAxis("Mouse ScrollWheel") != 0f)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            Vector2 mousePosition = Input.mousePosition;
            Zoom(scroll * zoomSpeed * 10f, mousePosition);
        }
    }

    private void HandlePinchZoom()
    {
        Touch touch0 = Input.GetTouch(0);
        Touch touch1 = Input.GetTouch(1);

        Vector2 touch0Pos = touch0.position;
        Vector2 touch1Pos = touch1.position;

        float currentPinchDistance = Vector2.Distance(touch0Pos, touch1Pos);
        Vector2 currentPinchCenter = (touch0Pos + touch1Pos) / 2f;

        if (touch0.phase == TouchPhase.Began || touch1.phase == TouchPhase.Began)
        {
            lastPinchDistance = currentPinchDistance;
            lastPinchCenter = currentPinchCenter;
        }
        else if (touch0.phase == TouchPhase.Moved || touch1.phase == TouchPhase.Moved)
        {
            float pinchDelta = currentPinchDistance - lastPinchDistance;
            Zoom(pinchDelta * zoomSpeed, currentPinchCenter);

            lastPinchDistance = currentPinchDistance;
            lastPinchCenter = currentPinchCenter;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!this.gameObject.CompareTag(targetTag)) return;
        lastMousePosition = eventData.position;
        touchStartPosition = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!this.gameObject.CompareTag(targetTag)) return;

        if (Input.touchCount == 1)
        {
            if (Vector2.Distance(eventData.position, touchStartPosition) > 10f)
            {
                Vector2 delta = ((Vector2)Input.mousePosition - lastMousePosition) * panSpeed;
                imageRect.anchoredPosition += delta;
                ClampToBounds();
            }
        }

        lastMousePosition = Input.mousePosition;
    }

    private void Zoom(float increment, Vector2 zoomCenter)
    {
        if (!this.gameObject.CompareTag(targetTag)) return;



        Vector3 oldScale = imageRect.localScale;
        Vector3 newScale = oldScale * (1 + increment);
        newScale.x = Mathf.Clamp(newScale.x, minZoom, maxZoom);
        newScale.y = Mathf.Clamp(newScale.y, minZoom, maxZoom);
        newScale.z = oldScale.z; // Preserve z-scale

        // Convert screen point to local point in rect
        Vector2 localZoomCenter;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(imageRect, zoomCenter, null, out localZoomCenter);

        // Calculate zoom pivot in normalized space
        Vector2 normalizedPivot = Rect.PointToNormalized(imageRect.rect, localZoomCenter);

        // Apply zoom
        imageRect.localScale = newScale;

        // Calculate new position to maintain zoom center
        Vector2 newLocalZoomCenter = Rect.NormalizedToPoint(imageRect.rect, normalizedPivot);
        Vector2 deltaPivot = newLocalZoomCenter - localZoomCenter;
        Vector2 deltaPosition = new Vector2(deltaPivot.x * newScale.x, deltaPivot.y * newScale.y);

        imageRect.anchoredPosition -= deltaPosition;

        // If zooming in and close to minZoom, gradually move towards center
        if (increment < 0 && newScale.x < minZoom + 0.1f)
        {
            float t = (minZoom + 0.1f - newScale.x) / 0.1f; // Transition factor
            imageRect.anchoredPosition = Vector2.Lerp(imageRect.anchoredPosition, originalPosition, t);
        }

        ClampToBounds();
    }

    private void ClampToBounds()
    {
        if (!this.gameObject.CompareTag(targetTag)) return;

        Vector2 position = imageRect.anchoredPosition;

        float imageWidth = imageRect.rect.width * imageRect.localScale.x;
        float imageHeight = imageRect.rect.height * imageRect.localScale.y;

        float maskWidth = maskRect.rect.width;
        float maskHeight = maskRect.rect.height;

        float minX = (maskWidth - imageWidth) / 2;
        float maxX = -minX;
        float minY = (maskHeight - imageHeight) / 2;
        float maxY = -minY;

        if (imageWidth > maskWidth)
        {
            position.x = Mathf.Clamp(position.x, minX, maxX);
        }
        else
        {
            position.x = 0;  // Center horizontally if smaller than mask
        }

        if (imageHeight > maskHeight)
        {
            position.y = Mathf.Clamp(position.y, minY, maxY);
        }
        else
        {
            position.y = 0;  // Center vertically if smaller than mask
        }

        imageRect.anchoredPosition = position;
    }
}