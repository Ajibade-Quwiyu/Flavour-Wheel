using UnityEngine;
using UnityEngine.EventSystems;

public class RotationAndZoomController : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IScrollHandler
{
    private Vector2 lastDragPosition;
    private RectTransform rectTransform;
    private float initialPinchDistance;
    private Vector3 initialScale;
    private float lastRotationTime;

    [Tooltip("Rotation sensitivity factor")]
    public float rotationSensitivity = 0.1f;

    [Tooltip("Maximum zoom scale")]
    public float maxZoom = 2.0f;

    [Tooltip("Minimum zoom scale")]
    public float minZoom = 0.5f;

    [Tooltip("Zoom sensitivity factor")]
    public float zoomSensitivity = 0.1f;

    [Tooltip("Audio clip for rotation sound")]
    public AudioClip rotationSound;

    private AudioSource audioSource;
    private bool isDragging = false;
    private float rotationVelocity = 0f;
    private float decelerationRate = 0.95f; // Decrease this value to make it stop faster

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        audioSource = Camera.main.GetComponent<AudioSource>();

        if (audioSource == null)
        {
            Debug.LogError("Main Camera does not have an AudioSource component.");
        }

        lastRotationTime = Time.time;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        lastDragPosition = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 currentDragPosition = eventData.position;
        Vector2 dragDifference = currentDragPosition - lastDragPosition;

        float rotationAngle = dragDifference.x * rotationSensitivity;
        rectTransform.Rotate(Vector3.forward, -rotationAngle);

        rotationVelocity = rotationAngle;

        float timeSinceLastRotation = Time.time - lastRotationTime;
        if (timeSinceLastRotation > 0.1f / Mathf.Abs(rotationAngle))
        {
            PlayRotationSound();
            lastRotationTime = Time.time;
        }

        lastDragPosition = currentDragPosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
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

        // Apply momentum-based rotation when not dragging
        if (!isDragging)
        {
            if (Mathf.Abs(rotationVelocity) > 0.01f)
            {
                rectTransform.Rotate(Vector3.forward, -rotationVelocity);
                rotationVelocity *= decelerationRate;
            }
            else
            {
                rotationVelocity = 0f;
            }
        }

        // Stop rotation on touch
        if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            rotationVelocity = 0f;
        }
    }

    private void PlayRotationSound()
    {
        if (audioSource != null && rotationSound != null)
        {
            audioSource.PlayOneShot(rotationSound);
        }
    }
}
