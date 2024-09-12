using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Collections;

public class FlavorWheelDataRecorder : MonoBehaviour
{
    private enum WheelOrder { First = 1, Second, Third, Fourth, Fifth }

    [SerializeField] private Transform duplicatedWheel;
    [SerializeField] private Transform Rating;
    [SerializeField] private Text spiritNameText;
    [SerializeField] private AudioClip ratingSound;

    private WheelOrder wheelOrder;
    private Transform names;
    private Text scoreText, scoreTextMain;
    private List<FlavorWheelController> controllers = new List<FlavorWheelController>();
    private List<(string imageName, string parentName, bool isDoubleClick)> selectedFlavors = new List<(string, string, bool)>();
    private List<Button> ratingButtons = new List<Button>();
    private int currentRating;
    private AudioSource audioSource;
    private SpiritManager spiritManager;
    private const int MAX_SELECTIONS = 7;

    private void Awake()
    {
        AssignWheelOrder();
    }

    private void Start()
    {
        InitializeComponents();
        InitializeWheel();
        StartCoroutine(DelayedUpdateSpiritManager());
    }

    void Update()
    {
        UpdateTextMeshProParents();
    }
    private void AssignWheelOrder()
    {
        int siblingIndex = transform.GetSiblingIndex();
        wheelOrder = (WheelOrder)Mathf.Clamp(siblingIndex + 1, 1, 5);
    }

    private IEnumerator DelayedUpdateSpiritManager()
    {
        yield return null; // Wait for the next frame
        UpdateSpiritManager();
        spiritManager = FindObjectOfType<SpiritManager>();
    }

    // Initialization methods
    private void InitializeComponents()
    {
        if (duplicatedWheel != null && duplicatedWheel.childCount > 1)
        {
            names = duplicatedWheel.GetChild(0);
            scoreText = duplicatedWheel.GetChild(1).GetComponent<Text>();
        }

        audioSource = Camera.main.GetComponent<AudioSource>();
        controllers.AddRange(GetComponentsInChildren<FlavorWheelController>());
        controllers.ForEach(c => c.dataRecorder = this);

        SetupRatingButtons();

        spiritManager = FindObjectOfType<SpiritManager>();
    }

    private void SetupRatingButtons()
    {
        if (Rating != null)
        {
            foreach (Transform child in Rating)
            {
                if (child.name == "Score")
                    scoreTextMain = child.GetComponent<Text>();
                else if (child.TryGetComponent(out Button button))
                {
                    ratingButtons.Add(button);
                    int rating = ratingButtons.Count;
                    button.onClick.AddListener(() => Rate(rating));
                    button.onClick.AddListener(PlayClickSound);
                }
            }
        }
    }

    private void InitializeWheel()
    {
        InitializeNames();
        UpdateNamesDisplay();
        UpdateScoreText();
    }

    private void InitializeNames()
    {
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

    // Update methods
    public void RecordFlavor(string imageName, string parentName, bool isDoubleClick)
    {
        if (selectedFlavors.Count >= MAX_SELECTIONS || selectedFlavors.Any(f => f.imageName == imageName))
        {
            Debug.Log($"Cannot record flavor {imageName}. Max selections reached or flavor already selected.");
            return;
        }

        selectedFlavors.Add((imageName, parentName, isDoubleClick));
        UpdateNamesDisplay();
        UpdateSpiritManager();

        if (selectedFlavors.Count == MAX_SELECTIONS)
        {
            NotifyControllersMaxReached();
        }
    }
    public void UnrecordFlavor(string imageName, string parentName, bool removeParent)
    {
        selectedFlavors.RemoveAll(f => f.imageName == imageName && f.parentName == parentName);
        UpdateNamesDisplay();
        UpdateSpiritManager();

        Debug.Log($"Flavor unselected. Current selection count: {selectedFlavors.Count}");
    }

    private void NotifyControllersMaxReached()
    {
        foreach (var controller in controllers)
        {
            controller.DisplayMaxSelectionsReached();
        }
    }

    public void Rate(int rating)
    {
        currentRating = rating;
        Color gold = new Color(1f, 0.75f, 0f);

        for (int i = 0; i < ratingButtons.Count; i++)
            ratingButtons[i].image.color = i < rating ? gold : Color.white;

        UpdateScoreText();
        UpdateSpiritManager();
    }

    private void UpdateNamesDisplay()
    {
        for (int i = 1; i < duplicatedWheel.childCount; i++)
        {
            Transform wheelChild = duplicatedWheel.GetChild(i);
            if (wheelChild.TryGetComponent(out Image image))
            {
                DisableImage(image);
            }
            DisableAllChildren(wheelChild);
        }

        for (int i = 0; i < names.childCount; i++)
        {
            Transform nameChild = names.GetChild(i);
            Image nameholderImage = nameChild.GetComponent<Image>();
            TextMeshProUGUI textMeshPro = nameChild.GetComponentInChildren<TextMeshProUGUI>();

            if (i < selectedFlavors.Count)
            {
                nameChild.gameObject.SetActive(true);
                if (textMeshPro != null)
                {
                    textMeshPro.text = selectedFlavors[i].imageName;
                    textMeshPro.overflowMode = TMPro.TextOverflowModes.Ellipsis;
                }
                if (nameholderImage != null)
                {
                    // Set alpha based on whether it's a double click or not
                    Color imageColor = nameholderImage.color;
                    imageColor.a = selectedFlavors[i].isDoubleClick ? 1f : 100f / 255f;
                    nameholderImage.color = imageColor;
                }
                EnableImageAndParentsInDuplicatedWheel(selectedFlavors[i].imageName, selectedFlavors[i].isDoubleClick);
            }
            else
            {
                nameChild.gameObject.SetActive(false);
                if (textMeshPro != null)
                {
                    textMeshPro.text = "";
                }
            }
        }
    }
    public int GetSelectionCount()
    {
        return selectedFlavors.Count;
    }
    private void UpdateTextMeshProParents()
    {
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
        scoreText.text = currentRating.ToString();
        scoreTextMain.text = currentRating.ToString();
    }

    private void UpdateSpiritManager()
    {
        if (duplicatedWheel && duplicatedWheel.childCount > 2)
        {
            Text spiritNameDisplay = duplicatedWheel.GetChild(2).GetComponent<Text>();
            if (spiritNameDisplay != null)
            {
                spiritNameDisplay.text = spiritNameText?.text ?? "";
            }
        }

        if (spiritManager != null)
        {
            int flavorCount = selectedFlavors.Count;
            // Ensure we send at least 1 for the flavor count
            flavorCount = flavorCount <= 1 ? 1 : flavorCount;

            spiritManager.ReceiveSpiritData(spiritNameText?.text ?? "", flavorCount, currentRating, (int)wheelOrder);
        }
    }

    // Reset methods
    public void ResetEverything()
    {
        selectedFlavors.Clear();
        currentRating = 0;

        ResetRatingButtons();
        ResetDuplicatedWheel();
        ResetControllers();

        InitializeWheel();
        UpdateSpiritManager();
    }

    private void ResetRatingButtons()
    {
        foreach (Button button in ratingButtons)
        {
            button.image.color = Color.white;
        }
    }

    private void ResetDuplicatedWheel()
    {
        if (duplicatedWheel != null)
        {
            for (int i = 1; i < duplicatedWheel.childCount; i++)
            {
                Transform wheelChild = duplicatedWheel.GetChild(i);
                DisableAllChildren(wheelChild);
            }
        }
    }

    private void ResetControllers()
    {
        foreach (FlavorWheelController controller in controllers)
        {
            controller.ResetController();
        }
    }

    // Utility methods
    private void EnableImage(Image image, bool isDoubleClick)
    {
        if (image == null) return;
        image.gameObject.SetActive(true);
        if (image.transform.childCount == 0) // Only change color for leaf nodes (grandchildren)
        {
            Color imageColor = isDoubleClick ? Color.yellow : Color.white;
            imageColor.a = 1f; // Ensure full opacity for the actual flavor images
            image.color = imageColor;
        }
        else
        {
            image.color = Color.white;
        }
        if (image.TryGetComponent(out Collider2D collider)) collider.enabled = true;
    }
    private void DisableImage(Image image)
    {
        if (image == null) return;
        image.color = new Color(0.35f, 0.35f, 0.35f, 1f);
        if (image.TryGetComponent(out Collider2D collider)) collider.enabled = false;
        image.gameObject.SetActive(false);
    }

    private void DisableAllChildren(Transform parent)
    {
        foreach (Transform child in parent)
        {
            if (child.TryGetComponent(out Image image))
            {
                DisableImage(image);
            }
            child.gameObject.SetActive(false);
            DisableAllChildren(child);
        }
    }

     private void EnableImageAndParentsInDuplicatedWheel(string imageName, bool isDoubleClick)
    {
        for (int i = 1; i < duplicatedWheel.childCount; i++)
        {
            Transform wheelChild = duplicatedWheel.GetChild(i);
            Transform target = FindChildRecursive(wheelChild, imageName);
            if (target != null)
            {
                EnableImage(target.GetComponent<Image>(), isDoubleClick);
                EnableParentImages(target);
                break;
            }
        }
    }
    private Transform FindChildRecursive(Transform parent, string name)
    {
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
        Transform parent = child.parent;
        while (parent != null)
        {
            if (parent.TryGetComponent(out Image parentImage))
            {
                parentImage.gameObject.SetActive(true);
                parentImage.color = Color.white;
                if (parentImage.TryGetComponent(out Collider2D collider)) collider.enabled = true;
            }
            parent = parent.parent;
        }
    }
    private void PlayClickSound()
    {
        if (audioSource && ratingSound)
            audioSource.PlayOneShot(ratingSound);
    }

    // Public method for button click
    public void OnResetButtonClick()
    {
        ResetEverything();
    }
}