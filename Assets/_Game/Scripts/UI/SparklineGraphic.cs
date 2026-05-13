using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Custom MaskableGraphic that draws a poly-line sparkline.
/// Set <see cref="data"/> and call <see cref="SetVerticesDirty"/> to refresh.
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public class SparklineGraphic : MaskableGraphic
{
    [HideInInspector] public float[] data;

    [Range(0.5f, 4f)] public float lineWidth = 1.5f;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (data == null || data.Length < 2) return;

        float min = float.MaxValue, max = float.MinValue;
        foreach (var v in data)
        {
            if (v < min) min = v;
            if (v > max) max = v;
        }
        if (Mathf.Approximately(min, max)) max = min + 1f;

        Rect r = rectTransform.rect;
        float w = r.width;
        float h = r.height;
        int   n = data.Length;

        for (int i = 0; i < n - 1; i++)
        {
            float x0 = r.xMin + (float)i       / (n - 1) * w;
            float x1 = r.xMin + (float)(i + 1) / (n - 1) * w;
            float y0 = r.yMin + (data[i]     - min) / (max - min) * h;
            float y1 = r.yMin + (data[i + 1] - min) / (max - min) * h;

            Vector2 dir  = new Vector2(x1 - x0, y1 - y0).normalized;
            Vector2 perp = new Vector2(-dir.y, dir.x) * lineWidth * 0.5f;

            // Fade from dim at left to bright at right
            float alpha0 = Mathf.Lerp(0.25f, 1f, (float)i       / (n - 1));
            float alpha1 = Mathf.Lerp(0.25f, 1f, (float)(i + 1) / (n - 1));
            Color c0 = new Color(color.r, color.g, color.b, color.a * alpha0);
            Color c1 = new Color(color.r, color.g, color.b, color.a * alpha1);

            int b = vh.currentVertCount;
            vh.AddVert(new Vector3(x0 - perp.x, y0 - perp.y), c0, Vector2.zero);
            vh.AddVert(new Vector3(x0 + perp.x, y0 + perp.y), c0, Vector2.zero);
            vh.AddVert(new Vector3(x1 + perp.x, y1 + perp.y), c1, Vector2.zero);
            vh.AddVert(new Vector3(x1 - perp.x, y1 - perp.y), c1, Vector2.zero);
            vh.AddTriangle(b, b + 1, b + 2);
            vh.AddTriangle(b, b + 2, b + 3);
        }
    }
}
