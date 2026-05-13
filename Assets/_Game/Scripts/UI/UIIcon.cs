using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Procedural icon Graphic — draws a small vector-style glyph for each
/// resource (and a few generic shapes for headers / actions).  Pixel-perfect
/// at any size, tinted via <see cref="Graphic.color"/>, zero asset
/// dependencies — survives missing TMP atlases / sprite atlases.
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public class UIIcon : MaskableGraphic
{
    public enum Kind
    {
        // Resource glyphs
        Scrap, Energy, Polymer, Data, Population, Nano,
        // Generic / action
        Chart, Mail, Settings, Menu, Build, Lock, Check, Cross, Plus, Minus,
        Bolt, Box, Flask, Gear, Person, Recycle, Diamond, Leaf,
    }

    public Kind kind = Kind.Scrap;

    [Range(0.3f, 1f)] public float fillPercent = 0.78f;  // how much of the rect the shape fills

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        var r = rectTransform.rect;
        Vector2 c   = r.center;
        float   sz  = Mathf.Min(r.width, r.height) * fillPercent * 0.5f;   // half-extent
        Color   col = color;

        switch (kind)
        {
            case Kind.Scrap:      DrawRecycle(vh, c, sz, col); break;
            case Kind.Energy:     DrawBolt(vh, c, sz, col); break;
            case Kind.Polymer:    DrawDiamond(vh, c, sz, col); break;
            case Kind.Data:       DrawDataBlock(vh, c, sz, col); break;
            case Kind.Population: DrawPerson(vh, c, sz, col); break;
            case Kind.Nano:       DrawAtom(vh, c, sz, col); break;

            case Kind.Chart:      DrawChart(vh, c, sz, col); break;
            case Kind.Mail:       DrawMail(vh, c, sz, col); break;
            case Kind.Settings:
            case Kind.Gear:       DrawGear(vh, c, sz, col); break;
            case Kind.Menu:       DrawMenu(vh, c, sz, col); break;
            case Kind.Build:      DrawBuild(vh, c, sz, col); break;
            case Kind.Lock:       DrawLock(vh, c, sz, col); break;
            case Kind.Check:      DrawCheck(vh, c, sz, col); break;
            case Kind.Cross:      DrawCross(vh, c, sz, col); break;
            case Kind.Plus:       DrawPlus(vh, c, sz, col); break;
            case Kind.Minus:      DrawMinus(vh, c, sz, col); break;
            case Kind.Bolt:       DrawBolt(vh, c, sz, col); break;
            case Kind.Box:        DrawBox(vh, c, sz, col); break;
            case Kind.Flask:      DrawFlask(vh, c, sz, col); break;
            case Kind.Person:     DrawPerson(vh, c, sz, col); break;
            case Kind.Recycle:    DrawRecycle(vh, c, sz, col); break;
            case Kind.Diamond:    DrawDiamond(vh, c, sz, col); break;
            case Kind.Leaf:       DrawLeaf(vh, c, sz, col); break;
        }
    }

    // ── Mesh primitives ───────────────────────────────────────────────────

    // Add a thick line segment (rectangle following the line) to vh
    static void Line(VertexHelper vh, Vector2 a, Vector2 b, float width, Color c)
    {
        Vector2 d = (b - a).normalized;
        Vector2 p = new Vector2(-d.y, d.x) * width * 0.5f;
        int s = vh.currentVertCount;
        vh.AddVert(a - p, c, Vector2.zero);
        vh.AddVert(a + p, c, Vector2.zero);
        vh.AddVert(b + p, c, Vector2.zero);
        vh.AddVert(b - p, c, Vector2.zero);
        vh.AddTriangle(s, s + 1, s + 2);
        vh.AddTriangle(s, s + 2, s + 3);
    }

    // Filled triangle
    static void Tri(VertexHelper vh, Vector2 a, Vector2 b, Vector2 cc, Color c)
    {
        int s = vh.currentVertCount;
        vh.AddVert(a,  c, Vector2.zero);
        vh.AddVert(b,  c, Vector2.zero);
        vh.AddVert(cc, c, Vector2.zero);
        vh.AddTriangle(s, s + 1, s + 2);
    }

    // Filled rect
    static void Rect(VertexHelper vh, Vector2 center, float w, float h, Color c)
    {
        Vector2 hs = new Vector2(w, h) * 0.5f;
        int s = vh.currentVertCount;
        vh.AddVert(center + new Vector2(-hs.x, -hs.y), c, Vector2.zero);
        vh.AddVert(center + new Vector2(-hs.x,  hs.y), c, Vector2.zero);
        vh.AddVert(center + new Vector2( hs.x,  hs.y), c, Vector2.zero);
        vh.AddVert(center + new Vector2( hs.x, -hs.y), c, Vector2.zero);
        vh.AddTriangle(s, s + 1, s + 2);
        vh.AddTriangle(s, s + 2, s + 3);
    }

    // Filled circle (fan)
    static void Circle(VertexHelper vh, Vector2 center, float r, Color c, int segs = 24)
    {
        int s = vh.currentVertCount;
        vh.AddVert(center, c, Vector2.zero);
        for (int i = 0; i <= segs; i++)
        {
            float a = (i / (float)segs) * Mathf.PI * 2f;
            vh.AddVert(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r, c, Vector2.zero);
        }
        for (int i = 0; i < segs; i++)
            vh.AddTriangle(s, s + 1 + i, s + 2 + i);
    }

    // Circle outline (ring)
    static void Ring(VertexHelper vh, Vector2 center, float rOuter, float rInner, Color c, int segs = 24)
    {
        int s = vh.currentVertCount;
        for (int i = 0; i <= segs; i++)
        {
            float a = (i / (float)segs) * Mathf.PI * 2f;
            Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            vh.AddVert(center + d * rInner, c, Vector2.zero);
            vh.AddVert(center + d * rOuter, c, Vector2.zero);
        }
        for (int i = 0; i < segs; i++)
        {
            int b = s + i * 2;
            vh.AddTriangle(b, b + 1, b + 3);
            vh.AddTriangle(b, b + 3, b + 2);
        }
    }

    // ── Icon shapes ───────────────────────────────────────────────────────

    static void DrawBolt(VertexHelper vh, Vector2 c, float s, Color col)
    {
        // Stylised lightning bolt: zig-zag of two filled triangles
        Vector2 p1 = c + new Vector2(-0.15f * s,  1.00f * s);
        Vector2 p2 = c + new Vector2( 0.45f * s,  0.10f * s);
        Vector2 p3 = c + new Vector2( 0.05f * s,  0.10f * s);
        Vector2 p4 = c + new Vector2( 0.15f * s, -1.00f * s);
        Vector2 p5 = c + new Vector2(-0.45f * s, -0.10f * s);
        Vector2 p6 = c + new Vector2(-0.05f * s, -0.10f * s);
        Tri(vh, p1, p2, p3, col);
        Tri(vh, p1, p3, p6, col);
        Tri(vh, p6, p3, p4, col);
        Tri(vh, p6, p4, p5, col);
    }

    static void DrawDiamond(VertexHelper vh, Vector2 c, float s, Color col)
    {
        Tri(vh, c + new Vector2(0,  s), c + new Vector2( s, 0), c + new Vector2(0, -s), col);
        Tri(vh, c + new Vector2(0,  s), c + new Vector2(0, -s), c + new Vector2(-s, 0), col);
    }

    static void DrawDataBlock(VertexHelper vh, Vector2 c, float s, Color col)
    {
        // Stacked thin rectangles — database / data silo look
        float w = s * 1.4f;
        for (int i = -1; i <= 1; i++)
        {
            float y = c.y + i * s * 0.5f;
            Rect(vh, new Vector2(c.x, y), w, s * 0.28f, col);
        }
    }

    static void DrawPerson(VertexHelper vh, Vector2 c, float s, Color col)
    {
        // Head (circle) + body (trapezoid via two triangles)
        Circle(vh, c + new Vector2(0, s * 0.45f), s * 0.35f, col, 16);
        Vector2 bl = c + new Vector2(-s * 0.55f, -s * 0.65f);
        Vector2 br = c + new Vector2( s * 0.55f, -s * 0.65f);
        Vector2 tl = c + new Vector2(-s * 0.35f,  s * 0.05f);
        Vector2 tr = c + new Vector2( s * 0.35f,  s * 0.05f);
        Tri(vh, bl, tl, tr, col);
        Tri(vh, bl, tr, br, col);
    }

    static void DrawAtom(VertexHelper vh, Vector2 c, float s, Color col)
    {
        // Nucleus + 3 orbital rings (just outlines)
        Circle(vh, c, s * 0.22f, col, 16);
        DrawEllipseRing(vh, c, s * 0.95f, s * 0.35f, 0f,                   col);
        DrawEllipseRing(vh, c, s * 0.95f, s * 0.35f, Mathf.PI / 3f,        col);
        DrawEllipseRing(vh, c, s * 0.95f, s * 0.35f, 2f * Mathf.PI / 3f,   col);
    }

    static void DrawEllipseRing(VertexHelper vh, Vector2 c, float rx, float ry, float rot, Color col)
    {
        const int segs = 28;
        float cosR = Mathf.Cos(rot), sinR = Mathf.Sin(rot);
        Vector2 Prev() => Vector2.zero;
        Vector2 last = Vector2.zero;
        bool first = true;
        for (int i = 0; i <= segs; i++)
        {
            float a = i / (float)segs * Mathf.PI * 2f;
            float x = Mathf.Cos(a) * rx;
            float y = Mathf.Sin(a) * ry;
            // rotate
            Vector2 p = c + new Vector2(x * cosR - y * sinR, x * sinR + y * cosR);
            if (!first) Line(vh, last, p, 1.5f, col);
            last = p;
            first = false;
        }
    }

    static void DrawRecycle(VertexHelper vh, Vector2 c, float s, Color col)
    {
        // 3 curved arrows arranged in a triangle — represented as 3 short angled lines + arrowheads
        const float r = 0.85f;
        for (int i = 0; i < 3; i++)
        {
            float a0 = (i / 3f) * Mathf.PI * 2f + Mathf.PI / 2f;
            float a1 = a0 + Mathf.PI * 0.45f;
            Vector2 p0 = c + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * s * r;
            Vector2 p1 = c + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * s * r;
            Line(vh, p0, p1, 2f, col);
            // arrowhead at p1
            Vector2 dir = (p1 - p0).normalized;
            Vector2 perp = new Vector2(-dir.y, dir.x);
            Tri(vh, p1 + dir * s * 0.18f, p1 + perp * s * 0.18f, p1 - perp * s * 0.18f, col);
        }
    }

    static void DrawGear(VertexHelper vh, Vector2 c, float s, Color col)
    {
        // Outer body — 8-tooth gear (octagon outline)
        const int teeth = 8;
        for (int i = 0; i < teeth; i++)
        {
            float a = (i / (float)teeth) * Mathf.PI * 2f;
            Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            Vector2 dp = new Vector2(-d.y, d.x);
            Vector2 outer = c + d * s;
            Rect(vh, outer, s * 0.28f, s * 0.28f, col);
        }
        Ring(vh, c, s * 0.78f, s * 0.50f, col, 20);
    }

    static void DrawChart(VertexHelper vh, Vector2 c, float s, Color col)
    {
        // Bar chart — 3 ascending bars
        float w = s * 0.32f;
        Rect(vh, c + new Vector2(-s * 0.55f, -s * 0.35f), w, s * 0.45f, col);
        Rect(vh, c + new Vector2(           0, -s * 0.20f), w, s * 0.75f, col);
        Rect(vh, c + new Vector2( s * 0.55f, -s * 0.05f), w, s * 1.05f, col);
    }

    static void DrawMail(VertexHelper vh, Vector2 c, float s, Color col)
    {
        // Envelope outline
        Vector2 tl = c + new Vector2(-s, s * 0.6f);
        Vector2 tr = c + new Vector2( s, s * 0.6f);
        Vector2 bl = c + new Vector2(-s, -s * 0.6f);
        Vector2 br = c + new Vector2( s, -s * 0.6f);
        Line(vh, tl, tr, 1.5f, col);
        Line(vh, tr, br, 1.5f, col);
        Line(vh, br, bl, 1.5f, col);
        Line(vh, bl, tl, 1.5f, col);
        // V flap
        Line(vh, tl, c, 1.5f, col);
        Line(vh, tr, c, 1.5f, col);
    }

    static void DrawMenu(VertexHelper vh, Vector2 c, float s, Color col)
    {
        // 3 horizontal lines
        float w = s * 1.5f, h = s * 0.18f;
        Rect(vh, c + new Vector2(0,  s * 0.55f), w, h, col);
        Rect(vh, c,                            w, h, col);
        Rect(vh, c + new Vector2(0, -s * 0.55f), w, h, col);
    }

    static void DrawBuild(VertexHelper vh, Vector2 c, float s, Color col)
    {
        // Pickaxe — diagonal handle + curved head
        Line(vh, c + new Vector2(-s * 0.8f, -s * 0.8f), c + new Vector2(s * 0.6f, s * 0.6f), 2f, col);
        // Head
        Vector2 head = c + new Vector2(s * 0.6f, s * 0.6f);
        Line(vh, head + new Vector2(-s * 0.4f, s * 0.4f), head + new Vector2(s * 0.4f, -s * 0.4f), 2f, col);
    }

    static void DrawLock(VertexHelper vh, Vector2 c, float s, Color col)
    {
        // Body + shackle
        Rect(vh, c + new Vector2(0, -s * 0.25f), s * 1.2f, s * 1.0f, col);
        Ring(vh, c + new Vector2(0,  s * 0.30f), s * 0.55f, s * 0.30f, col, 18);
        // Mask off the bottom of the ring
        Rect(vh, c + new Vector2(0, s * 0.10f), s * 1.1f, s * 0.4f, col);
    }

    static void DrawCheck(VertexHelper vh, Vector2 c, float s, Color col)
    {
        Line(vh, c + new Vector2(-s * 0.7f, -s * 0.05f), c + new Vector2(-s * 0.15f, -s * 0.55f), 2.2f, col);
        Line(vh, c + new Vector2(-s * 0.15f, -s * 0.55f), c + new Vector2(s * 0.7f, s * 0.50f), 2.2f, col);
    }

    static void DrawCross(VertexHelper vh, Vector2 c, float s, Color col)
    {
        Line(vh, c + new Vector2(-s * 0.6f, -s * 0.6f), c + new Vector2(s * 0.6f, s * 0.6f), 2f, col);
        Line(vh, c + new Vector2(-s * 0.6f,  s * 0.6f), c + new Vector2(s * 0.6f, -s * 0.6f), 2f, col);
    }

    static void DrawPlus(VertexHelper vh, Vector2 c, float s, Color col)
    {
        Rect(vh, c, s * 1.4f, s * 0.28f, col);
        Rect(vh, c, s * 0.28f, s * 1.4f, col);
    }

    static void DrawMinus(VertexHelper vh, Vector2 c, float s, Color col)
    {
        Rect(vh, c, s * 1.4f, s * 0.28f, col);
    }

    static void DrawBox(VertexHelper vh, Vector2 c, float s, Color col)
    {
        // Box outline + center stripe
        Ring(vh, c, s * 0.95f, s * 0.75f, col, 4);
        Rect(vh, c, s * 1.7f, s * 0.18f, col);
    }

    static void DrawFlask(VertexHelper vh, Vector2 c, float s, Color col)
    {
        // Flask shape — narrow neck + wider body (triangle approximation)
        Rect(vh, c + new Vector2(0, s * 0.5f), s * 0.4f, s * 0.55f, col);  // neck
        Tri(vh, c + new Vector2(-s * 0.2f,  s * 0.2f), c + new Vector2( s * 0.2f, s * 0.2f),
                c + new Vector2( s * 0.8f, -s * 0.7f), col);
        Tri(vh, c + new Vector2(-s * 0.2f,  s * 0.2f), c + new Vector2( s * 0.8f, -s * 0.7f),
                c + new Vector2(-s * 0.8f, -s * 0.7f), col);
        Rect(vh, c + new Vector2(0, -s * 0.7f), s * 1.6f, s * 0.18f, col);
    }

    static void DrawLeaf(VertexHelper vh, Vector2 c, float s, Color col)
    {
        // Leaf shape — pointed oval
        Tri(vh, c + new Vector2(0, s), c + new Vector2(s * 0.7f, 0),    c + new Vector2(0, -s), col);
        Tri(vh, c + new Vector2(0, s), c + new Vector2(-s * 0.7f, 0),   c + new Vector2(0, -s), col);
        Line(vh, c + new Vector2(0, s), c + new Vector2(0, -s), 1.2f,
             new Color(col.r * 0.5f, col.g * 0.5f, col.b * 0.5f, col.a));
    }
}
