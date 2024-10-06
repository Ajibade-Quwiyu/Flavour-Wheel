using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CanvasScalerController : MonoBehaviour
{
    public Canvas[] canvases;
    public Slider slider;
    public TextMeshProUGUI valueText;

    private CanvasScaler[] canvasScalers;

    void Start()
    {
        // Initialize the array of CanvasScaler components
        canvasScalers = new CanvasScaler[canvases.Length];
        for (int i = 0; i < canvases.Length; i++)
        {
            canvasScalers[i] = canvases[i].GetComponent<CanvasScaler>();
            if (canvasScalers[i] == null)
            {
                Debug.LogWarning($"Canvas {i} does not have a CanvasScaler component!");
            }
        }

        // Set up the slider
        slider.minValue = 0f;
        slider.maxValue = 1f;

        // If there's at least one valid CanvasScaler, set the initial slider value
        if (canvasScalers.Length > 0 && canvasScalers[0] != null)
        {
            slider.value = canvasScalers[0].matchWidthOrHeight;
        }
        else
        {
            slider.value = 0.5f; // Default to middle if no valid CanvasScaler
        }

        // Add listener for when the slider value changes
        slider.onValueChanged.AddListener(UpdateCanvasScalers);

        // Initial update of the TextMeshPro
        UpdateValueText(slider.value);
    }

    void UpdateCanvasScalers(float value)
    {
        foreach (var scaler in canvasScalers)
        {
            if (scaler != null)
            {
                scaler.matchWidthOrHeight = value;
            }
        }

        UpdateValueText(value);
    }

    void UpdateValueText(float value)
    {
        if (valueText != null)
        {
            valueText.text = value.ToString();
        }
    }
}