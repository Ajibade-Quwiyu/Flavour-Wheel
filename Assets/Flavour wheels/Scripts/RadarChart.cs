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

    public SpiritManager spiritManager;
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
            spirit2 = values[1];
            spirit3 = values[2];
            spirit4 = values[3];
            spirit5 = values[4];

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
                values.Add(spiritManager.GetLocalDataRating(0));
                values.Add(spiritManager.GetLocalDataRating(1));
                values.Add(spiritManager.GetLocalDataRating(2));
                values.Add(spiritManager.GetLocalDataRating(3));
                values.Add(spiritManager.GetLocalDataRating(4));
                break;
            case DataType.MyDetects:
                values.Add(spiritManager.GetLocalDataFlavour(0));
                values.Add(spiritManager.GetLocalDataFlavour(1));
                values.Add(spiritManager.GetLocalDataFlavour(2));
                values.Add(spiritManager.GetLocalDataFlavour(3));
                values.Add(spiritManager.GetLocalDataFlavour(4));
                break;
            case DataType.OtherThinks:
                values.Add(spiritManager.GetAverageRating(0));
                values.Add(spiritManager.GetAverageRating(1));
                values.Add(spiritManager.GetAverageRating(2));
                values.Add(spiritManager.GetAverageRating(3));
                values.Add(spiritManager.GetAverageRating(4));
                break;
            case DataType.OtherDetects:
                values.Add(spiritManager.GetAverageFlavour(0));
                values.Add(spiritManager.GetAverageFlavour(1));
                values.Add(spiritManager.GetAverageFlavour(2));
                values.Add(spiritManager.GetAverageFlavour(3));
                values.Add(spiritManager.GetAverageFlavour(4));
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
