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

    public float lineWidth = 2f, minValue, maxValue;
    public Color lineColor = Color.red;

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
            SetMinMaxValues(values);
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

    private void SetMinMaxValues(List<float> values)
    {
        if (selectedDataType == DataType.Neutral)
        {
            minValue = 0;
            maxValue = 1;
        }
        else
        {
            minValue = values.Min();
            maxValue = values.Max();
        }
    }

    private List<float> GetDataValues(DataType dataType)
    {
        float[] data = new float[5];

        switch (dataType)
        {
            case DataType.MyThink:
                data = GetLocalData(uIManager.GetLocalDataRating);
                break;
            case DataType.MyDetects:
                data = GetLocalData(uIManager.GetLocalDataFlavour);
                break;
            case DataType.OtherThinks:
                data = GetLocalData(uIManager.GetAverageRating);
                break;
            case DataType.OtherDetects:
                data = GetLocalData(uIManager.GetAverageFlavour);
                break;
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

    private void UpdateRadarShape()
    {
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
