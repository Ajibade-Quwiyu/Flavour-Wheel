using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System;

[System.Serializable]
public class ChildHierarchy
{
    public Image image;
    public List<ChildHierarchy> children = new List<ChildHierarchy>();
}

public class FlavorWheelController : MonoBehaviour
{
    private ChildHierarchy hierarchy;
    private Dictionary<Image, Vector3> originalScales = new Dictionary<Image, Vector3>();
    private int currentNumber;
    public TMP_Text numberText;
    public float animationDuration = 1f, endpos = 50f;
    private Color darkenColor = new Color(0.35f, 0.35f, 0.35f, 1f), doubleClickColor = Color.yellow;
    private float doubleClickTime = 0.3f, lastClickTime;
    private bool isDoubleClick;
    public FlavorWheelDataRecorder dataRecorder;
    public AudioClip clickClip, doubleClickClip;
    private AudioSource audioSource;
    private Dictionary<Image, bool> selectedImages = new Dictionary<Image, bool>();
    private Dictionary<Image, bool> doubleClickedImages = new Dictionary<Image, bool>();
    private HashSet<Image> selectedParentImages = new HashSet<Image>();
    private const int MAX_SELECTIONS = 7;
    private bool isMaxSelectionsReached = false;

    void Start()
    {
        InitializeController();
    }

    void Update()
    {
        HandleInput();
    }

    // Initialization methods
    private void InitializeController()
    {
        hierarchy = new ChildHierarchy { image = GetComponent<Image>() };
        PopulateHierarchy(transform, hierarchy);
        ResetHierarchyRecursive(hierarchy);
        UpdateNumberText();

        audioSource = Camera.main.GetComponent<AudioSource>();
    }

    private void PopulateHierarchy(Transform parent, ChildHierarchy parentHierarchy)
    {
        foreach (Transform child in parent)
        {
            Image childImage = child.GetComponent<Image>();
            if (childImage != null)
            {
                ChildHierarchy childHierarchy = new ChildHierarchy { image = childImage };
                parentHierarchy.children.Add(childHierarchy);
                PopulateHierarchy(child, childHierarchy);
            }
        }
    }

    // Input handling methods
    private void HandleInput()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        var hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
        if (hit.collider?.GetComponent<Image>() is Image image && IsPartOfHierarchy(image))
        {
            if (image == GetComponent<Image>())
            {
                HandleMainImageClick();
            }
            else
            {
                isDoubleClick = Time.time - lastClickTime <= doubleClickTime;
                if (isDoubleClick)
                {
                    HandleDoubleClick(image);
                }
                else
                {
                    StartCoroutine(HandleSingleClick(image));
                }
            }
            lastClickTime = Time.time;
        }
    }

    private void HandleMainImageClick()
    {
        // Enable all immediate children of the main image
        hierarchy.children.ForEach(child => EnableImage(child.image));
        selectedParentImages.Clear(); // Clear selected parents when main image is clicked
    }

    private IEnumerator HandleSingleClick(Image image)
    {
        yield return new WaitForSeconds(doubleClickTime);
        if (!isDoubleClick)
        {
            if (IsGrandchild(image))
            {
                if (selectedImages.TryGetValue(image, out bool isSelected) && isSelected)
                {
                    UnselectImage(image);
                }
                else if (dataRecorder.GetSelectionCount() < MAX_SELECTIONS)
                {
                    SelectImage(image, false);
                    ScaleImage(image, true);
                    selectedImages[image] = true;
                    doubleClickedImages[image] = false;
                    dataRecorder?.RecordFlavor(image.name, image.transform.parent?.name, false);
                    Select(1);
                }
                else
                {
                    DisplayMaxSelectionsReached();
                }
            }
            else
            {
                ToggleImageState(image);
            }
            PlayClickSound();
        }
    }

    private void HandleDoubleClick(Image image)
    {
        if (!IsGrandchild(image)) return;

        if (selectedImages.TryGetValue(image, out bool isSelected) && isSelected)
        {
            UnselectImage(image);
        }
        else if (dataRecorder.GetSelectionCount() < MAX_SELECTIONS)
        {
            SelectImage(image, true);
            ScaleImage(image, true);
            ApplyDoubleClickIndicator(image);
            selectedImages[image] = true;
            doubleClickedImages[image] = true;
            dataRecorder?.RecordFlavor(image.name, image.transform.parent?.name, true);
            Select(2);
        }
        else
        {
            DisplayMaxSelectionsReached();
        }
        PlayDoubleClickSound();
        Handheld.Vibrate();
    }
    // Image state management methods
    private void ToggleImageState(Image image)
    {
        if (image == null) return;

        if (IsGrandchild(image))
        {
            if (selectedImages.TryGetValue(image, out bool isSelected) && isSelected)
            {
                UnselectImage(image);
            }
            else if (dataRecorder.GetSelectionCount() < MAX_SELECTIONS)
            {
                SelectImage(image, false);
                ScaleImage(image, true);
                selectedImages[image] = true;
                doubleClickedImages[image] = false;
                dataRecorder?.RecordFlavor(image.name, image.transform.parent?.name, false);
                Select(1);
            }
            else
            {
                DisplayMaxSelectionsReached();
            }
        }
        else // Parent image
        {
            var hierarchy = FindHierarchy(image);
            if (hierarchy != null)
            {
                bool wasEnabled = image.color == Color.white;
                if (wasEnabled)
                {
                    // If the parent was enabled, toggle its children
                    bool anyChildEnabled = hierarchy.children.Any(child => child.image.color == Color.white);
                    if (anyChildEnabled)
                    {
                        // Disable all children
                        foreach (var child in hierarchy.children)
                        {
                            DarkenAndDisable(child.image);
                            UnselectAllGrandchildren(child);
                        }
                    }
                    else
                    {
                        // Enable all children
                        foreach (var child in hierarchy.children)
                        {
                            EnableImage(child.image);
                        }
                    }
                }
                else
                {
                    // If the parent was disabled, enable it and its children
                    DisableSiblingsOfParent(image);
                    EnableImage(image);
                    foreach (var child in hierarchy.children)
                    {
                        EnableImage(child.image);
                    }
                }
            }
        }
    }

    private void UnselectAllGrandchildren(ChildHierarchy parent)
    {
        foreach (var child in parent.children)
        {
            if (child.children.Count == 0) // This is a grandchild
            {
                if (selectedImages.TryGetValue(child.image, out bool isSelected) && isSelected)
                {
                    UnselectImage(child.image);
                }
            }
            else
            {
                UnselectAllGrandchildren(child);
            }
        }
    }

    private void SelectImage(Image image, bool isDoubleClick)
    {
        var hierarchy = FindHierarchy(image);
        if (hierarchy == null) return;

        if (IsGrandchild(image) && dataRecorder.GetSelectionCount() < MAX_SELECTIONS)
        {
            ScaleImage(image, true);
            selectedImages[image] = true;
            EnableImage(image);
            if (isDoubleClick)
            {
                ApplyDoubleClickIndicator(image);
            }
        }
        else if (!IsGrandchild(image))
        {
            DisableSiblingsOfParent(image);
            EnableImage(image);
            hierarchy.children.ForEach(child => EnableImage(child.image));
        }
    }
    private void UnselectImage(Image image)
    {
        bool anySiblingSelected = image.transform.parent.GetComponentsInChildren<Image>()
            .Any(siblingImage => siblingImage != image && selectedImages.TryGetValue(siblingImage, out bool selected) && selected);

        dataRecorder?.UnrecordFlavor(image.name, image.transform.parent?.name, !anySiblingSelected);
        RemoveDoubleClickIndicator(image);
        ScaleImage(image, false);
        
        int decrementAmount = doubleClickedImages.TryGetValue(image, out bool isDoubleClicked) && isDoubleClicked ? 2 : 1;
        selectedImages.Remove(image);
        doubleClickedImages.Remove(image);
        Unselect(decrementAmount);
    }
    private void DisableSiblingsOfParent(Image image)
    {
        ChildHierarchy parentHierarchy = FindParentHierarchy(image);
        if (parentHierarchy != null)
        {
            foreach (var sibling in parentHierarchy.children)
            {
                if (sibling.image != image)
                {
                    DarkenAndDisable(sibling.image);
                    UnselectAllGrandchildren(sibling);
                }
            }
        }
    }
    // Counter management methods
    public void Select(int increment = 1)
    {
        currentNumber += increment;
        UpdateNumberText();
        if (dataRecorder.GetSelectionCount() >= MAX_SELECTIONS)
        {
            DisplayMaxSelectionsReached();
        }
        else
        {
            StartCoroutine(AnimateNumber("+" + increment));
        }
    }

    public void Unselect(int decrement = 1)
    {
        currentNumber -= decrement;
        UpdateNumberText();
        StartCoroutine(AnimateNumber("-" + decrement));
    }

    private void UpdateNumberText()
    {
        numberText.text = currentNumber.ToString();
    }
    public void DisplayMaxSelectionsReached()
    {
        StartCoroutine(AnimateMaxReachedMessage());
    }
    private IEnumerator AnimateMaxReachedMessage()
    {
        TMP_Text maxReachedText = Instantiate(numberText, numberText.transform.parent);
        maxReachedText.text = "Max\nReached";
        maxReachedText.fontSize = 30; // Starting size
        maxReachedText.gameObject.SetActive(true);
        maxReachedText.transform.localPosition = numberText.transform.localPosition;

        float elapsed = 0f;
        Vector3 startPos = maxReachedText.transform.localPosition;
        Vector3 targetPos = new Vector3(startPos.x, endpos, startPos.z);
        float startFontSize = 30f;
        float endFontSize = 100f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            maxReachedText.transform.localPosition = Vector3.Lerp(startPos, targetPos, t);
            maxReachedText.fontSize = Mathf.Lerp(startFontSize, endFontSize, t);
            maxReachedText.color = new Color(maxReachedText.color.r, maxReachedText.color.g, maxReachedText.color.b, 1 - t);
            yield return null;
        }

        Destroy(maxReachedText.gameObject);
    }

    private IEnumerator AnimateNumber(string changeText)
    {
        TMP_Text changeTMP = Instantiate(numberText, numberText.transform.parent);
        changeTMP.text = changeText;
        changeTMP.gameObject.SetActive(true);
        changeTMP.transform.localPosition = numberText.transform.localPosition;

        float elapsed = 0f;
        Vector3 startPos = changeTMP.transform.localPosition;
        Vector3 targetPos = new Vector3(startPos.x, endpos, startPos.z);
        float startFontSize = 50f;
        float endFontSize = 200f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            changeTMP.transform.localPosition = Vector3.Lerp(startPos, targetPos, t);
            changeTMP.fontSize = Mathf.Lerp(startFontSize, endFontSize, t);
            changeTMP.color = new Color(changeTMP.color.r, changeTMP.color.g, changeTMP.color.b, 1 - t);
            yield return null;
        }

        Destroy(changeTMP.gameObject);
    }

    // Image manipulation methods
    private void ApplyDarkenAndDisableToHierarchy(List<ChildHierarchy> children)
    {
        children.ForEach(child =>
        {
            DarkenAndDisable(child.image);
            ApplyDarkenAndDisableToHierarchy(child.children);
        });
    }

    private void DarkenAndDisable(Image image)
    {
        if (image == null) return;
        image.color = darkenColor;
        if (image.TryGetComponent(out Collider2D collider))
        {
            collider.enabled = false;
        }
    }

    private void EnableImage(Image image)
    {
        if (image == null) return;
        image.gameObject.SetActive(true);
        if (IsGrandchild(image))
        {
            image.color = selectedImages.TryGetValue(image, out bool isSelected) && isSelected ? 
                (image.color == doubleClickColor ? doubleClickColor : Color.white) : Color.white;
        }
        else
        {
            image.color = Color.white;
        }
        if (image.TryGetComponent(out Collider2D collider))
        {
            collider.enabled = true;
        }
    }
    private void ScaleImage(Image image, bool scaleUp)
    {
        if (image == null) return;

        if (!originalScales.ContainsKey(image))
            originalScales[image] = image.rectTransform.localScale;

        image.rectTransform.localScale = scaleUp
            ? originalScales[image] * 1.2f
            : originalScales[image];
    }

     private void ApplyDoubleClickIndicator(Image image)
    {
        if (image != null && IsGrandchild(image))
        {
            image.color = doubleClickColor;
        }
    }

    private void RemoveDoubleClickIndicator(Image image)
    {
        if (image != null && IsGrandchild(image))
        {
            image.color = Color.white;
        }
    }
    // Hierarchy navigation methods
    private ChildHierarchy FindParentHierarchy(Image image) =>
        FindParentHierarchyRecursive(hierarchy, image);

    private ChildHierarchy FindParentHierarchyRecursive(ChildHierarchy parent, Image image)
    {
        return parent.children.FirstOrDefault(child => child.image == image) != null
            ? parent
            : parent.children.Select(child => FindParentHierarchyRecursive(child, image))
                             .FirstOrDefault(result => result != null);
    }

    private ChildHierarchy FindHierarchy(Image image) => FindHierarchyRecursive(hierarchy, image);

    private ChildHierarchy FindHierarchyRecursive(ChildHierarchy hierarchy, Image image)
    {
        if (hierarchy.image == image) return hierarchy;
        return hierarchy.children.Select(child => FindHierarchyRecursive(child, image))
                                 .FirstOrDefault(found => found != null);
    }

    private bool IsGrandchild(Image image)
    {
        var hierarchy = FindHierarchy(image);
        return hierarchy != null && FindParentHierarchyRecursive(this.hierarchy, image) != null && hierarchy.children.Count == 0;
    }

    private bool IsPartOfHierarchy(Image image)
    {
        return FindHierarchy(image) != null;
    }

    // Audio methods
    private void PlayClickSound()
    {
        audioSource.PlayOneShot(clickClip);
    }

    private void PlayDoubleClickSound()
    {
        audioSource.PlayOneShot(doubleClickClip);
    }

    // Reset method
    public void ResetController()
    {
        currentNumber = 0;
        UpdateNumberText();
        selectedImages.Clear();
        ResetHierarchyRecursive(hierarchy);
    }
    private IEnumerator ResetMaxSelectionsMessage()
    {
        yield return new WaitForSeconds(2f); // Display for 2 seconds
        isMaxSelectionsReached = false;
        UpdateNumberText();
    }

    private void ResetHierarchyRecursive(ChildHierarchy currentHierarchy)
    {
        if (currentHierarchy.image != null)
        {
            if (currentHierarchy.image == GetComponent<Image>()) // Outermost image (the one with this script)
            {
                EnableImage(currentHierarchy.image);
            }
            else
            {
                DarkenAndDisable(currentHierarchy.image);
            }
            ResetImageScale(currentHierarchy.image);
        }

        foreach (var child in currentHierarchy.children)
        {
            ResetHierarchyRecursive(child);
        }
    }

    private void ResetImageScale(Image image)
    {
        if (image == null) return;

        if (originalScales.TryGetValue(image, out Vector3 originalScale))
        {
            image.rectTransform.localScale = originalScale;
        }
    }
}