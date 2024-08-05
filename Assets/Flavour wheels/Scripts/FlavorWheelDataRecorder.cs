using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FlavorWheelDataRecorder : MonoBehaviour
{
    public Transform duplicatedWheel; // Serialized field for the duplicated wheel
    public Transform Rating; // Serialized field for the Rating buttons
    public Text spiritNameText; // Public Text field to hold the spirit name

    private Transform names; // Transform for names, automatically set as the first child of duplicatedWheel
    private Text scoreText; // Text component for score, automatically set as the second child of duplicatedWheel
    private List<FlavorWheelController> controllers = new List<FlavorWheelController>(); // List of all FlavorWheelController components
    private List<(string imageName, string parentName)> selectedFlavors = new List<(string, string)>(); // List of selected flavors
    private List<Button> ratingButtons = new List<Button>(); // List of rating buttons
    private int currentRating = 0; // Current rating value
    private Text scoreTextMain; // Score text from the original Rating transform

    private AudioSource audioSource; // AudioSource from the camera
    [SerializeField] private AudioClip ratingSound; // Audio clip for rating button click

    private SpiritManager spiritManager; // Reference to SpiritManager
    [SerializeField] private SelectionManager selectionManager; // Reference to SelectionManager

    void Start()
    {
        // Automatically set names and scoreText to be the first and second child of duplicatedWheel
        if (duplicatedWheel != null && duplicatedWheel.childCount > 1)
        {
            names = duplicatedWheel.GetChild(0);
            scoreText = duplicatedWheel.GetChild(1).GetComponent<Text>();
        }

        // Get the AudioSource from the Camera
        audioSource = Camera.main.GetComponent<AudioSource>();

        // Get all FlavorWheelController components that are children of this object
        controllers.AddRange(GetComponentsInChildren<FlavorWheelController>());

        // Assign this FlavorWheelDataRecorder to each controller
        foreach (var controller in controllers)
        {
            controller.dataRecorder = this;
        }

        // Initialize rating buttons and score text
        if (Rating != null)
        {
            foreach (Transform child in Rating)
            {
                if (child.name == "Score")
                {
                    scoreTextMain = child.GetComponent<Text>();
                }
                else
                {
                    Button button = child.GetComponent<Button>();
                    if (button != null)
                    {
                        ratingButtons.Add(button);
                        int rating = ratingButtons.Count; // Assign rating based on position
                        button.onClick.AddListener(() => Rate(rating));
                        button.onClick.AddListener(PlayClickSound); // Add sound to the rating button click
                    }
                }
            }
        }

        // Initialize the names transform children
        InitializeNames();
        UpdateNamesDisplay();
        UpdateScoreText(); // Initialize score text

        // Find and reference the SpiritManager
        spiritManager = FindObjectOfType<SpiritManager>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            ListSelectedFlavors();
        }
        // Continuously check the TextMeshPro state and update parents
        UpdateTextMeshProParents();
    }

    public void RecordFlavor(string imageName, string parentName)
    {
        // Prevent adding more than 7 flavors
        if (selectedFlavors.Count >= 7)
        {
            return;
        }

        // Check if the flavor already exists
        foreach (var flavor in selectedFlavors)
        {
            if (flavor.imageName == imageName)
            {
                return;
            }
        }

        // Add flavor to the list and update the display
        selectedFlavors.Add((imageName, parentName));
        UpdateNamesDisplay();
        UpdateSpiritManager();

        // Call Select on SelectionManager
        if (selectionManager != null)
        {
            selectionManager.Select();
        }
    }

    public void UnrecordFlavor(string imageName, string parentName, bool removeParent)
    {
        // Remove flavor from the list and update the display
        selectedFlavors.RemoveAll(f => f.imageName == imageName && f.parentName == parentName);
        UpdateNamesDisplay();
        UpdateSpiritManager();

        // Call Unselect on SelectionManager
        if (selectionManager != null)
        {
            selectionManager.Unselect();
        }
    }

    public void Rate(int rating)
    {
        currentRating = rating; // Update current rating

        // Reset all buttons to their default color
        foreach (Button btn in ratingButtons)
        {
            btn.image.color = Color.white;
        }

        // Change the color of the buttons based on the rating
        Color gold = new Color(255 / 255f, 192 / 255f, 0 / 255f); // Gold color in RGB
        for (int i = 0; i < rating; i++)
        {
            ratingButtons[i].image.color = gold;
        }

        UpdateScoreText(); // Update both score texts
        UpdateSpiritManager();
    }

    private void EnableImage(Image image)
    {
        // Enable image and collider
        if (image != null)
        {
            image.gameObject.SetActive(true);
            image.color = Color.white;
            Collider2D collider = image.GetComponent<Collider2D>();
            if (collider != null)
            {
                collider.enabled = true;
            }
        }
    }

    private void DisableImage(Image image)
    {
        // Disable image and collider, and darken the image
        if (image != null)
        {
            image.color = new Color(0.35f, 0.35f, 0.35f, 1f);
            Collider2D collider = image.GetComponent<Collider2D>();
            if (collider != null)
            {
                collider.enabled = false;
            }
            image.gameObject.SetActive(false);
        }
    }

    private void ListSelectedFlavors()
    {
        // List all selected flavors (used for debugging or display)
        if (selectedFlavors.Count > 0)
        {
            foreach (var flavor in selectedFlavors)
            {
                Debug.Log($"Flavor: {flavor.imageName}, Parent: {flavor.parentName}");
            }
        }
        else
        {
            Debug.Log("No flavors have been selected.");
        }
    }

    private void InitializeNames()
    {
        // Initialize names by setting all TextMeshPro text to null
        for (int i = 0; i < names.childCount; i++)
        {
            Transform nameChild = names.GetChild(i);
            TextMeshProUGUI textMeshPro = nameChild.GetComponentInChildren<TextMeshProUGUI>();
            if (textMeshPro != null)
            {
                textMeshPro.text = null;
            }
        }
    }

    private void UpdateNamesDisplay()
    {
        // Update the display of names and enable corresponding images
        for (int i = 0; i < names.childCount; i++)
        {
            Transform nameChild = names.GetChild(i);
            if (i < selectedFlavors.Count)
            {
                nameChild.gameObject.SetActive(true);
                TextMeshProUGUI textMeshPro = nameChild.GetComponentInChildren<TextMeshProUGUI>();
                if (textMeshPro != null)
                {
                    textMeshPro.text = selectedFlavors[i].imageName;
                }

                // Enable corresponding image and its parent in duplicatedWheel
                EnableImageAndParentsInDuplicatedWheel(selectedFlavors[i].imageName);
            }
            else
            {
                nameChild.gameObject.SetActive(false);
            }
        }

        // Disable any images in duplicatedWheel that are not in the selectedFlavors list
        DisableUnselectedImagesAndParentsInDuplicatedWheel();
    }

    private void UpdateTextMeshProParents()
    {
        // Continuously check and update the TextMeshPro state and their parent
        for (int i = 0; i < names.childCount; i++)
        {
            Transform nameChild = names.GetChild(i);
            TextMeshProUGUI textMeshPro = nameChild.GetComponentInChildren<TextMeshProUGUI>();
            if (textMeshPro != null)
            {
                nameChild.gameObject.SetActive(!string.IsNullOrEmpty(textMeshPro.text));
            }
        }
    }

    private void UpdateScoreText()
    {
        // Update the score text
        if (scoreText != null)
        {
            scoreText.text = currentRating.ToString();
        }

        if (scoreTextMain != null)
        {
            scoreTextMain.text = currentRating.ToString();
        }
    }

    private void EnableImageAndParentsInDuplicatedWheel(string imageName)
    {
        // Enable image and its parents in the duplicated wheel
        foreach (Transform child in duplicatedWheel)
        {
            Transform target = FindChildRecursive(child, imageName);
            if (target != null)
            {
                EnableImage(target.GetComponent<Image>());
                EnableParentImages(target);
            }
        }
    }

    private void DisableUnselectedImagesAndParentsInDuplicatedWheel()
    {
        // Disable unselected images and their parents
        foreach (Transform child in duplicatedWheel)
        {
            DisableUnselectedImagesAndParentsRecursive(child);
        }
    }

    private void DisableUnselectedImagesAndParentsRecursive(Transform parent)
    {
        // Recursively disable unselected images and their parents
        foreach (Transform child in parent)
        {
            bool isSelected = false;
            foreach (var flavor in selectedFlavors)
            {
                if (child.name == flavor.imageName)
                {
                    isSelected = true;
                    break;
                }
            }

            if (!isSelected)
            {
                Image image = child.GetComponent<Image>();
                if (image != null)
                {
                    DisableImage(image);

                    // Check if all siblings are inactive, then disable the parent
                    Transform parentTransform = child.parent;
                    if (parentTransform != null && AreAllSiblingsInactive(child))
                    {
                        Image parentImage = parentTransform.GetComponent<Image>();
                        if (parentImage != null)
                        {
                            DisableImage(parentImage);
                        }
                    }
                }
            }
            else
            {
                DisableUnselectedImagesAndParentsRecursive(child);
            }
        }
    }

    private Transform FindChildRecursive(Transform parent, string name)
    {
        // Recursively find a child by name
        foreach (Transform child in parent)
        {
            if (child.name == name)
            {
                return child;
            }
            Transform found = FindChildRecursive(child, name);
            if (found != null)
            {
                return found;
            }
        }
        return null;
    }

    private void EnableParentImages(Transform child)
    {
        // Enable all parent images up the hierarchy
        Transform parent = child.parent;
        while (parent != null)
        {
            Image parentImage = parent.GetComponent<Image>();
            if (parentImage != null)
            {
                EnableImage(parentImage);
            }
            parent = parent.parent;
        }
    }

    private bool AreAllSiblingsInactive(Transform child)
    {
        // Check if all siblings of a child are inactive
        Transform parent = child.parent;
        if (parent == null) return false;

        foreach (Transform sibling in parent)
        {
            if (sibling != child && sibling.gameObject.activeSelf)
            {
                return false;
            }
        }
        return true;
    }

    private void PlayClickSound()
    {
        // Play the rating sound
        if (audioSource != null && ratingSound != null)
        {
            audioSource.PlayOneShot(ratingSound);
        }
    }

    private void UpdateSpiritManager()
    {
        // Update the "Spirit" text child of duplicatedWheel with the spirit name
        if (duplicatedWheel != null && duplicatedWheel.childCount > 2)
        {
            Text spiritText = duplicatedWheel.GetChild(2).GetComponent<Text>();
            if (spiritText != null)
            {
                spiritText.text = spiritNameText.text;
            }
        }

        // Send the data to the SpiritManager
        if (spiritManager != null && spiritNameText != null)
        {
            string spiritName = spiritNameText.text;
            spiritManager.ReceiveSpiritData(spiritName, selectedFlavors.Count, currentRating);
        }
    }
}
