using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;

public class PlaceObjectInFrontOfCamera : MonoBehaviour
{
    [Header("AR Setup")]
    public GameObject objectToPlace;
    public Camera arCamera;

    [Header("Placement Settings")]
    public float minDistanceFromWall = 0.05f; // Small offset from wall
    public float moveSpeed = 0.5f;

    private ARRaycastManager raycastManager;
    private ARPlaneManager planeManager;
    private ARAnchorManager anchorManager;
    private GameObject placedObject;
    private ARAnchor currentAnchor;
    private ARPlane currentPlane;
    private bool isPlaced = false;
    private bool isDragging = false;
    private Vector2 lastTouchPosition;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

    void Awake()
    {
        raycastManager = GetComponent<ARRaycastManager>();
        planeManager = GetComponent<ARPlaneManager>();
        anchorManager = GetComponent<ARAnchorManager>();

        if (arCamera == null)
            arCamera = Camera.main;

        // Configure for vertical planes only
        planeManager.requestedDetectionMode = PlaneDetectionMode.Vertical;
    }

    void Update()
    {
        if (!isPlaced)
        {
            CheckForVerticalPlane();
        }
        else
        {
            HandleObjectMovement();
        }
    }
    public GameObject GetPlacedObject()
    {
        return placedObject;
    }
    void CheckForVerticalPlane()
    {
        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        if (raycastManager.Raycast(screenCenter, hits, TrackableType.Planes))
        {
            foreach (var hit in hits)
            {
                ARPlane plane = planeManager.GetPlane(hit.trackableId);
                if (plane != null && IsVerticalPlane(plane))
                {
                    PlaceObject(hit, plane);
                    break;
                }
            }
        }
    }

    bool IsVerticalPlane(ARPlane plane)
    {
        Vector3 planeNormal = plane.normal;
        float angle = Vector3.Angle(planeNormal, Vector3.up);
        return angle > 85f && angle < 95f; // Allow small deviation from perfectly vertical
    }

    void PlaceObject(ARRaycastHit hit, ARPlane plane)
    {
        if (placedObject != null) return;

        // Create object slightly in front of the plane using plane's normal
        Vector3 planeNormal = plane.normal;
        Vector3 position = hit.pose.position + (planeNormal * minDistanceFromWall);
        Quaternion rotation = Quaternion.LookRotation(-planeNormal); // Face away from wall

        placedObject = Instantiate(objectToPlace, position, rotation);

        // Create anchor
        Pose anchorPose = new Pose(position, rotation);
        currentAnchor = anchorManager.AttachAnchor(plane, anchorPose);

        if (currentAnchor != null)
        {
            placedObject.transform.parent = currentAnchor.transform;
            currentPlane = plane;
            isPlaced = true;
        }
    }

    void HandleObjectMovement()
    {
        if (Input.touchCount == 0 || currentPlane == null) return;

        Touch touch = Input.GetTouch(0);

        switch (touch.phase)
        {
            case TouchPhase.Began:
                isDragging = true;
                lastTouchPosition = touch.position;
                break;

            case TouchPhase.Moved:
                if (!isDragging) return;

                // Calculate movement delta
                Vector2 delta = touch.position - lastTouchPosition;
                lastTouchPosition = touch.position;

                // Move object along the plane
                Vector3 right = Vector3.ProjectOnPlane(currentPlane.transform.right, currentPlane.transform.up).normalized;
                Vector3 up = Vector3.ProjectOnPlane(currentPlane.transform.forward, currentPlane.transform.up).normalized;

                Vector3 moveDirection = (right * delta.x + up * delta.y) * moveSpeed * Time.deltaTime;
                Vector3 newPosition = placedObject.transform.position + moveDirection;

                // Ensure we stay on the plane
                Ray ray = new Ray(newPosition, -currentPlane.transform.up);
                Plane verticalPlane = new Plane(currentPlane.transform.up, currentPlane.transform.position);

                float enter;
                if (verticalPlane.Raycast(ray, out enter))
                {
                    Vector3 projectedPosition = ray.GetPoint(enter);
                    placedObject.transform.position = projectedPosition + (currentPlane.transform.up * minDistanceFromWall);
                }
                break;

            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                isDragging = false;
                break;
        }
    }

    void OnDisable()
    {
        if (currentAnchor != null)
            Destroy(currentAnchor.gameObject);

        if (placedObject != null)
            Destroy(placedObject);
    }
}