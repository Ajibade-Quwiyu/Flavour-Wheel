using UnityEngine;
using TMPro;

public class CrossPlatformKeyboardInput : MonoBehaviour
{
    private TMP_InputField tmpInputField;

    void Start()
    {
        tmpInputField = this.GetComponent<TMP_InputField>();
        tmpInputField.onSelect.AddListener(HandleInputFieldSelect);
        tmpInputField.onDeselect.AddListener(HandleInputFieldDeselect);
    }

    public void HandleInputFieldSelect(string text)
    {
        RectTransform rectTransform = tmpInputField.GetComponent<RectTransform>();
        Vector3[] worldCorners = new Vector3[4];
        rectTransform.GetWorldCorners(worldCorners);

        Vector3 bottomLeft = worldCorners[0];
        Vector3 topRight = worldCorners[2];

        Vector3 screenBottomLeft = RectTransformUtility.WorldToScreenPoint(Camera.main, bottomLeft);
        Vector3 screenTopRight = RectTransformUtility.WorldToScreenPoint(Camera.main, topRight);

        float xPos = screenBottomLeft.x;
        float yPos = Screen.height - screenTopRight.y;
        float width = screenTopRight.x - screenBottomLeft.x;
        float height = screenTopRight.y - screenBottomLeft.y;

        TMP_InputField.ContentType contentType = tmpInputField.contentType;
        int characterLimit = tmpInputField.characterLimit;

#if UNITY_WEBGL && !UNITY_EDITOR
        CreateWebGLInput(tmpInputField.gameObject.name, xPos, yPos, width, height, (int)contentType, characterLimit);
#endif
    }

    public void OnInputChanged(string value)
    {
        tmpInputField.text = value;
    }

    public void OnInputFinished()
    {
        tmpInputField.DeactivateInputField();
    }

    public void HandleInputFieldDeselect(string text)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        OnInputFinished();
#endif
    }

    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void CreateWebGLInput(string elementId, float xPos, float yPos, float width, float height, int contentType, int characterLimit);
}