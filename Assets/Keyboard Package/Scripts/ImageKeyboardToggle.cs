using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class ImageKeyboardToggle : MonoBehaviour
{
    // Reference to the keyboard GameObject
    public GameObject keyboard;

    // Make InputField public to assign from Inspector
    public InputField inputField;

    // UnityEvents for enable/disable actions
    public UnityEvent OnEnableEvent;
    public UnityEvent OnDisableEvent;

    // Static variable to store the currently enabled ImageKeyboardToggle
    private static ImageKeyboardToggle currentActiveImage;

    // Reference to the Image component of this GameObject
    private Image imageComponent;

    // Store the original color of the image
    private Color originalColor;

    private void Awake()
    {
        // Check if InputField is assigned
        if (inputField == null)
        {
            inputField = GetComponent<InputField>(); // Try to get it from the current GameObject
        }

        // Get the Image component and store the original color
        imageComponent = GetComponent<Image>();
        if (imageComponent != null)
        {
            originalColor = imageComponent.color;
        }
    }

    // Static method to disable all active keyboards
    public static void DisableAllKeyboards()
    {
        if (currentActiveImage != null)
        {
            currentActiveImage.DisableKeyboard();
            currentActiveImage = null;
        }
    }

    // Method called when this image is clicked
    private void OnMouseDown()
    {
        // Check if this image is already active
        if (currentActiveImage == this)
        {
            DisableKeyboard();
            currentActiveImage = null;
        }
        else
        {
            // Disable the currently active keyboard
            if (currentActiveImage != null)
            {
                currentActiveImage.OnDisableEvent.Invoke(); // Call OnDisableEvent on the previous keyboard
                currentActiveImage.DisableKeyboard();
            }

            // Enable this image's keyboard
            EnableKeyboard();
            OnEnableEvent.Invoke(); // Call OnEnableEvent on the newly clicked keyboard
            currentActiveImage = this;

            // Set the new input field for the GameManager
            GameManager.Instance.SetInputField(inputField);
        }
    }

    // Enables the keyboard associated with this image and changes its color to green
    private void EnableKeyboard()
    {
        if (keyboard != null)
        {
            keyboard.SetActive(true);
        }

        // Change the image color to green
        if (imageComponent != null)
        {
            imageComponent.color = Color.green;
        }
    }

    // Disables the keyboard associated with this image and resets its color to the original
    private void DisableKeyboard()
    {
        if (keyboard != null)
        {
            keyboard.SetActive(false);
        }

        // Reset the image color to the original
        if (imageComponent != null)
        {
            imageComponent.color = originalColor;
        }
        OnDisableEvent.Invoke(); // Ensure disable event is called even when disabling manually
    }
}
