using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class ARController : MonoBehaviour
{
    [Header("Cameras")]
    public Camera normalCamera;
    public Camera arCamera;

    [Header("UI Elements")]
    public GameObject placementIndicator;
    public RectTransform objectToPlace;
    public RectTransform canvasWheel;

    public KeyCode switchKey = KeyCode.Tab;

    [Header("Events")]
    public UnityEvent OnARMode;
    public UnityEvent OnNormalMode;
    public UnityEvent OnSurfaceDetected;
    public UnityEvent OnObjectPlaced;

    private UserInputManager userInputManager;
    private ARSession arSession;
    private ARPlaneManager planeManager;
    private ARRaycastManager raycastManager;
    private ARAnchorManager anchorManager;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private bool isInARMode = false;
    private bool isPlaced = false;
    private Pose placementPose;
    private Transform placementTransform;
    private bool placementPoseIsValid = false;
    private GameObject placementRef;
    private ARAnchor currentAnchor;
    private Coroutine arModeCoroutine;
    private const string MAIN_CAMERA_TAG = "MainCamera";

    void Awake()
    {
        InitializeARComponents();
    }

    void InitializeARComponents()
    {
        arSession = FindObjectOfType<ARSession>();
        planeManager = FindObjectOfType<ARPlaneManager>();
        raycastManager = FindObjectOfType<ARRaycastManager>();
        anchorManager = FindObjectOfType<ARAnchorManager>();
        userInputManager = FindObjectOfType<UserInputManager>();
    }

    private void CanvasController(bool isAR)
    {
        Canvas canvas = objectToPlace.GetComponent<Canvas>();
        CanvasScaler scaler = objectToPlace.GetComponent<CanvasScaler>();
        GraphicRaycaster raycaster = objectToPlace.GetComponent<GraphicRaycaster>();

        if (isAR)
        {
            objectToPlace.SetParent(null);
            if (canvas == null) canvas = objectToPlace.gameObject.AddComponent<Canvas>();
            if (scaler == null) scaler = objectToPlace.gameObject.AddComponent<CanvasScaler>();
            if (raycaster == null) raycaster = objectToPlace.gameObject.AddComponent<GraphicRaycaster>();

            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = arCamera;
            objectToPlace.localScale = new Vector3(0.0006f, 0.0006f, 0.0006f);
            objectToPlace.localRotation = Quaternion.Euler(-90f, 0f, 180f);
        }
        else
        {
            if (raycaster != null) Destroy(raycaster);
            if (scaler != null) Destroy(scaler);
            if (canvas != null) Destroy(canvas);

            objectToPlace.SetParent(canvasWheel.transform, false);
            objectToPlace.localPosition = Vector3.zero;
            objectToPlace.localScale = Vector3.one;
            objectToPlace.localRotation = Quaternion.identity;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(switchKey))
            ToggleCamera();

        if (!isInARMode) return;

        if (!isPlaced)
        {
            UpdatePlacementPose();
            UpdatePlacementIndicator();

            if (placementPoseIsValid && Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
                PlaceObject();
        }
    }

    void UpdatePlacementPose()
    {
        if (isPlaced) return;

        var screenCenter = Camera.main.ViewportToScreenPoint(new Vector3(0.5f, 0.5f));
        if (raycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon))
        {
            placementPoseIsValid = hits.Count > 0;
            if (placementPoseIsValid)
            {
                if (!placementIndicator.activeInHierarchy)
                {
                    OnSurfaceDetected?.Invoke();
                }

                placementPose = hits[0].pose;
                ARPlane plane = planeManager.GetPlane(hits[0].trackableId);
                placementTransform = plane.transform;

                Vector3 forward = arCamera.transform.position - placementPose.position;
                forward.y = 0;
                placementPose.rotation = Quaternion.LookRotation(forward) * Quaternion.Euler(-90, 0, 0);
            }
        }
    }

    void UpdatePlacementIndicator()
    {
        placementIndicator.SetActive(placementPoseIsValid);
        if (placementPoseIsValid)
            placementIndicator.transform.SetPositionAndRotation(placementPose.position, placementPose.rotation);
    }

    void PlaceObject()
{
    if (placementRef == null)
    {
        placementRef = new GameObject("PlacementRef");
    }

    ARPlane plane = planeManager.GetPlane(hits[0].trackableId);
    currentAnchor = anchorManager.AttachAnchor(plane, placementPose);

    if (currentAnchor != null)
    {
        placementRef.transform.parent = currentAnchor.transform;
        placementRef.transform.SetPositionAndRotation(placementPose.position, placementPose.rotation);

        objectToPlace.gameObject.SetActive(true);
        objectToPlace.SetParent(placementRef.transform);
        objectToPlace.localPosition = Vector3.zero;
        objectToPlace.localRotation = Quaternion.Euler(-90f, 0f, 180f);

        // Disable all detected planes after successful placement
        foreach (var detectedPlane in planeManager.trackables)
        {
            detectedPlane.gameObject.SetActive(false);
        }

        OnObjectPlaced?.Invoke();
    }

    isPlaced = true;
    placementIndicator.SetActive(false);
}

    public void ToggleCamera()
    {
        if (arModeCoroutine != null)
        {
            StopCoroutine(arModeCoroutine);
        }
        arModeCoroutine = StartCoroutine(SetARModeWhenReady(!isInARMode));
    }

    private IEnumerator SetARModeWhenReady(bool enableAR)
    {
        float timeout = 30f;
        float elapsedTime = 0f;

        while (!userInputManager.isGameActive && elapsedTime < timeout)
        {
            yield return new WaitForSeconds(0.5f);
            elapsedTime += 0.5f;
        }

        if (elapsedTime >= timeout || !userInputManager.isGameActive)
        {
            yield break;
        }

        if (enableAR == isInARMode) yield break;

        isInARMode = enableAR;
        arCamera.gameObject.SetActive(enableAR);
        arCamera.tag = enableAR ? MAIN_CAMERA_TAG : "Untagged";
        normalCamera.gameObject.SetActive(!enableAR);
        normalCamera.tag = enableAR ? "Untagged" : MAIN_CAMERA_TAG;

        if (!enableAR)
        {
            CleanupAR();
        }
        else
        {
            RestartPlacement();
        }

        CanvasController(enableAR);
        EnableARComponents(enableAR);

        if (enableAR)
            OnARMode?.Invoke();
        else
            OnNormalMode?.Invoke();
    }

    public void SetARMode(bool enableAR)
    {
        if (arModeCoroutine != null)
        {
            StopCoroutine(arModeCoroutine);
        }
        arModeCoroutine = StartCoroutine(SetARModeWhenReady(enableAR));
    }

    private void RestartPlacement()
    {
        isPlaced = false;
        placementPoseIsValid = false;
        placementIndicator.SetActive(true);
        objectToPlace.gameObject.SetActive(false);

        if (currentAnchor != null)
        {
            Destroy(currentAnchor.gameObject);
            currentAnchor = null;
        }

        if (placementRef != null)
        {
            Destroy(placementRef);
            placementRef = null;
        }
    }

    private void EnableARComponents(bool enable)
    {
        if (enable)
        {
            arSession.enabled = true;
            StartCoroutine(DelayedSessionReset());
        }
        else
        {
            arSession.enabled = false;
        }
    }

    private IEnumerator DelayedSessionReset()
    {
        yield return null;
        if (arSession.enabled) arSession.Reset();
    }

    void CleanupAR()
    {
        placementIndicator.SetActive(false);

        if (currentAnchor != null)
        {
            Destroy(currentAnchor.gameObject);
            currentAnchor = null;
        }

        if (placementRef != null)
        {
            Destroy(placementRef);
            placementRef = null;
        }

        foreach (var plane in planeManager.trackables)
            plane.gameObject.SetActive(false);

        isPlaced = false;
        placementPoseIsValid = false;
    }

    void OnDestroy()
    {
        if (currentAnchor != null)
            Destroy(currentAnchor.gameObject);

        if (placementRef != null)
            Destroy(placementRef);
    }
}