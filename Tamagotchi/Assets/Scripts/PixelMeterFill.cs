using UnityEngine;
using UnityEngine.UI;

/// <summary>Pixel-stepped, highlighted fill. The end cap moves with the current value.</summary>
[AddComponentMenu("UI/Pixel Meter Fill")]
public sealed class PixelMeterFill : Image
{
    protected override void OnPopulateMesh(VertexHelper vertices)
    {
        vertices.Clear();
        var rect = GetPixelAdjustedRect();
        float width = rect.width * Mathf.Clamp01(fillAmount);
        if (width <= 0 || rect.height <= 0) return;
        const int rows = 12;
        float rowHeight = rect.height / rows;
        float cap = Mathf.Min(rect.height * 0.24f, width * 0.25f);
        for (int row = 0; row < rows; row++)
        {
            int distance = Mathf.Min(row, rows - 1 - row);
            float inset = distance == 0 ? cap : distance == 1 ? cap * 0.5f : distance == 2 ? cap * 0.25f : 0;
            float x0 = rect.xMin + inset, x1 = rect.xMin + width - inset;
            float y0 = rect.yMin + rowHeight * row, y1 = y0 + rowHeight;
            Color shade = row <= 1 ? Color.Lerp(color, Color.black, 0.22f) :
                row >= 10 ? Color.Lerp(color, Color.white, 0.5f) :
                row >= 8 ? Color.Lerp(color, Color.white, 0.22f) : color;
            Quad(vertices, x0, y0, x1, y1, shade);
            // A shaded terminal edge keeps the end looking carved rather than clipped.
            float edge = Mathf.Min(rowHeight * 0.65f, (x1 - x0) * 0.15f);
            if (row < 10) Quad(vertices, x1 - edge, y0, x1, y1, Color.Lerp(shade, Color.black, 0.12f));
        }
        // The tiny specular stair echoes the highlights on the supplied frame artwork.
        if (width >= rect.height * 1.25f)
        {
            Color shine = Color.Lerp(color, Color.white, 0.8f);
            float unit = rowHeight;
            Quad(vertices, rect.xMin + unit * 1.5f, rect.yMax - unit * 4, rect.xMin + unit * 3, rect.yMax - unit * 2, shine);
            Quad(vertices, rect.xMin + unit * 3, rect.yMax - unit * 3, rect.xMin + unit * 4.5f, rect.yMax - unit * 1.8f, shine);
        }
    }
    private static void Quad(VertexHelper mesh, float x0, float y0, float x1, float y1, Color shade)
    {
        if (x1 <= x0 || y1 <= y0) return;
        int index = mesh.currentVertCount;
        mesh.AddVert(new Vector3(x0, y0), shade, new Vector2(0.5f, 0.5f));
        mesh.AddVert(new Vector3(x0, y1), shade, new Vector2(0.5f, 0.5f));
        mesh.AddVert(new Vector3(x1, y1), shade, new Vector2(0.5f, 0.5f));
        mesh.AddVert(new Vector3(x1, y0), shade, new Vector2(0.5f, 0.5f));
        mesh.AddTriangle(index, index + 1, index + 2);
        mesh.AddTriangle(index, index + 2, index + 3);
    }
}
