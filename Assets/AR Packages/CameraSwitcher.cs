using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class CameraSwitcher : MonoBehaviour
{
    [Header("Cameras")]
    public Camera normalCamera;
    public Camera arCamera;

    [Header("Input Settings")]
    public KeyCode switchKey = KeyCode.Tab;

    // Private AR components
    private ARSession arSession;
    private ARCameraManager arCameraManager;
    private ARPlaneManager arPlaneManager;
    private ARAnchorManager arAnchorManager;
    private ARRaycastManager arRaycastManager;
    private ARSessionOrigin arSessionOrigin;

    private bool isInARMode = true;
    private const string MAIN_CAMERA_TAG = "MainCamera";

    void Start()
    {
        if (!FindARComponents())
        {
            Debug.LogError("Failed to initialize AR components.");
            enabled = false;
            return;
        }

        if (arCamera == null || normalCamera == null)
        {
            Debug.LogError("One or both cameras are missing!");
            enabled = false;
            return;
        }

        SetARMode(true);
    }

    bool FindARComponents()
    {
        arSessionOrigin = FindObjectOfType<ARSessionOrigin>();
        arSession = FindObjectOfType<ARSession>();
        arCameraManager = FindObjectOfType<ARCameraManager>();
        arPlaneManager = FindObjectOfType<ARPlaneManager>();
        arAnchorManager = FindObjectOfType<ARAnchorManager>();
        arRaycastManager = FindObjectOfType<ARRaycastManager>();

        // Return false if any essential AR component is missing
        return arSessionOrigin != null && arSession != null &&
               arCameraManager != null && arPlaneManager != null &&
               arAnchorManager != null && arRaycastManager != null;
    }

    void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(switchKey))
        {
            ToggleCamera();
        }
#else
        if (Input.touchCount == 2 && Input.GetTouch(1).phase == TouchPhase.Began)
        {
            ToggleCamera();
        }
#endif
    }

    public void ToggleCamera()
    {
        SetARMode(!isInARMode);
    }

    void SetARMode(bool enableAR)
    {
        isInARMode = enableAR;

        // Switch cameras and tags
        arCamera.gameObject.SetActive(enableAR);
        arCamera.tag = enableAR ? MAIN_CAMERA_TAG : "Untagged";
        normalCamera.gameObject.SetActive(!enableAR);
        normalCamera.tag = enableAR ? "Untagged" : MAIN_CAMERA_TAG;

        // Manage AR Session Origin
        arSessionOrigin.gameObject.SetActive(enableAR);

        // Manage AR Session
        arSession.enabled = enableAR;
        if (enableAR)
        {
            arSession.Reset();
        }

        // Enable/disable AR managers
        arCameraManager.enabled = enableAR;
        arPlaneManager.enabled = enableAR;
        arAnchorManager.enabled = enableAR;
        arRaycastManager.enabled = enableAR;

        if (!enableAR)
        {
            foreach (var plane in arPlaneManager.trackables)
            {
                plane.gameObject.SetActive(false);
            }
        }
    }

    public void SwitchToAR()
    {
        SetARMode(true);
    }

    public void SwitchToNormal()
    {
        SetARMode(false);
    }

    // Helper method to get current active camera
    public Camera GetActiveCamera()
    {
        return isInARMode ? arCamera : normalCamera;
    }
}
