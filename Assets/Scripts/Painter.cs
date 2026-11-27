using System.Collections.Generic;
using UnityEngine;

namespace NightRoom
{
    /// <summary>
    /// CPU上でTexture2Dに絵を描く小さな道具。画像素材なしで敵や背景を作るために使う。
    /// 座標は左下が原点(yが上向き)。
    /// </summary>
    public class Painter
    {
        public readonly int W, H;
        readonly Color[] px;

        public Painter(int w, int h) { W = w; H = h; px = new Color[w * h]; }

        public void Blend(int x, int y, Color c)
        {
            if (x < 0 || y < 0 || x >= W || y >= H || c.a <= 0f) return;
            int i = y * W + x;
            Color d = px[i];
            float a = c.a + d.a * (1f - c.a);
            if (a <= 0f) return;
            px[i] = new Color(
                (c.r * c.a + d.r * d.a * (1f - c.a)) / a,
                (c.g * c.a + d.g * d.a * (1f - c.a)) / a,
                (c.b * c.a + d.b * d.a * (1f - c.a)) / a, a);
        }

        public void Fill(Color c) { for (int i = 0; i < px.Length; i++) px[i] = c; }

        /// <summary>光の向き(lx,ly)から陰影をつけた楕円。</summary>
        public void Ellipse(float cx, float cy, float rx, float ry, Color lit, Color dark, float lx = -0.5f, float ly = 0.7f)
        {
            int x0 = Mathf.Max(0, (int)(cx - rx - 2)), x1 = Mathf.Min(W - 1, (int)(cx + rx + 2));
            int y0 = Mathf.Max(0, (int)(cy - ry - 2)), y1 = Mathf.Min(H - 1, (int)(cy + ry + 2));
            float soft = Mathf.Max(1f, Mathf.Min(rx, ry) * 0.04f);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float nx = (x - cx) / rx, ny = (y - cy) / ry;
                    float d = Mathf.Sqrt(nx * nx + ny * ny);
                    if (d > 1.05f) continue;
                    float edge = Mathf.Clamp01((1f - d) * Mathf.Min(rx, ry) / soft);
                    float t = Mathf.Clamp01(0.5f + 0.5f * (nx * lx + ny * ly)) * (1f - d * 0.45f);
                    Color c = Color.Lerp(dark, lit, t);
                    c.a *= edge;
                    Blend(x, y, c);
                }
        }

        public void Ellipse(float cx, float cy, float rx, float ry, Color c) { Ellipse(cx, cy, rx, ry, c, c); }

        /// <summary>やわらかい光の玉。中心ほど濃い。</summary>
        public void Glow(float cx, float cy, float r, Color c, float power = 2f)
        {
            int x0 = Mathf.Max(0, (int)(cx - r)), x1 = Mathf.Min(W - 1, (int)(cx + r));
            int y0 = Mathf.Max(0, (int)(cy - r)), y1 = Mathf.Min(H - 1, (int)(cy + r));
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) / r;
                    if (d >= 1f) continue;
                    Color k = c; k.a *= Mathf.Pow(1f - d, power);
                    Blend(x, y, k);
                }
        }

        public void Line(Vector2 a, Vector2 b, float thick, Color c)
        {
            float r = thick * 0.5f;
            int x0 = Mathf.Max(0, (int)(Mathf.Min(a.x, b.x) - r - 1)), x1 = Mathf.Min(W - 1, (int)(Mathf.Max(a.x, b.x) + r + 1));
            int y0 = Mathf.Max(0, (int)(Mathf.Min(a.y, b.y) - r - 1)), y1 = Mathf.Min(H - 1, (int)(Mathf.Max(a.y, b.y) + r + 1));
            Vector2 ab = b - a; float len2 = Mathf.Max(0.0001f, ab.sqrMagnitude);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    Vector2 p = new Vector2(x, y);
                    float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
                    float d = Vector2.Distance(p, a + ab * t);
                    if (d > r + 1f) continue;
                    Color k = c; k.a *= Mathf.Clamp01(r + 1f - d);
                    Blend(x, y, k);
                }
        }

        /// <summary>折れ線。太さは始点から終点へ細くなる。</summary>
        public void Poly(IList<Vector2> pts, float thickA, float thickB, Color c)
        {
            for (int i = 0; i < pts.Count - 1; i++)
            {
                float t = (float)i / Mathf.Max(1, pts.Count - 1);
                Line(pts[i], pts[i + 1], Mathf.Lerp(thickA, thickB, t), c);
            }
        }

        public void Fill(IList<Vector2> pts, Color c)
        {
            float minX = 1e9f, maxX = -1e9f, minY = 1e9f, maxY = -1e9f;
            foreach (var p in pts) { minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x); minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y); }
            int x0 = Mathf.Max(0, (int)minX), x1 = Mathf.Min(W - 1, (int)maxX + 1);
            int y0 = Mathf.Max(0, (int)minY), y1 = Mathf.Min(H - 1, (int)maxY + 1);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    bool inside = false;
                    for (int i = 0, j = pts.Count - 1; i < pts.Count; j = i++)
                    {
                        Vector2 a = pts[i], b = pts[j];
                        if (((a.y > y) != (b.y > y)) && (x < (b.x - a.x) * (y - a.y) / (b.y - a.y) + a.x)) inside = !inside;
                    }
                    if (inside) Blend(x, y, c);
                }
        }

        public void Triangle(Vector2 a, Vector2 b, Vector2 c, Color col) { Fill(new[] { a, b, c }, col); }

        /// <summary>二次ベジェを点列にする(角やツノ用)。</summary>
        public static List<Vector2> Curve(Vector2 a, Vector2 ctrl, Vector2 b, int n = 14)
        {
            var l = new List<Vector2>();
            for (int i = 0; i <= n; i++)
            {
                float t = (float)i / n, u = 1f - t;
                l.Add(u * u * a + 2f * u * t * ctrl + t * t * b);
            }
            return l;
        }

        /// <summary>不透明な部分にざらつきを足す。</summary>
        public void Grain(float amount, int seed)
        {
            var r = new System.Random(seed);
            for (int i = 0; i < px.Length; i++)
            {
                if (px[i].a <= 0.05f) continue;
                float n = ((float)r.NextDouble() - 0.5f) * amount;
                px[i] = new Color(Mathf.Clamp01(px[i].r + n), Mathf.Clamp01(px[i].g + n), Mathf.Clamp01(px[i].b + n), px[i].a);
            }
        }

        public Sprite ToSprite(float ppu, Vector2 pivot)
        {
            var t = new Texture2D(W, H, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Bilinear; t.wrapMode = TextureWrapMode.Clamp;
            t.SetPixels(px); t.Apply();
            return Sprite.Create(t, new Rect(0, 0, W, H), pivot, ppu);
        }
    }
}
