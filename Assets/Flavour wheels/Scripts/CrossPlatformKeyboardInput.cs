using UnityEngine;
using TMPro;

public class CrossPlatformKeyboardInput : MonoBehaviour
{
    private TMP_InputField tmpInputField;

    void Start()
    {
        tmpInputField = this.GetComponent<TMP_InputField>();

        #if UNITY_WEBGL && !UNITY_EDITOR
        tmpInputField.onSelect.AddListener(HandleInputFieldSelect);
        #endif
    }

    #if UNITY_WEBGL && !UNITY_EDITOR
    public void HandleInputFieldSelect(string text)
    {
        RectTransform rectTransform = tmpInputField.GetComponent<RectTransform>();
        Vector3[] worldCorners = new Vector3[4];
        rectTransform.GetWorldCorners(worldCorners);

        Vector3 screenBottomLeft = RectTransformUtility.WorldToScreenPoint(Camera.main, worldCorners[0]);
        Vector3 screenTopRight = RectTransformUtility.WorldToScreenPoint(Camera.main, worldCorners[2]);

        float xPos = screenBottomLeft.x;
        float yPos = Screen.height - screenTopRight.y;
        float width = screenTopRight.x - screenBottomLeft.x;
        float height = screenTopRight.y - screenBottomLeft.y;

        CreateWebGLInput(tmpInputField.gameObject.name, xPos, yPos, width, height, (int)tmpInputField.contentType, tmpInputField.characterLimit);
    }

    public void OnInputChanged(string value)
    {
        tmpInputField.text = value;
        tmpInputField.onValueChanged.Invoke(value);
    }

    public void OnInputFinished()
    {
        tmpInputField.DeactivateInputField();
        tmpInputField.onEndEdit.Invoke(tmpInputField.text);
    }

    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void CreateWebGLInput(string elementId, float xPos, float yPos, float width, float height, int contentType, int characterLimit);
    #endif
}