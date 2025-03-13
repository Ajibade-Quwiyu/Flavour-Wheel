using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

[RequireComponent(typeof(ARPlaneManager))]
public class ARPlanceController : MonoBehaviour
{
    ARPlaneManager m_ARPlaneManager;
    void Awake()
    {
        //Get Plane Manager object
        m_ARPlaneManager = GetComponent<ARPlaneManager>();
    }

    private void OnEnable()
    {
        PlaceObjectOnPlanes.onPlaceDartBoard += DisablePlandectection;
    }

    private void OnDisable()
    {
        PlaceObjectOnPlanes.onPlaceDartBoard -= DisablePlandectection;
    }
    void DisablePlandectection()
    {
        
        SetAllPlanesActive(false);
        m_ARPlaneManager.enabled = !m_ARPlaneManager.enabled;   
    }

    void SetAllPlanesActive(bool value)
    {
        //disable all trackables points from detection view
        foreach(var plane in m_ARPlaneManager.trackables)
        {
            plane.gameObject.SetActive(value);
        }
    }

}
