using UnityEngine;
using TMPro;
using System.Collections;

public class SelectionManager : MonoBehaviour
{
    public TMP_Text numberText;  // Reference to your TMP_Text component
    private int currentNumber = 0;
    public float animationDuration = 1f; // Duration of the animation

    void Start()
    {
        UpdateNumberText();
    }

    public void Select()
    {
        currentNumber += 1;
        UpdateNumberText();
        StartCoroutine(AnimateNumber("+1"));
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
        changeTMP.transform.localPosition = numberText.transform.localPosition;

        Vector3 startPosition = changeTMP.transform.localPosition;
        Vector3 endPosition = startPosition + new Vector3(0, 50, 0);
        Color startColor = changeTMP.color;
        Color endColor = startColor;
        endColor.a = 0;

        float elapsedTime = 0;

        while (elapsedTime < animationDuration)
        {
            float t = elapsedTime / animationDuration;
            changeTMP.transform.localPosition = Vector3.Lerp(startPosition, endPosition, t);
            changeTMP.color = Color.Lerp(startColor, endColor, t);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        Destroy(changeTMP.gameObject);
    }
}
