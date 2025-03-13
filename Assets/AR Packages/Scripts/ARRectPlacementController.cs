using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;

public class ARRectPlacementController : MonoBehaviour
{
    [Header("AR Components")]
    public Camera arCamera;
    public GameObject placementIndicator;
    public RectTransform objectToPlace;

    [Header("Placement Settings")]
    public float minScale = 0.5f;
    public float maxScale = 2.0f;
    public float moveSpeed = 0.5f;
    public float scaleSpeed = 0.01f;

    private ARRaycastManager raycastManager;
    private ARPlaneManager planeManager;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private GameObject anchorPoint;
    private bool isPlaced = false;
    private Vector2 lastTouchPosition;

    void Awake()
    {
        raycastManager = GetComponent<ARRaycastManager>();
        planeManager = GetComponent<ARPlaneManager>();

        if (arCamera == null)
            arCamera = Camera.main;

        planeManager.requestedDetectionMode = PlaneDetectionMode.Vertical;

        // Initial setup
        if (placementIndicator != null)
            placementIndicator.SetActive(true);
        if (objectToPlace != null)
            objectToPlace.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!isPlaced)
        {
            UpdatePlacementIndicator();
            CheckForPlacement();
        }
        else
        {
            HandleInteraction();
        }
    }

    void UpdatePlacementIndicator()
    {
        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        if (raycastManager.Raycast(screenCenter, hits, TrackableType.Planes))
        {
            foreach (var hit in hits)
            {
                ARPlane plane = planeManager.GetPlane(hit.trackableId);
                if (plane != null && IsVerticalPlane(plane))
                {
                    placementIndicator.SetActive(true);
                    placementIndicator.transform.position = hit.pose.position;
                    return;
                }
            }
        }
    }

    void CheckForPlacement()
    {
        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        if (raycastManager.Raycast(screenCenter, hits, TrackableType.Planes))
        {
            foreach (var hit in hits)
            {
                ARPlane plane = planeManager.GetPlane(hit.trackableId);
                if (plane != null && IsVerticalPlane(plane))
                {
                    PlaceObject(hit);
                    break;
                }
            }
        }
    }

    bool IsVerticalPlane(ARPlane plane)
    {
        Vector3 planeNormal = plane.normal;
        float angle = Vector3.Angle(planeNormal, Vector3.up);
        return angle > 85f && angle < 95f;
    }

    void PlaceObject(ARRaycastHit hit)
    {
        if (isPlaced) return;

        // Create anchor point
        anchorPoint = new GameObject("AnchorPoint");
        anchorPoint.transform.position = hit.pose.position;

        // Setup object - only position, maintain original rotation
        objectToPlace.gameObject.SetActive(true);
        objectToPlace.transform.SetParent(anchorPoint.transform);
        objectToPlace.transform.position = hit.pose.position;

        // Update state
        isPlaced = true;

        // Hide indicator
        if (placementIndicator != null)
            placementIndicator.SetActive(false);
    }

    void HandleInteraction()
    {
        if (Input.touchCount == 1)
        {
            // Handle movement
            Touch touch = Input.GetTouch(0);
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    lastTouchPosition = touch.position;
                    break;

                case TouchPhase.Moved:
                    Vector2 delta = touch.position - lastTouchPosition;
                    lastTouchPosition = touch.position;

                    Vector3 moveDirection = new Vector3(delta.x, delta.y, 0) * moveSpeed * Time.deltaTime;
                    anchorPoint.transform.position += moveDirection;
                    break;
            }
        }
        else if (Input.touchCount == 2)
        {
            // Handle scaling
            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);

            // Get the positions of touches from previous frame
            Vector2 touch0PrevPos = touch0.position - touch0.deltaPosition;
            Vector2 touch1PrevPos = touch1.position - touch1.deltaPosition;

            // Find the magnitude of the vector between the touches in each frame
            float prevTouchDeltaMag = (touch0PrevPos - touch1PrevPos).magnitude;
            float touchDeltaMag = (touch0.position - touch1.position).magnitude;

            // Find the difference in the distances between frames
            float deltaMagnitudeDiff = touchDeltaMag - prevTouchDeltaMag;

            // Get current scale and adjust it
            Vector3 currentScale = objectToPlace.localScale;
            float scaleFactor = deltaMagnitudeDiff * scaleSpeed;

            // Apply scale change with clamping
            Vector3 newScale = currentScale + Vector3.one * scaleFactor;
            newScale.x = Mathf.Clamp(newScale.x, minScale, maxScale);
            newScale.y = Mathf.Clamp(newScale.y, minScale, maxScale);
            newScale.z = Mathf.Clamp(newScale.z, minScale, maxScale);

            objectToPlace.localScale = newScale;
        }
    }

    void OnDisable()
    {
        if (anchorPoint != null)
            Destroy(anchorPoint);
    }
}