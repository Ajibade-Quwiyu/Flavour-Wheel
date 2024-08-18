using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI.Extensions; // Ensure this is the correct namespace

public class RadarChart : MonoBehaviour
{
    public enum DataType
    {
        MyThink,
        MyDetects,
        OtherThinks,
        OtherDetects,
        Neutral // Added Neutral data type
    }

    public UIManager uIManager;
    public DataType selectedDataType;

    public float spirit1;
    public float spirit2;
    public float spirit3;
    public float spirit4;
    public float spirit5;

    public RectTransform radarChart;
    public UILineRenderer lineRenderer;
    public float lineWidth = 2f;
    public Color lineColor = Color.red;

    private float minValue;
    private float maxValue;

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
        List<float> values = GetDataValues(selectedDataType);
        if (values.Count == 5)
        {
            spirit1 = values[0];
            spirit2 = values[4];
            spirit3 = values[3];
            spirit4 = values[2];
            spirit5 = values[1];

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

            UpdateRadarShape();
        }
    }

    private List<float> GetDataValues(DataType dataType)
    {
        List<float> values = new List<float>();

        switch (dataType)
        {
            case DataType.MyThink:
                values.Add(uIManager.GetLocalDataRating(0));
                values.Add(uIManager.GetLocalDataRating(1));
                values.Add(uIManager.GetLocalDataRating(2));
                values.Add(uIManager.GetLocalDataRating(3));
                values.Add(uIManager.GetLocalDataRating(4));
                break;
            case DataType.MyDetects:
                values.Add(uIManager.GetLocalDataFlavour(0));
                values.Add(uIManager.GetLocalDataFlavour(1));
                values.Add(uIManager.GetLocalDataFlavour(2));
                values.Add(uIManager.GetLocalDataFlavour(3));
                values.Add(uIManager.GetLocalDataFlavour(4));
                break;
            case DataType.OtherThinks:
                values.Add(uIManager.GetAverageRating(0));
                values.Add(uIManager.GetAverageRating(1));
                values.Add(uIManager.GetAverageRating(2));
                values.Add(uIManager.GetAverageRating(3));
                values.Add(uIManager.GetAverageRating(4));
                break;
            case DataType.OtherDetects:
                values.Add(uIManager.GetAverageFlavour(0));
                values.Add(uIManager.GetAverageFlavour(1));
                values.Add(uIManager.GetAverageFlavour(2));
                values.Add(uIManager.GetAverageFlavour(3));
                values.Add(uIManager.GetAverageFlavour(4));
                break;
            case DataType.Neutral:
                values.Add(1f); // Neutral values set to 1
                values.Add(1f); // Neutral values set to 1
                values.Add(1f); // Neutral values set to 1
                values.Add(1f); // Neutral values set to 1
                values.Add(1f); // Neutral values set to 1
                break;
        }

        return values;
    }

    private void UpdateRadarShape()
    {
        float[] values = { spirit1, spirit2, spirit3, spirit4, spirit5 };

        for (int i = 0; i < values.Length; i++)
        {
            values[i] = Mathf.InverseLerp(minValue, maxValue, values[i]);
        }

        int numPoints = values.Length;
        Vector2[] points = new Vector2[numPoints + 1];

        float width = radarChart.rect.width;
        float height = radarChart.rect.height;
        float centerX = width / 2;
        float centerY = height / 2;
        float maxRadius = Mathf.Min(width / 2, height / 2);

        for (int i = 0; i < numPoints; i++)
        {
            float angle = (i * (2 * Mathf.PI / numPoints)) + (Mathf.PI / 2);
            float radius = values[i] * maxRadius;
            float x = centerX + radius * Mathf.Cos(angle);
            float y = centerY + radius * Mathf.Sin(angle);
            points[i] = new Vector2(x, y);
        }

        points[numPoints] = points[0]; // Closing the radar chart loop

        // Setting up the line renderer
        lineRenderer.Points = points;
        lineRenderer.LineThickness = lineWidth;
        lineRenderer.color = lineColor;
        lineRenderer.SetAllDirty(); // Mark the line renderer as dirty to update the mesh
    }
}
