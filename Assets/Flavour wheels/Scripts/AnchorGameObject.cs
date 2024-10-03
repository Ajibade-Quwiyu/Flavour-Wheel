using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class AnchorGameObject : MonoBehaviour
{
    public enum AnchorType
    {
        BottomLeft, BottomCenter, BottomRight,
        MiddleLeft, MiddleCenter, MiddleRight,
        TopLeft, TopCenter, TopRight,
    }

    public bool executeInUpdate;
    public AnchorType anchorType;
    public Vector3 anchorOffset;

    private IEnumerator updateAnchorRoutine;

    void Start()
    {
        updateAnchorRoutine = UpdateAnchorAsync();
        StartCoroutine(updateAnchorRoutine);
    }

    IEnumerator UpdateAnchorAsync()
    {
        uint cameraWaitCycles = 0;
        while (CameraViewportHandler.Instance == null)
        {
            ++cameraWaitCycles;
            yield return new WaitForEndOfFrame();
        }

        if (cameraWaitCycles > 0)
        {
            Debug.Log($"CameraAnchor found CameraFit instance after waiting {cameraWaitCycles} frame(s). " +
                      "You might want to check that CameraFit has an earlier execution order.");
        }

        UpdateAnchor();
        updateAnchorRoutine = null;
    }

    void UpdateAnchor()
    {
        Vector3 anchorPosition = GetAnchorPosition();
        SetAnchor(anchorPosition);
    }

    Vector3 GetAnchorPosition()
    {
        switch (anchorType)
        {
            case AnchorType.BottomLeft: return CameraViewportHandler.Instance.BottomLeft;
            case AnchorType.BottomCenter: return CameraViewportHandler.Instance.BottomCenter;
            case AnchorType.BottomRight: return CameraViewportHandler.Instance.BottomRight;
            case AnchorType.MiddleLeft: return CameraViewportHandler.Instance.MiddleLeft;
            case AnchorType.MiddleCenter: return CameraViewportHandler.Instance.MiddleCenter;
            case AnchorType.MiddleRight: return CameraViewportHandler.Instance.MiddleRight;
            case AnchorType.TopLeft: return CameraViewportHandler.Instance.TopLeft;
            case AnchorType.TopCenter: return CameraViewportHandler.Instance.TopCenter;
            case AnchorType.TopRight: return CameraViewportHandler.Instance.TopRight;
            default: return Vector3.zero;
        }
    }

    void SetAnchor(Vector3 anchor)
    {
        Vector3 newPos = anchor + anchorOffset;
        newPos.z = 0f; // Ensure z-position is always zero

        if (transform is RectTransform rectTransform)
        {
            if (rectTransform.anchoredPosition3D != newPos)
            {
                rectTransform.anchoredPosition3D = newPos;
            }
        }
        else
        {
            if (transform.position != newPos)
            {
                transform.position = newPos;
            }
        }
    }

#if UNITY_EDITOR
    void Update()
    {
        if (updateAnchorRoutine == null && executeInUpdate)
        {
            updateAnchorRoutine = UpdateAnchorAsync();
            StartCoroutine(updateAnchorRoutine);
        }
    }
#endif
}