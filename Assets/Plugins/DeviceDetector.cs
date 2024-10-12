using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Runtime.InteropServices;

public class DeviceDetector : MonoBehaviour
{
    [System.Serializable]
    public class DeviceDetectedEvent : UnityEvent<string> { }

    public DeviceDetectedEvent onDeviceDetected;
    public DeviceDetectedEvent onAndroidDetected;
    public DeviceDetectedEvent oniOSDetected;
    public DeviceDetectedEvent oniPadDetected;
    public DeviceDetectedEvent onPCDetected;

    [SerializeField]
    private RectTransform targetRectTransform;

    [SerializeField]
    private float iPadHeight = 500f;

    [DllImport("__Internal")]
    private static extern System.IntPtr DetectDevice();

    void Start()
    {
        string deviceType = GetDeviceType();
        onDeviceDetected.Invoke(deviceType);

        switch (deviceType)
        {
            case "Android":
                onAndroidDetected.Invoke(deviceType);
                break;
            case "iOS":
                oniOSDetected.Invoke(deviceType);
                break;
            case "iPad":
                oniPadDetected.Invoke(deviceType);
                AdjustiPadLayout();
                break;
            case "PC":
                onPCDetected.Invoke(deviceType);
                break;
        }
    }

    private void AdjustiPadLayout()
    {
        if (targetRectTransform != null)
        {
            targetRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, iPadHeight);
            Debug.Log($"Adjusted RectTransform height for iPad: {iPadHeight}");
        }
        else
        {
            Debug.LogWarning("Target RectTransform is not assigned. Cannot adjust layout for iPad.");
        }
    }

    public static string GetDeviceType()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        System.IntPtr ptr = DetectDevice();
        if (ptr != System.IntPtr.Zero)
        {
            string result = Marshal.PtrToStringAnsi(ptr);
            Marshal.FreeHGlobal(ptr);
            return result;
        }
        else
        {
            Debug.LogError("DetectDevice returned null pointer");
            return "Error";
        }
#else
        return "Other";
#endif
    }
}