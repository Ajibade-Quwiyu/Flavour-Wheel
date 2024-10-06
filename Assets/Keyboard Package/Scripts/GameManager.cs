using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    // Reference to the InputField itself
    private InputField activeInputField;

    private void Awake()
    {
        // Singleton pattern to ensure only one instance of GameManager exists
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Method to delete the last letter
    public void DeleteLetter()
    {
        if (activeInputField != null && activeInputField.text.Length != 0)
        {
            activeInputField.text = activeInputField.text.Remove(activeInputField.text.Length - 1, 1);
        }
        UpdatePlaceholder();
    }

    // Method to add a new letter to the InputField
    public void AddLetter(string letter)
    {
        if (activeInputField != null)
        {
            activeInputField.text += letter;
            UpdatePlaceholder();
        }
    }

    // Method to submit the word (clear the InputField and simulate "enter" behavior)
    public void SubmitWord()
    {
        if (activeInputField != null)
        {
            activeInputField.text = "";
            UpdatePlaceholder();
        }
    }

    // Method to handle showing or hiding the placeholder based on whether the InputField is empty
    private void UpdatePlaceholder()
    {
        if (activeInputField != null && activeInputField.placeholder != null)
        {
            var placeholder = activeInputField.placeholder.GetComponent<Text>();
            placeholder.enabled = activeInputField.text.Length == 0;
        }
    }

    // Method to set a new InputField
    public void SetInputField(InputField newInputField)
    {
        activeInputField = newInputField;
        UpdatePlaceholder(); // Update placeholder visibility based on initial state
    }
}
