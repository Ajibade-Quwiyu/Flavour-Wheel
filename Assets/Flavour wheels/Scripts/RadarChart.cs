using UnityEngine;
using UnityEngine.UI;

public class UI_RadarChart : MonoBehaviour
{
    public RectTransform radarChart;
    public float spirit1_value1 = 1f;
    public float spirit1_value2 = 1f;
    public float spirit1_value3 = 1f;
    public float spirit1_value4 = 1f;
    public float spirit1_value5 = 1f;

    public float spirit2_value1 = 1f;
    public float spirit2_value2 = 1f;
    public float spirit2_value3 = 1f;
    public float spirit2_value4 = 1f;
    public float spirit2_value5 = 1f;

    public float spirit3_value1 = 1f;
    public float spirit3_value2 = 1f;
    public float spirit3_value3 = 1f;
    public float spirit3_value4 = 1f;
    public float spirit3_value5 = 1f;

    public float spirit4_value1 = 1f;
    public float spirit4_value2 = 1f;
    public float spirit4_value3 = 1f;
    public float spirit4_value4 = 1f;
    public float spirit4_value5 = 1f;

    public float spirit5_value1 = 1f;
    public float spirit5_value2 = 1f;
    public float spirit5_value3 = 1f;
    public float spirit5_value4 = 1f;
    public float spirit5_value5 = 1f;

    private RawImage radarImage;

    void Start()
    {
        radarImage = radarChart.GetComponent<RawImage>();
        UpdateRadarChart();
    }

    void Update()
    {
        UpdateRadarChart();
    }

    void UpdateRadarChart()
    {
        Texture2D texture = new Texture2D(256, 256, TextureFormat.RGBA32, false);
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                texture.SetPixel(x, y, Color.clear);
            }
        }

        // Draw each spirit radar chart
        DrawSpiritRadarChart(texture, new float[] { spirit1_value1, spirit1_value2, spirit1_value3, spirit1_value4, spirit1_value5 }, Color.red);
        DrawSpiritRadarChart(texture, new float[] { spirit2_value1, spirit2_value2, spirit2_value3, spirit2_value4, spirit2_value5 }, Color.green);
        DrawSpiritRadarChart(texture, new float[] { spirit3_value1, spirit3_value2, spirit3_value3, spirit3_value4, spirit3_value5 }, Color.blue);
        DrawSpiritRadarChart(texture, new float[] { spirit4_value1, spirit4_value2, spirit4_value3, spirit4_value4, spirit4_value5 }, Color.yellow);
        DrawSpiritRadarChart(texture, new float[] { spirit5_value1, spirit5_value2, spirit5_value3, spirit5_value4, spirit5_value5 }, Color.magenta);

        texture.Apply();
        radarImage.texture = texture;
    }

    void DrawSpiritRadarChart(Texture2D texture, float[] values, Color color)
    {
        Vector2[] vertices = new Vector2[6];
        float angle = 360f / 5;

        vertices[0] = Vector2.zero; // Center of the radar chart
        for (int i = 0; i < 5; i++)
        {
            vertices[i + 1] = GetPoint(values[i], angle * i);
        }

        for (int i = 1; i < vertices.Length; i++)
        {
            Vector2 start = vertices[i];
            Vector2 end = vertices[(i % 5) + 1];
            DrawLine(texture, start, end, color);
        }
    }

    Vector2 GetPoint(float value, float angle)
    {
        float rad = Mathf.Deg2Rad * angle;
        return new Vector2(value * Mathf.Cos(rad), value * Mathf.Sin(rad));
    }

    void DrawLine(Texture2D texture, Vector2 start, Vector2 end, Color color)
    {
        int width = texture.width;
        int height = texture.height;
        start = new Vector2((start.x + 1) * 0.5f * width, (start.y + 1) * 0.5f * height);
        end = new Vector2((end.x + 1) * 0.5f * width, (end.y + 1) * 0.5f * height);
        int x0 = (int)start.x;
        int y0 = (int)start.y;
        int x1 = (int)end.x;
        int y1 = (int)end.y;

        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            texture.SetPixel(x0, y0, color);
            if (x0 == x1 && y0 == y1) break;
            int e2 = err * 2;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }
}
