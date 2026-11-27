using System.Collections.Generic;
using UnityEngine;

namespace NightRoom
{
    /// <summary>扉の中に立つ敵の見た目。近づくほど大きく、震え、目が脈打つ。</summary>
    public class MonsterView : MonoBehaviour
    {
        // 段階0〜4での大きさ(段階4は扉のすぐ前)
        static readonly float[] StageScale = { 0f, 0.27f, 0.38f, 0.52f, 0.72f };
        static readonly float[] KindMul = { 0.95f, 1.0f, 1.0f };

        int kind;
        float doorX, doorBottom;
        SpriteRenderer body;
        readonly List<SpriteRenderer> glows = new List<SpriteRenderer>();
        Vector3[] eyes;
        float seed;

        public void Setup(int k, Vector2 doorCenter, float doorHeight, SpriteMaskInteraction mask)
        {
            kind = k; doorX = doorCenter.x; doorBottom = doorCenter.y - doorHeight * 0.5f + 0.1f;
            seed = k * 17.3f;
            body = gameObject.AddComponent<SpriteRenderer>();
            body.sprite = CreepyArt.Monster(k);
            body.sortingOrder = 11;
            body.maskInteraction = mask;
            eyes = CreepyArt.Eyes(k);
            for (int i = 0; i < eyes.Length; i++)
            {
                var go = new GameObject("eye" + i);
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = CreepyArt.Glow();
                sr.maskInteraction = mask;
                sr.sortingOrder = 12;
                float r = eyes[i].z;
                go.transform.localPosition = new Vector3((eyes[i].x - CreepyArt.MonW * 0.5f) / 100f, (eyes[i].y - CreepyArt.MonH * 0.02f) / 100f, 0f);
                go.transform.localScale = Vector3.one * (r * 4.2f / 100f);
                glows.Add(sr);
            }
        }

        /// <param name="bodyAlpha">体の見え方(扉を覗いて光を当てたときだけ1)</param>
        /// <param name="eyesShown">暗闇でも光る目を見せるか</param>
        /// <param name="eyesOnTop">停電中は目を暗闇より手前に出す</param>
        public void Apply(int stage, float bodyAlpha, bool eyesShown, bool eyesOnTop, float t)
        {
            bool any = stage > 0 || eyesShown;
            int st = Mathf.Clamp(stage, 0, 4);
            if (eyesShown && stage < 3) st = 3;
            if (!any) { body.enabled = false; foreach (var g in glows) g.enabled = false; return; }

            body.enabled = bodyAlpha > 0.01f;
            body.color = new Color(1, 1, 1, bodyAlpha);

            float s = StageScale[st] * KindMul[kind];
            float shake = st >= 3 ? 0.012f * (st - 2) : 0.003f;
            float nx = (Mathf.PerlinNoise(t * 9f + seed, 0f) - 0.5f) * 2f * shake;
            float ny = (Mathf.PerlinNoise(0f, t * 9f + seed) - 0.5f) * 2f * shake;
            // たまにビクッと跳ねる
            float tw = Mathf.PerlinNoise(t * 1.7f + seed, 3f) > 0.82f ? 0.06f : 0f;
            transform.position = new Vector3(doorX + nx + tw, doorBottom + (4 - st) * 0.1f + ny, 0f);
            transform.localScale = Vector3.one * s;
            transform.rotation = Quaternion.Euler(0, 0, (Mathf.PerlinNoise(t * 0.6f + seed, 1f) - 0.5f) * 7f + tw * 40f);

            Color col = st >= 4 ? new Color(1f, 0.18f, 0.12f) : new Color(1f, 0.82f, 0.3f);
            for (int i = 0; i < glows.Count; i++)
            {
                var g = glows[i];
                g.enabled = eyesShown || bodyAlpha > 0.5f;
                float pulse = 0.65f + 0.35f * Mathf.Sin(t * (4f + st) + i * 1.7f);
                float flick = Mathf.PerlinNoise(t * 14f + i * 5f, seed) > 0.18f ? 1f : 0.2f;
                col.a = pulse * (st >= 3 ? flick : 1f);
                g.color = col;
                g.sortingOrder = eyesOnTop ? 220 : 12;
            }
        }
    }
}
