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
            isDoubleClick = Time.time - lastClickTime <= doubleClickTime;
            if (isDoubleClick)
            {
                HandleDoubleClick(image);
            }
            else
            {
                StartCoroutine(HandleSingleClick(image));
            }
            lastClickTime = Time.time;
        }
    }

    private IEnumerator HandleSingleClick(Image image)
    {
        yield return new WaitForSeconds(doubleClickTime);
        if (!isDoubleClick)
        {
            ToggleImageState(image);
            PlayClickSound();
        }
    }

    private void HandleDoubleClick(Image image)
    {
        if (!IsGrandchild(image)) return;

        Select(selectedImages.TryGetValue(image, out bool isSelected) && isSelected ? 1 : 2);
        ScaleImage(image, true);
        ApplyDoubleClickIndicator(image);
        selectedImages[image] = true;
        dataRecorder?.RecordFlavor(image.name, image.transform.parent?.name);
        PlayDoubleClickSound();
        Handheld.Vibrate();
    }

    // Image state management methods
    private void ToggleImageState(Image image)
    {
        if (image == null) return;
        (selectedImages.TryGetValue(image, out bool isSelected) && isSelected ?
            (Action<Image>)UnselectImage : SelectImage)(image);
    }

    private void SelectImage(Image image)
    {
        var hierarchy = FindHierarchy(image);
        if (hierarchy == null) return;

        if (IsGrandchild(image))
        {
            ScaleImage(image, true);
            selectedImages[image] = true;
            Select(1);
        }
        else
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
        int decrementAmount = (image.color == doubleClickColor) ? 2 : 1;
        RemoveDoubleClickIndicator(image);
        ScaleImage(image, false);
        selectedImages.Remove(image);
        Unselect(decrementAmount);
    }

    // Counter management methods
    public void Select(int increment = 1)
    {
        currentNumber += increment;
        UpdateNumberText();
        StartCoroutine(AnimateNumber("+" + increment));
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

    private IEnumerator AnimateNumber(string changeText)
    {
        TMP_Text changeTMP = Instantiate(numberText, numberText.transform.parent);
        changeTMP.text = changeText;
        changeTMP.gameObject.SetActive(true);
        changeTMP.transform.localPosition = numberText.transform.localPosition;

        float elapsed = 0f;
        Vector3 targetPos = new Vector3(changeTMP.transform.localPosition.x, endpos, changeTMP.transform.localPosition.z);

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            changeTMP.transform.localPosition = Vector3.Lerp(changeTMP.transform.localPosition, targetPos, elapsed / animationDuration);
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
        image.color = Color.white;
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
        if (image != null) image.color = doubleClickColor;
    }

    private void RemoveDoubleClickIndicator(Image image)
    {
        if (image != null) image.color = Color.white;
    }

    private void DisableSiblingsOfParent(Image image)
    {
        FindParentHierarchy(image)?.children
            .Where(sibling => sibling.image != image)
            .ToList()
            .ForEach(sibling => DarkenAndDisable(sibling.image));
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