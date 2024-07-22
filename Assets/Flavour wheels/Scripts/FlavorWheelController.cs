using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class ChildHierarchy
{
    public Image image;
    public List<ChildHierarchy> children;

    public ChildHierarchy()
    {
        children = new List<ChildHierarchy>();
    }
}

public class FlavorWheelController : MonoBehaviour
{
    private ChildHierarchy hierarchy; // Root hierarchy for images
    private Dictionary<Image, Vector3> originalScales = new Dictionary<Image, Vector3>(); // Store original scales of images
    private HashSet<Image> scaledLastHierarchyImages = new HashSet<Image>(); // Store scaled images in the last hierarchy
    private Image currentImage = null; // Currently selected image
    private Color darkenColor = new Color(0.35f, 0.35f, 0.35f, 1f); // Color to darken images
    private Color doubleClickColor = Color.yellow; // Color to indicate double-clicked images

    private float doubleClickTime = 0.3f; // Time interval for double-click detection
    private float lastClickTime = 0f; // Time of the last click
    private bool isDoubleClick = false; // Flag for double-click detection

    public FlavorWheelDataRecorder dataRecorder; // Reference to the associated FlavorWheelDataRecorder

    // Audio clips for click and double click
    public AudioClip clickClip;
    public AudioClip doubleClickClip;
    private AudioSource audioSource; // Audio source for playing clips

    void Start()
    {
        // Automatically populate the hierarchy
        hierarchy = new ChildHierarchy { image = GetComponent<Image>() };
        PopulateHierarchy(transform, hierarchy);

        // Apply darken and disable to all children from the start
        ApplyDarkenAndDisableToHierarchy(hierarchy.children);

        // Get the AudioSource from the Camera
        audioSource = Camera.main.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogError("AudioSource not found on the Camera.");
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

            if (hit.collider != null)
            {
                Image image = hit.collider.GetComponent<Image>();
                if (image != null && IsPartOfHierarchy(image))
                {
                    float timeSinceLastClick = Time.time - lastClickTime;
                    if (timeSinceLastClick <= doubleClickTime)
                    {
                        isDoubleClick = true;
                        HandleDoubleClick(image);
                    }
                    else
                    {
                        isDoubleClick = false;
                        StartCoroutine(HandleSingleClick(image));
                    }
                    lastClickTime = Time.time;
                }
            }
        }
    }

    // Handle single click with delay to differentiate from double-click
    private IEnumerator HandleSingleClick(Image image)
    {
        yield return new WaitForSeconds(doubleClickTime);

        if (!isDoubleClick)
        {
            ToggleImageState(image);
            PlayClickSound(); // Play click sound for normal clicks
        }
    }

    // Handle double-click interactions
    private void HandleDoubleClick(Image image)
    {
        if (IsGrandchild(image))
        {
            ScaleImage(image, true); // Always scale on double click
            ApplyDoubleClickIndicator(image); // Apply visual indicator for double-clicked images

            // Record the flavor and its parent in the data recorder
            if (dataRecorder != null)
            {
                dataRecorder.RecordFlavor(image.name, image.transform.parent?.name);
            }

            PlayDoubleClickSound(); // Play double-click sound
            Vibrate(); // Cause vibration on double click
        }
    }

    // Play click sound for normal clicks
    private void PlayClickSound()
    {
        if (audioSource != null && clickClip != null)
        {
            audioSource.PlayOneShot(clickClip);
        }
    }

    // Play double-click sound
    private void PlayDoubleClickSound()
    {
        if (audioSource != null && doubleClickClip != null)
        {
            audioSource.PlayOneShot(doubleClickClip);
        }
    }

    // Cause vibration on double-click
    private void Vibrate()
    {
        Handheld.Vibrate();
    }

    // Check if the image is a grandchild (leaf node)
    private bool IsGrandchild(Image image)
    {
        var hierarchy = FindHierarchy(image);
        return hierarchy != null && FindParentHierarchyRecursive(this.hierarchy, image) != null && hierarchy.children.Count == 0;
    }

    // Check if the image is part of the hierarchy
    private bool IsPartOfHierarchy(Image image)
    {
        return FindHierarchy(image) != null;
    }

    // Populate the hierarchy recursively
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

    // Toggle the state of an image
    private void ToggleImageState(Image image)
    {
        if (image == null)
        {
            return;
        }

        if (image.color == doubleClickColor)
        {
            bool anySiblingSelected = false;
            Transform parent = image.transform.parent;
            foreach (Transform sibling in parent)
            {
                Image siblingImage = sibling.GetComponent<Image>();
                if (siblingImage != image && siblingImage != null && siblingImage.color == doubleClickColor)
                {
                    anySiblingSelected = true;
                    break;
                }
            }

            // Determine if the parent should be removed based on siblings' selection status
            bool removeParent = !anySiblingSelected;
            if (dataRecorder != null)
            {
                dataRecorder.UnrecordFlavor(image.name, image.transform.parent?.name, removeParent);
            }
            RemoveDoubleClickIndicator(image);
            ScaleImage(image, false); // Unscale on unselect
        }
        else
        {
            var hierarchy = FindHierarchy(image);
            if (hierarchy != null)
            {
                if (!IsGrandchild(image))
                {
                    DisableSiblingsOfParent(image); // Disable siblings of the parent
                    EnableImage(image); // Enable itself and its children
                    foreach (var childHierarchy in hierarchy.children)
                    {
                        EnableImage(childHierarchy.image);
                    }
                }
                else
                {
                    ScaleImage(image, false); // Handle scaling of grandchild on single click
                    RemoveDoubleClickIndicator(image); // Remove visual indicator if previously double-clicked
                }
            }
        }
    }

    // Disable siblings of the parent image
    private void DisableSiblingsOfParent(Image image)
    {
        var parentHierarchy = FindParentHierarchy(image);
        if (parentHierarchy != null)
        {
            foreach (var sibling in parentHierarchy.children)
            {
                if (sibling.image != image)
                {
                    DarkenAndDisable(sibling.image);
                }
            }
        }
    }

    // Find the parent hierarchy of an image
    private ChildHierarchy FindParentHierarchy(Image image)
    {
        return FindParentHierarchyRecursive(this.hierarchy, image);
    }

    // Recursively find the parent hierarchy
    private ChildHierarchy FindParentHierarchyRecursive(ChildHierarchy parent, Image image)
    {
        foreach (var child in parent.children)
        {
            if (child.image == image)
            {
                return parent;
            }

            var result = FindParentHierarchyRecursive(child, image);
            if (result != null)
            {
                return result;
            }
        }
        return null;
    }

    // Apply darken and disable to hierarchy recursively
    private void ApplyDarkenAndDisableToHierarchy(List<ChildHierarchy> children)
    {
        foreach (var childHierarchy in children)
        {
            DarkenAndDisable(childHierarchy.image);
            ApplyDarkenAndDisableToHierarchy(childHierarchy.children);
        }
    }

    // Darken and disable an image
    private void DarkenAndDisable(Image image)
    {
        if (image != null)
        {
            image.color = darkenColor;
            Collider2D collider = image.GetComponent<Collider2D>();
            if (collider != null)
            {
                collider.enabled = false;
            }
        }
    }

    // Enable an image
    private void EnableImage(Image image)
    {
        if (image != null)
        {
            image.color = Color.white;
            Collider2D collider = image.GetComponent<Collider2D>();
            if (collider != null)
            {
                collider.enabled = true;
            }
        }
    }

    // Scale an image up or down
    private void ScaleImage(Image image, bool alwaysScaleUp)
    {
        if (image != null)
        {
            if (!originalScales.ContainsKey(image))
            {
                originalScales[image] = image.rectTransform.localScale;
            }
            if (alwaysScaleUp || image.rectTransform.localScale == originalScales[image])
            {
                image.rectTransform.localScale = originalScales[image] * 1.2f; // Scale up by 20%
            }
            else
            {
                image.rectTransform.localScale = originalScales[image]; // Revert to original scale
            }
        }
    }

    // Apply double-click indicator to an image
    private void ApplyDoubleClickIndicator(Image image)
    {
        if (image != null)
        {
            image.color = doubleClickColor; // Change color to indicate double-click
        }
    }

    // Remove double-click indicator from an image
    private void RemoveDoubleClickIndicator(Image image)
    {
        if (image != null && originalScales.ContainsKey(image) && image.rectTransform.localScale == originalScales[image] * 1.2f)
        {
            image.color = Color.white; // Revert color if scaled by single click
        }
    }

    // Find the hierarchy of an image
    private ChildHierarchy FindHierarchy(Image image)
    {
        return FindHierarchyRecursive(hierarchy, image);
    }

    // Recursively find the hierarchy of an image
    private ChildHierarchy FindHierarchyRecursive(ChildHierarchy hierarchy, Image image)
    {
        if (hierarchy.image == image)
        {
            return hierarchy;
        }

        foreach (var childHierarchy in hierarchy.children)
        {
            var foundHierarchy = FindHierarchyRecursive(childHierarchy, image);
            if (foundHierarchy != null)
            {
                return foundHierarchy;
            }
        }
        return null;
    }
}
