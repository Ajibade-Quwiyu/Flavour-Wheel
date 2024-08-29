using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI.Extensions; // Ensure this is the correct namespace

public class RadarChart : MonoBehaviour
{
    public enum DataType
    {
        MyThink, MyDetects, OtherThinks, OtherDetects, Neutral
    }

    public UIManager uIManager;
    public DataType selectedDataType;
    public float[] spirits = new float[5];
    public RectTransform radarChart;
    public UILineRenderer lineRenderer;
    public float lineWidth = 2f;
    public Color lineColor = Color.red;

    private const float MIN_THINK = 0f;
    private const float MAX_THINK = 5f;
    private const float MIN_DETECT = 0f;
    private const float MAX_DETECT = 7f;

    void Start()
    {
        UpdateRadarChart();
    }

    void Update()
    {
        UpdateRadarChart();
    }

    public void UpdateRadarChart()
    {
        var values = GetDataValues(selectedDataType);
        if (values.Count == 5)
        {
            SetSpiritValues(values);
            UpdateRadarShape();
        }
    }

    private void SetSpiritValues(List<float> values)
    {
        // Mapping the spirit values in the order defined in the original method
        float[] mappedValues = { values[0], values[4], values[3], values[2], values[1] };
        for (int i = 0; i < spirits.Length; i++)
        {
            spirits[i] = mappedValues[i];
        }
    }

    private List<float> GetDataValues(DataType dataType)
    {
        float[] data = new float[5];
        switch (dataType)
        {
            case DataType.MyThink:
            case DataType.OtherThinks:
                data = GetLocalData(dataType == DataType.MyThink ? uIManager.GetLocalDataRating : uIManager.GetAverageRating);
                return NormalizeData(data, MIN_THINK, MAX_THINK);
            case DataType.MyDetects:
            case DataType.OtherDetects:
                data = GetLocalData(dataType == DataType.MyDetects ? uIManager.GetLocalDataFlavour : uIManager.GetAverageFlavour);
                return NormalizeData(data, MIN_DETECT, MAX_DETECT);
            case DataType.Neutral:
                return Enumerable.Repeat(1f, 5).ToList(); // All values set to 1 for Neutral
        }
        return data.ToList();
    }

    private float[] GetLocalData(System.Func<int, float> getDataMethod)
    {
        return new float[]
        {
            getDataMethod(0),
            getDataMethod(1),
            getDataMethod(2),
            getDataMethod(3),
            getDataMethod(4)
        };
    }

    private List<float> NormalizeData(float[] data, float min, float max)
    {
        return data.Select(v => Mathf.Clamp(v, min, max)).ToList();
    }

    private void UpdateRadarShape()
    {
        float minValue, maxValue;
        if (selectedDataType == DataType.MyThink || selectedDataType == DataType.OtherThinks)
        {
            minValue = MIN_THINK;
            maxValue = MAX_THINK;
        }
        else if (selectedDataType == DataType.MyDetects || selectedDataType == DataType.OtherDetects)
        {
            minValue = MIN_DETECT;
            maxValue = MAX_DETECT;
        }
        else // Neutral
        {
            minValue = 0;
            maxValue = 1;
        }

        float[] normalizedValues = spirits.Select(v => Mathf.InverseLerp(minValue, maxValue, v)).ToArray();
        int numPoints = normalizedValues.Length;
        Vector2[] points = new Vector2[numPoints + 1];
        float width = radarChart.rect.width;
        float height = radarChart.rect.height;
        float centerX = width / 2;
        float centerY = height / 2;
        float maxRadius = Mathf.Min(width / 2, height / 2);

        for (int i = 0; i < numPoints; i++)
        {
            float angle = (i * (2 * Mathf.PI / numPoints)) + (Mathf.PI / 2);
            float radius = normalizedValues[i] * maxRadius;
            points[i] = new Vector2(centerX + radius * Mathf.Cos(angle), centerY + radius * Mathf.Sin(angle));
        }
        points[numPoints] = points[0]; // Closing the radar chart loop

        // Setting up the line renderer
        lineRenderer.Points = points;
        lineRenderer.LineThickness = lineWidth;
        lineRenderer.color = lineColor;
        lineRenderer.SetAllDirty(); // Mark the line renderer as dirty to update the mesh
    }
}