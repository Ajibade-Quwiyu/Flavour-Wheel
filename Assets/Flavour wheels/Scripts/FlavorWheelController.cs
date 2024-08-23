using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

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
    public TMP_Text numberText; public float animationDuration = 1f, endpos = 50f;
    private Color darkenColor = new Color(0.35f, 0.35f, 0.35f, 1f), doubleClickColor = Color.yellow;
    private float doubleClickTime = 0.3f, lastClickTime;
    private bool isDoubleClick;
    public FlavorWheelDataRecorder dataRecorder;
    public AudioClip clickClip, doubleClickClip; private AudioSource audioSource;

    public void Select(int increment = 1)
    {
        currentNumber += increment;
        UpdateNumberText();
        StartCoroutine(AnimateNumber("+" + increment));
    }

    public void Unselect()
    {
        currentNumber -= 1;
        UpdateNumberText();
        StartCoroutine(AnimateNumber("-1"));
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

    void Start()
    {
        hierarchy = new ChildHierarchy { image = GetComponent<Image>() };
        PopulateHierarchy(transform, hierarchy);
        ApplyDarkenAndDisableToHierarchy(hierarchy.children);
        UpdateNumberText();

        audioSource = Camera.main.GetComponent<AudioSource>();
    }

    void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

        if (hit.collider?.GetComponent<Image>() is Image image && IsPartOfHierarchy(image))
        {
            float timeSinceLastClick = Time.time - lastClickTime;
            isDoubleClick = timeSinceLastClick <= doubleClickTime;

            if (isDoubleClick)
                HandleDoubleClick(image);
            else
                StartCoroutine(HandleSingleClick(image));

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

        Select(2);
        ScaleImage(image, true);
        ApplyDoubleClickIndicator(image);

        dataRecorder?.RecordFlavor(image.name, image.transform.parent?.name);

        PlayDoubleClickSound();
        Handheld.Vibrate();
    }
    private void PlayClickSound()
    {
        audioSource.PlayOneShot(clickClip);
    }
    private void PlayDoubleClickSound()
    {
        audioSource.PlayOneShot(doubleClickClip);
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

    private void ToggleImageState(Image image)
    {
        if (image == null) return;

        if (image.color == doubleClickColor)
        {
            UnselectImage(image);
        }
        else
        {
            SelectImage(image);
        }
    }

    private void UnselectImage(Image image)
    {
        bool anySiblingSelected = image.transform.parent.GetComponentsInChildren<Image>()
            .Any(siblingImage => siblingImage != image && siblingImage.color == doubleClickColor);

        dataRecorder?.UnrecordFlavor(image.name, image.transform.parent?.name, !anySiblingSelected);
        RemoveDoubleClickIndicator(image);
        ScaleImage(image, false);
        Unselect();
    }

    private void SelectImage(Image image)
    {
        var hierarchy = FindHierarchy(image);
        if (hierarchy == null) return;

        if (!IsGrandchild(image))
        {
            DisableSiblingsOfParent(image);
            EnableImage(image);
            hierarchy.children.ForEach(child => EnableImage(child.image));
        }
        else
        {
            ScaleImage(image, false);
            RemoveDoubleClickIndicator(image);
            Select(1);
        }
    }

    private void DisableSiblingsOfParent(Image image)
    {
        FindParentHierarchy(image)?.children
            .Where(sibling => sibling.image != image)
            .ToList()
            .ForEach(sibling => DarkenAndDisable(sibling.image));
    }

    private ChildHierarchy FindParentHierarchy(Image image) =>
        FindParentHierarchyRecursive(hierarchy, image);

    private ChildHierarchy FindParentHierarchyRecursive(ChildHierarchy parent, Image image)
    {
        return parent.children.FirstOrDefault(child => child.image == image) != null
            ? parent
            : parent.children.Select(child => FindParentHierarchyRecursive(child, image))
                             .FirstOrDefault(result => result != null);
    }

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
        image.GetComponent<Collider2D>().enabled = false;
    }

    private void EnableImage(Image image)
    {
        if (image == null) return;
        image.color = Color.white;
        image.GetComponent<Collider2D>().enabled = true;
    }

    private void ScaleImage(Image image, bool alwaysScaleUp)
    {
        if (image == null) return;

        if (!originalScales.ContainsKey(image))
            originalScales[image] = image.rectTransform.localScale;

        image.rectTransform.localScale = (alwaysScaleUp || image.rectTransform.localScale == originalScales[image])
            ? originalScales[image] * 1.2f
            : originalScales[image];
    }

    private void ApplyDoubleClickIndicator(Image image)
    {
        if (image != null) image.color = doubleClickColor;
    }

    private void RemoveDoubleClickIndicator(Image image)
    {
        if (image != null && originalScales.TryGetValue(image, out var originalScale)
            && image.rectTransform.localScale == originalScale * 1.2f)
        {
            image.color = Color.white;
        }
    }

    private ChildHierarchy FindHierarchy(Image image) => FindHierarchyRecursive(hierarchy, image);

    private ChildHierarchy FindHierarchyRecursive(ChildHierarchy hierarchy, Image image)
    {
        if (hierarchy.image == image) return hierarchy;
        return hierarchy.children.Select(child => FindHierarchyRecursive(child, image))
                                 .FirstOrDefault(found => found != null);
    }
}
