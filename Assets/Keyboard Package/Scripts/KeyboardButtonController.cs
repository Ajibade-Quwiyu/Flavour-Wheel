using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class KeyboardButtonController : MonoBehaviour
{
    [SerializeField] Image containerBorderImage;
    [SerializeField] Image containerFillImage;
    [SerializeField] Image containerIcon;
    [SerializeField] TextMeshProUGUI containerText;
    [SerializeField] TextMeshProUGUI containerActionText;

    private float clickAnimationDuration = 0.15f;
    private float clickScaleMultiplier = 1.5f;

    private Vector3 originalScale;

    private void Start()
    {
        SetContainerBorderColor(ColorDataStore.GetKeyboardBorderColor());
        SetContainerFillColor(ColorDataStore.GetKeyboardFillColor());
        SetContainerTextColor(ColorDataStore.GetKeyboardTextColor());
        SetContainerActionTextColor(ColorDataStore.GetKeyboardActionTextColor());
        originalScale = transform.localScale;
    }

    public void SetContainerBorderColor(Color color) => containerBorderImage.color = color;
    public void SetContainerFillColor(Color color) => containerFillImage.color = color;
    public void SetContainerTextColor(Color color) => containerText.color = color;
    public void SetContainerActionTextColor(Color color)
    {
        containerActionText.color = color;
        containerIcon.color = color;
    }

    public void AddLetter()
    {
        StartCoroutine(ClickAnimation());
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddLetter(containerText.text);
        }
        else
        {
            Debug.Log(containerText.text + " is pressed");
        }
    }

    public void DeleteLetter()
    {
        StartCoroutine(ClickAnimation());
        if (GameManager.Instance != null)
        {
            GameManager.Instance.DeleteLetter();
        }
        else
        {
            Debug.Log("Last char deleted");
        }
    }

    public void SubmitWord()
    {
        StartCoroutine(ClickAnimation());
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SubmitWord();
        }
        else
        {
            Debug.Log("Submitted successfully!");
        }
    }

    private IEnumerator ClickAnimation()
    {
        // Scale up
        float elapsedTime = 0;
        while (elapsedTime < clickAnimationDuration / 2)
        {
            transform.localScale = Vector3.Lerp(originalScale, originalScale * clickScaleMultiplier, elapsedTime / (clickAnimationDuration / 2));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Scale down
        elapsedTime = 0;
        while (elapsedTime < clickAnimationDuration / 2)
        {
            transform.localScale = Vector3.Lerp(originalScale * clickScaleMultiplier, originalScale, elapsedTime / (clickAnimationDuration / 2));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.localScale = originalScale;
    }
}