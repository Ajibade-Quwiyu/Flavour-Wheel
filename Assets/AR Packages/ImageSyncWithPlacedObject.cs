using UnityEngine;

public class ImageSyncWithPlacedObject : MonoBehaviour
{
    [Header("References")]
    public PlaceObjectInFrontOfCamera placementScript;
    public RectTransform targetRectTransform;

    [Header("Settings")]
    public Vector3 positionOffset = Vector3.zero;
    public Vector3 rotationOffset = Vector3.zero;

    [Header("Debug")]
    public bool debugMode = true;

    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;

        if (debugMode)
        {
            Debug.Log($"[RectSync] Starting with RectTransform: {(targetRectTransform != null ? "Found" : "Missing")}");
            if (targetRectTransform != null)
            {
                Debug.Log($"[RectSync] Initial RectTransform position: {targetRectTransform.position}");
            }
        }
    }

    void Update()
    {
        UpdatePosition();
    }

    void UpdatePosition()
    {
        if (placementScript != null)
        {
            GameObject placedObject = placementScript.GetPlacedObject();
            if (placedObject != null && targetRectTransform != null)
            {
                // Update position and rotation
                targetRectTransform.position = placedObject.transform.position + positionOffset;

                // Make RectTransform face camera
                Vector3 directionToCamera = mainCamera.transform.position - targetRectTransform.position;
                directionToCamera.y = 0; // Keep vertical alignment
                if (directionToCamera != Vector3.zero)
                {
                    targetRectTransform.rotation = Quaternion.LookRotation(-directionToCamera) * Quaternion.Euler(rotationOffset);
                }

                if (debugMode && Time.frameCount % 60 == 0) // Log every 60 frames
                {
                    Debug.Log($"[RectSync] Current Position: {targetRectTransform.position}");
                    Debug.Log($"[RectSync] Current Rotation: {targetRectTransform.rotation.eulerAngles}");
                }
            }
        }
    }
}