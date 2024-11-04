using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
public class PlaceObjectOnPlanes : MonoBehaviour
{
    public GameObject placementIndicator;
    public GameObject objectToPlace;
    
    Pose placementPose;
    Transform placementTransform;
    bool placementPoseIsValid = false;
    bool isDartBoardPlaced = false;
    TrackableId placePlaneId = TrackableId.invalidId;

    ARRaycastManager m_RaycastManager;
    static List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();

    public static event Action onPlaceDartBoard;
    void Awake()
    {
        if (TryGetComponent(out ARRaycastManager aRRaycast))
            m_RaycastManager = aRRaycast;
    }
    void Update()
    {
        if(!isDartBoardPlaced)
        {
            UpdatePlacementPosition();
            UpdatePlacementIndicator();

            if (placementPoseIsValid && Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                PlaceDartBoard();
            }
        }
    }

   

    void UpdatePlacementPosition()
    {
        if (isDartBoardPlaced)
            return;

        var screenCenter = Camera.main.ViewportToScreenPoint(new Vector3(0.5f, 0.5f));
        if (m_RaycastManager.Raycast(screenCenter, s_Hits,TrackableType.PlaneWithinPolygon))
        {
            placementPoseIsValid = s_Hits.Count > 0;

            if (placementPoseIsValid)
            {
                placementPose = s_Hits[0].pose;
                placePlaneId = s_Hits[0].trackableId;

                var planManager = GetComponent<ARPlaneManager>();
                ARPlane arPlane = planManager.GetPlane(placePlaneId);
                placementTransform = arPlane.transform;
            }
        }
    }

    void UpdatePlacementIndicator()
    {
        if(placementPoseIsValid)
        {
            placementIndicator.SetActive(true);
            placementIndicator.transform.SetPositionAndRotation(placementPose.position, placementTransform.rotation);
        }
        else
        {
            placementIndicator.SetActive(false);
        }
    }
    void PlaceDartBoard()
    {
        Instantiate(objectToPlace, placementPose.position, placementTransform.rotation);
        onPlaceDartBoard?.Invoke();
        isDartBoardPlaced = true;
        placementIndicator.SetActive(false);
    }

}
