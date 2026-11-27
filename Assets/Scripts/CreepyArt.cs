using System.Collections.Generic;
using UnityEngine;

namespace NightRoom
{
    /// <summary>敵・人形・背景などのスプライトをコードで生成する(画像素材は不要)。</summary>
    public static class CreepyArt
    {
        public const int MonW = 320, MonH = 480;
        const float PPU = 100f;
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        static Sprite Get(string key, System.Func<Sprite> make)
        {
            Sprite s;
            if (!cache.TryGetValue(key, out s)) { s = make(); cache[key] = s; }
            return s;
        }

        public static Sprite Square() { return Get("sq", () => { var p = new Painter(4, 4); p.Fill(Color.white); return p.ToSprite(4f, new Vector2(0.5f, 0.5f)); }); }

        public static Sprite Glow() { return Get("glow", () => { var p = new Painter(64, 64); p.Glow(32, 32, 31, Color.white, 2f); return p.ToSprite(64f, new Vector2(0.5f, 0.5f)); }); }

        /// <summary>懐中電灯用。中心は透明、外側は暗闇。</summary>
        public static Sprite LightMask()
        {
            return Get("mask", () =>
            {
                int n = 256; var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
                t.wrapMode = TextureWrapMode.Clamp; t.filterMode = FilterMode.Bilinear;
                var c = new Color[n * n];
                for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x, y), new Vector2(n * 0.5f, n * 0.5f)) / (n * 0.5f);
                        float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.14f, 0.55f, d)) * 0.985f;
                        c[y * n + x] = new Color(0, 0, 0, a);
                    }
                t.SetPixels(c); t.Apply();
                return Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
            });
        }

        /// <summary>扉の向こうの廊下。奥に向かう線と、床の汚れ。</summary>
        public static Sprite Hallway()
        {
            return Get("hall", () =>
            {
                var p = new Painter(220, 360);
                for (int y = 0; y < 360; y++)
                {
                    float t = y / 360f;
                    Color c = Color.Lerp(new Color(0.05f, 0.04f, 0.04f), new Color(0.09f, 0.13f, 0.18f), t);
                    for (int x = 0; x < 220; x++) p.Blend(x, y, c);
                }
                p.Fill(new[] { new Vector2(0, 0), new Vector2(220, 0), new Vector2(220, 120), new Vector2(0, 120) }, new Color(0.08f, 0.06f, 0.05f, 1f));
                for (int i = 0; i <= 8; i++) p.Line(new Vector2(110, 190), new Vector2(i * 27.5f, 0), 1.4f, new Color(0.5f, 0.6f, 0.75f, 0.12f));
                p.Fill(new[] { new Vector2(70, 190), new Vector2(150, 190), new Vector2(150, 262), new Vector2(70, 262) }, new Color(0.03f, 0.035f, 0.05f, 1f));
                p.Glow(110, 300, 90, new Color(0.7f, 0.75f, 0.6f, 0.07f), 1.5f);
                p.Grain(0.05f, 3);
                return p.ToSprite(PPU, new Vector2(0.5f, 0.5f));
            });
        }

        /// <summary>敵の目の位置(スプライト内ピクセル座標と半径)。光る目の重ね描き用。</summary>
        public static Vector3[] Eyes(int kind)
        {
            if (kind == 0) return new[] { new Vector3(128, 434, 9), new Vector3(166, 432, 8) };
            if (kind == 1) return new[] { new Vector3(160, 212, 40), new Vector3(70, 152, 11), new Vector3(250, 142, 11), new Vector3(92, 262, 9), new Vector3(232, 266, 9) };
            return new[] { new Vector3(135, 228, 12), new Vector3(185, 228, 12), new Vector3(160, 254, 9) };
        }

        public static Sprite Monster(int kind) { return Get("mon" + kind, () => kind == 0 ? Tall() : kind == 1 ? Round() : Horned()); }

        static readonly Color Ink = new Color(0.035f, 0.03f, 0.05f, 1f);
        static readonly Color InkLit = new Color(0.16f, 0.15f, 0.2f, 1f);
        static readonly Color Bone = new Color(0.84f, 0.8f, 0.68f, 1f);
        static readonly Color BoneDark = new Color(0.2f, 0.18f, 0.15f, 1f);
        static readonly Color Rim = new Color(0.4f, 0.5f, 0.7f, 0.22f);

        static void Teeth(Painter p, float x0, float x1, float y, int n, float hmin, float hmax, bool down, System.Random r)
        {
            for (int i = 0; i < n; i++)
            {
                float a = Mathf.Lerp(x0, x1, (float)i / n), b = Mathf.Lerp(x0, x1, (i + 1f) / n);
                float h = Mathf.Lerp(hmin, hmax, (float)r.NextDouble());
                float lean = ((float)r.NextDouble() - 0.5f) * (b - a);
                float dir = down ? -1f : 1f;
                p.Triangle(new Vector2(a, y), new Vector2(b, y), new Vector2((a + b) * 0.5f + lean, y + dir * h), Color.Lerp(Bone, BoneDark, (float)r.NextDouble() * 0.5f));
            }
        }

        /// <summary>ノッポ: 異様に長い体と腕、ひび割れた白い面、耳まで裂けた縫い目の口。</summary>
        static Sprite Tall()
        {
            var p = new Painter(MonW, MonH); var r = new System.Random(11);
            p.Fill(new[] { new Vector2(98, 8), new Vector2(124, 336), new Vector2(198, 336), new Vector2(224, 8) }, Ink);
            p.Line(new Vector2(124, 336), new Vector2(98, 8), 3f, Rim);
            // 長すぎる腕と指
            Vector2[] la = { new Vector2(126, 322), new Vector2(74, 205), new Vector2(62, 78) };
            Vector2[] ra = { new Vector2(196, 322), new Vector2(252, 212), new Vector2(262, 70) };
            p.Poly(la, 9, 5, Ink); p.Poly(ra, 9, 5, Ink);
            for (int i = 0; i < 5; i++)
            {
                p.Line(la[2], new Vector2(la[2].x - 18 + i * 9, 18 + (float)r.NextDouble() * 20), 3.2f, InkLit);
                p.Line(ra[2], new Vector2(ra[2].x - 18 + i * 9, 14 + (float)r.NextDouble() * 22), 3.2f, InkLit);
            }
            p.Line(new Vector2(160, 330), new Vector2(150, 398), 15, Ink);
            // 傾いた面
            p.Ellipse(146, 428, 42, 58, new Color(0.9f, 0.87f, 0.76f, 1f), new Color(0.22f, 0.2f, 0.17f, 1f), -0.6f, 0.6f);
            p.Poly(new[] { new Vector2(166, 484), new Vector2(156, 460), new Vector2(170, 444), new Vector2(158, 428), new Vector2(164, 408) }, 2.4f, 1.2f, new Color(0.08f, 0.06f, 0.05f, 0.95f));
            p.Poly(new[] { new Vector2(128, 440), new Vector2(112, 424), new Vector2(116, 404) }, 2f, 1f, new Color(0.08f, 0.06f, 0.05f, 0.9f));
            // 目の穴(左右で大きさが違う)
            p.Ellipse(128, 436, 13, 19, Color.black); p.Ellipse(167, 432, 10, 15, Color.black);
            p.Glow(128, 434, 9, new Color(1f, 0.85f, 0.35f, 0.55f), 2f); p.Glow(166, 432, 8, new Color(1f, 0.85f, 0.35f, 0.55f), 2f);
            // 縫い目の口: 顔の端まで裂ける
            p.Ellipse(147, 392, 34, 15, new Color(0.02f, 0.01f, 0.01f, 1f));
            p.Poly(new[] { new Vector2(108, 398), new Vector2(124, 388), new Vector2(150, 384), new Vector2(176, 390), new Vector2(188, 402) }, 3, 3, new Color(0.02f, 0.01f, 0.01f, 1f));
            Teeth(p, 116, 180, 396, 14, 7, 14, true, r); Teeth(p, 118, 178, 384, 13, 6, 12, false, r);
            for (int i = 0; i < 9; i++) { float sx = 112 + i * 9; p.Line(new Vector2(sx, 400), new Vector2(sx + 3, 382), 1.6f, new Color(0.02f, 0.02f, 0.02f, 0.85f)); }
            p.Grain(0.06f, 5);
            return p.ToSprite(PPU, new Vector2(0.5f, 0.02f));
        }

        /// <summary>まんまる: 血走った巨大な一つ目、毛羽立つ体、不揃いな歯。体中に小さな目。</summary>
        static Sprite Round()
        {
            var p = new Painter(MonW, MonH); var r = new System.Random(23);
            p.Ellipse(160, 175, 148, 158, new Color(0.2f, 0.15f, 0.24f, 1f), new Color(0.025f, 0.02f, 0.035f, 1f), -0.5f, 0.7f);
            for (int i = 0; i < 260; i++)
            {
                float a = (float)(r.NextDouble() * Mathf.PI * 2), rx = 146, ry = 156;
                Vector2 c = new Vector2(160, 175);
                Vector2 s = c + new Vector2(Mathf.Cos(a) * rx * 0.96f, Mathf.Sin(a) * ry * 0.96f);
                Vector2 e = c + new Vector2(Mathf.Cos(a) * (rx + 8 + (float)r.NextDouble() * 16), Mathf.Sin(a) * (ry + 8 + (float)r.NextDouble() * 16));
                p.Line(s, e, 2.2f, new Color(0.03f, 0.025f, 0.04f, 0.9f));
            }
            // 巨大な目と血管
            p.Ellipse(160, 212, 58, 66, new Color(0.92f, 0.88f, 0.62f, 1f), new Color(0.35f, 0.25f, 0.12f, 1f), -0.4f, 0.5f);
            for (int i = 0; i < 34; i++)
            {
                float a = (float)(r.NextDouble() * Mathf.PI * 2), len = 0.45f + (float)r.NextDouble() * 0.4f;
                Vector2 s = new Vector2(160 + Mathf.Cos(a) * 56, 212 + Mathf.Sin(a) * 64);
                Vector2 m = new Vector2(160 + Mathf.Cos(a) * 56 * (1 - len * 0.5f) + ((float)r.NextDouble() - .5f) * 10, 212 + Mathf.Sin(a) * 64 * (1 - len * 0.5f));
                Vector2 e = new Vector2(160 + Mathf.Cos(a) * 56 * (1 - len), 212 + Mathf.Sin(a) * 64 * (1 - len));
                p.Poly(new[] { s, m, e }, 2f, 0.8f, new Color(0.72f, 0.05f, 0.06f, 0.85f));
            }
            p.Ellipse(160, 210, 11, 48, Color.black);
            p.Glow(160, 212, 40, new Color(1f, 0.9f, 0.4f, 0.28f), 2f);
            // 体の小さな目
            foreach (var e in new[] { new Vector3(70, 152, 11), new Vector3(250, 142, 11), new Vector3(92, 262, 9), new Vector3(232, 266, 9) })
            {
                p.Ellipse(e.x, e.y, e.z + 3, e.z + 3, new Color(0.02f, 0.01f, 0.02f, 1f));
                p.Ellipse(e.x, e.y, e.z, e.z * 0.9f, new Color(0.85f, 0.8f, 0.5f, 1f), new Color(0.3f, 0.2f, 0.1f, 1f));
                p.Ellipse(e.x, e.y, e.z * 0.25f, e.z * 0.8f, Color.black);
            }
            // 口
            p.Ellipse(160, 88, 98, 36, new Color(0.04f, 0.0f, 0.02f, 1f));
            p.Ellipse(160, 82, 80, 20, new Color(0.35f, 0.04f, 0.07f, 0.85f));
            Teeth(p, 70, 250, 112, 17, 10, 30, true, r); Teeth(p, 74, 246, 64, 15, 8, 26, false, r);
            p.Grain(0.07f, 9);
            return p.ToSprite(PPU, new Vector2(0.5f, 0.02f));
        }

        /// <summary>つのっこ: 長い脚で這う、ツノと三つ目の怪物。顎は縦に大きく裂ける。</summary>
        static Sprite Horned()
        {
            var p = new Painter(MonW, MonH); var r = new System.Random(37);
            Vector2[][] legs = {
                new[]{ new Vector2(98,150), new Vector2(28,246), new Vector2(18,66) }, new[]{ new Vector2(102,126), new Vector2(8,176), new Vector2(22,14) },
                new[]{ new Vector2(222,150), new Vector2(292,246), new Vector2(302,66) }, new[]{ new Vector2(218,126), new Vector2(312,176), new Vector2(298,14) } };
            foreach (var l in legs) { p.Poly(l, 11, 4, Ink); p.Poly(l, 3, 2, Rim); }
            p.Ellipse(160, 145, 96, 76, new Color(0.17f, 0.19f, 0.15f, 1f), new Color(0.02f, 0.025f, 0.02f, 1f));
            foreach (var d in new[] { -1f, 1f })
            {
                var horn = Painter.Curve(new Vector2(160 + d * 46, 250), new Vector2(160 + d * 118, 300), new Vector2(160 + d * 78, 452));
                p.Poly(horn, 24, 3, new Color(0.1f, 0.09f, 0.07f, 1f));
                p.Poly(horn, 5, 1.5f, new Color(0.55f, 0.5f, 0.4f, 0.5f));
            }
            p.Ellipse(160, 222, 66, 60, new Color(0.24f, 0.26f, 0.2f, 1f), new Color(0.03f, 0.035f, 0.03f, 1f), -0.5f, 0.6f);
            foreach (var e in new[] { new Vector3(135, 228, 12), new Vector3(185, 228, 12), new Vector3(160, 254, 9) })
            {
                p.Ellipse(e.x, e.y, e.z + 5, e.z + 5, Color.black);
                p.Ellipse(e.x, e.y, e.z, e.z * 1.15f, new Color(0.9f, 0.78f, 0.2f, 1f), new Color(0.4f, 0.25f, 0.05f, 1f));
                p.Ellipse(e.x, e.y, e.z * 0.25f, e.z * 1.05f, Color.black);
            }
            // 縦に裂ける顎
            p.Ellipse(160, 178, 46, 36, new Color(0.03f, 0.0f, 0.01f, 1f));
            Teeth(p, 118, 202, 198, 12, 10, 24, true, r); Teeth(p, 118, 202, 160, 12, 8, 22, false, r);
            for (int i = 0; i < 6; i++) { float x = 128 + i * 13; p.Line(new Vector2(x, 150), new Vector2(x + ((float)r.NextDouble() - .5f) * 6, 96 - (float)r.NextDouble() * 40), 1.8f, new Color(0.7f, 0.78f, 0.7f, 0.45f)); }
            p.Grain(0.06f, 13);
            return p.ToSprite(PPU, new Vector2(0.5f, 0.02f));
        }

        /// <summary>オルゴールが止まると近づく人形「ミミ」。</summary>
        public static Sprite Doll()
        {
            return Get("doll", () =>
            {
                var p = new Painter(MonW, MonH);
                p.Fill(new[] { new Vector2(112, 8), new Vector2(132, 190), new Vector2(188, 190), new Vector2(208, 8) }, new Color(0.18f, 0.08f, 0.1f, 1f));
                p.Line(new Vector2(132, 190), new Vector2(112, 8), 3f, Rim);
                p.Line(new Vector2(136, 180), new Vector2(86, 110), 9, new Color(0.7f, 0.66f, 0.58f, 1f));
                p.Line(new Vector2(184, 180), new Vector2(236, 120), 9, new Color(0.7f, 0.66f, 0.58f, 1f));
                p.Ellipse(160, 252, 64, 68, new Color(0.92f, 0.88f, 0.78f, 1f), new Color(0.25f, 0.22f, 0.18f, 1f), -0.5f, 0.6f);
                p.Triangle(new Vector2(160, 310), new Vector2(92, 350), new Vector2(98, 296), new Color(0.5f, 0.05f, 0.08f, 1f));
                p.Triangle(new Vector2(160, 310), new Vector2(228, 350), new Vector2(222, 296), new Color(0.5f, 0.05f, 0.08f, 1f));
                p.Ellipse(130, 258, 17, 20, Color.black);
                p.Glow(130, 258, 12, new Color(1f, 0.3f, 0.2f, 0.6f), 2f);
                p.Line(new Vector2(176, 272), new Vector2(204, 244), 5, Color.black); p.Line(new Vector2(204, 272), new Vector2(176, 244), 5, Color.black);
                p.Poly(new[] { new Vector2(118, 214), new Vector2(140, 200), new Vector2(160, 196), new Vector2(180, 200), new Vector2(202, 214) }, 5, 5, new Color(0.35f, 0.02f, 0.04f, 1f));
                for (int i = 0; i < 7; i++) { float x = 124 + i * 12; p.Line(new Vector2(x, 216 - (i % 2) * 6), new Vector2(x, 202), 1.6f, new Color(0.1f, 0.04f, 0.04f, 0.9f)); }
                p.Grain(0.06f, 17);
                return p.ToSprite(PPU, new Vector2(0.5f, 0.02f));
            });
        }
    }
}
