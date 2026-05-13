using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Procedural rounded-rectangle Graphic.
/// No sprites, no textures — generates a clean mesh at any size.
/// Works as a drop-in replacement for Image backgrounds everywhere.
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public class RoundedRect : MaskableGraphic
{
    [Min(0f)]        public float cornerRadius   = 10f;
    [Range(3, 16)]   public int   cornerSegments = 8;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect  r   = GetPixelAdjustedRect();
        float rad = Mathf.Min(
            Mathf.Max(cornerRadius, 0.01f),
            r.width  * 0.5f,
            r.height * 0.5f);

        // ── Perimeter points (clockwise, starting at top-right corner) ────────
        var pts = new List<Vector2>(4 * (cornerSegments + 1));

        // Each corner: (arc center, starting angle in degrees)
        // Clockwise means angle DECREASES from start by 90° over the arc.
        (Vector2 c, float startDeg)[] corners =
        {
            (new Vector2(r.xMax - rad, r.yMax - rad),  90f),  // top-right
            (new Vector2(r.xMax - rad, r.yMin + rad),   0f),  // bottom-right
            (new Vector2(r.xMin + rad, r.yMin + rad), -90f),  // bottom-left
            (new Vector2(r.xMin + rad, r.yMax - rad), 180f),  // top-left
        };

        float step = 90f / cornerSegments;
        foreach (var (c, startDeg) in corners)
            for (int i = 0; i <= cornerSegments; i++)
            {
                float a = (startDeg - step * i) * Mathf.Deg2Rad;
                pts.Add(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rad);
            }

        // ── Vertices ──────────────────────────────────────────────────────────

        // Center (index 0)
        var cv = UIVertex.simpleVert;
        cv.position = r.center;
        cv.color    = color;
        cv.uv0      = new Vector2(0.5f, 0.5f);
        vh.AddVert(cv);

        // Perimeter (indices 1 … count)
        for (int i = 0; i < pts.Count; i++)
        {
            var v = UIVertex.simpleVert;
            v.position = pts[i];
            v.color    = color;
            v.uv0      = new Vector2(0.5f, 0.5f);
            vh.AddVert(v);
        }

        // ── Fan triangles from center ─────────────────────────────────────────
        int n = pts.Count;
        for (int i = 0; i < n - 1; i++)
            vh.AddTriangle(0, i + 1, i + 2);
        vh.AddTriangle(0, n, 1);   // close the fan
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        SetVerticesDirty();
    }
#endif
}
