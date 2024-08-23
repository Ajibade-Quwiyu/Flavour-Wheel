using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class FlavorWheelDataRecorder : MonoBehaviour
{
    public Transform duplicatedWheel, Rating;
    public Text spiritNameText;
    [SerializeField] private AudioClip ratingSound;

    private Transform names;
    private Text scoreText, scoreTextMain;
    private List<FlavorWheelController> controllers = new List<FlavorWheelController>();
    private List<(string imageName, string parentName)> selectedFlavors = new List<(string, string)>();
    private List<Button> ratingButtons = new List<Button>();
    private int currentRating;
    private AudioSource audioSource;
    private SpiritManager spiritManager;

    void Start()
    {
        if (duplicatedWheel != null && duplicatedWheel.childCount > 1)
        {
            names = duplicatedWheel.GetChild(0);
            scoreText = duplicatedWheel.GetChild(1).GetComponent<Text>();
        }

        audioSource = Camera.main.GetComponent<AudioSource>();
        controllers.AddRange(GetComponentsInChildren<FlavorWheelController>());
        controllers.ForEach(c => c.dataRecorder = this);

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

        InitializeNames();
        UpdateNamesDisplay();
        UpdateScoreText();
        spiritManager = FindObjectOfType<SpiritManager>();
    }

    void Update() => UpdateTextMeshProParents();

    public void RecordFlavor(string imageName, string parentName)
    {
        if (selectedFlavors.Count >= 7 || selectedFlavors.Any(f => f.imageName == imageName))
            return;

        selectedFlavors.Add((imageName, parentName));
        UpdateNamesDisplay();
        UpdateSpiritManager();
    }

    public void UnrecordFlavor(string imageName, string parentName, bool removeParent)
    {
        selectedFlavors.RemoveAll(f => f.imageName == imageName && f.parentName == parentName);
        UpdateNamesDisplay();
        UpdateSpiritManager();
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

    private void EnableImage(Image image)
    {
        if (image == null) return;
        image.gameObject.SetActive(true);
        image.color = Color.white;
        if (image.TryGetComponent(out Collider2D collider)) collider.enabled = true;
    }

    private void DisableImage(Image image)
    {
        if (image == null) return;
        image.color = new Color(0.35f, 0.35f, 0.35f, 1f);
        if (image.TryGetComponent(out Collider2D collider)) collider.enabled = false;
        image.gameObject.SetActive(false);
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
        // First, disable all images in the duplicated wheel
        for (int i = 1; i < duplicatedWheel.childCount; i++)
        {
            Transform wheelChild = duplicatedWheel.GetChild(i);
            if (wheelChild.TryGetComponent(out Image image))
            {
                DisableImage(image);
            }
            DisableAllChildren(wheelChild);
        }

        // Now, update the names and enable corresponding images
        for (int i = 0; i < names.childCount; i++)
        {
            Transform nameChild = names.GetChild(i);
            TextMeshProUGUI textMeshPro = nameChild.GetComponentInChildren<TextMeshProUGUI>();

            if (i < selectedFlavors.Count)
            {
                nameChild.gameObject.SetActive(true);
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
                if (textMeshPro != null)
                {
                    textMeshPro.text = "";
                }
            }
        }
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
        scoreText.text = currentRating.ToString();
        scoreTextMain.text = currentRating.ToString();
    }

    private void EnableImageAndParentsInDuplicatedWheel(string imageName)
    {
        for (int i = 1; i < duplicatedWheel.childCount; i++)
        {
            Transform wheelChild = duplicatedWheel.GetChild(i);
            Transform target = FindChildRecursive(wheelChild, imageName);
            if (target != null)
            {
                EnableImage(target.GetComponent<Image>());
                EnableParentImages(target);
                break; // Exit the loop once we've found and enabled the correct image
            }
        }
    }

    private void DisableUnselectedImagesAndParentsRecursive(Transform parent)
    {
        foreach (Transform child in parent)
        {
            if (!selectedFlavors.Any(f => f.imageName == child.name))
            {
                if (child.TryGetComponent(out Image image))
                {
                    DisableImage(image);
                    if (child.parent && AreAllSiblingsInactive(child))
                    {
                        child.parent.TryGetComponent(out Image parentImage);
                        DisableImage(parentImage);
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
        Transform parent = child.parent;
        while (parent != null)
        {
            if (parent.TryGetComponent(out Image parentImage))
                EnableImage(parentImage);
            parent = parent.parent;
        }
    }

    private bool AreAllSiblingsInactive(Transform child)
    {
        return child.parent != null && child.parent.GetComponentsInChildren<Transform>()
            .Where(t => t != child)
            .All(t => !t.gameObject.activeSelf);
    }

    private void PlayClickSound()
    {
        if (audioSource && ratingSound)
            audioSource.PlayOneShot(ratingSound);
    }

    private void UpdateSpiritManager()
    {
        if (duplicatedWheel && duplicatedWheel.childCount > 2)
            duplicatedWheel.GetChild(2).GetComponent<Text>().text = spiritNameText?.text;

        spiritManager?.ReceiveSpiritData(spiritNameText?.text ?? "", selectedFlavors.Count, currentRating);
    }
}