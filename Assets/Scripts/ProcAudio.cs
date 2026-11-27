using System.Collections.Generic;
using UnityEngine;

namespace NightRoom
{
    /// <summary>音声ファイル不要。効果音と環境音を計算で作る。</summary>
    public static class ProcAudio
    {
        const int SR = 44100;
        static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
        static System.Random rng = new System.Random(99);

        static float Rnd() { return (float)(rng.NextDouble() * 2.0 - 1.0); }

        static AudioClip Make(string key, float dur, System.Func<float, float, float> f)
        {
            AudioClip c;
            if (cache.TryGetValue(key, out c)) return c;
            int n = (int)(SR * dur);
            var d = new float[n];
            for (int i = 0; i < n; i++) d[i] = Mathf.Clamp(f((float)i / SR, (float)i / n), -1f, 1f);
            c = AudioClip.Create(key, n, 1, SR, false);
            c.SetData(d, 0);
            cache[key] = c;
            return c;
        }

        static float Sweep(float t, float f0, float f1, float dur) { return Mathf.Sin(2f * Mathf.PI * (f0 * t + (f1 - f0) * t * t / (2f * dur))); }

        public static AudioClip Thump() { return Make("thump", .35f, (t, p) => Sweep(t, 90, 36, .35f) * Mathf.Pow(1f - p, 3f) * .9f); }

        public static AudioClip Footstep() { return Make("step", .25f, (t, p) => (Sweep(t, 70, 40, .25f) * .8f + Rnd() * .12f) * Mathf.Pow(1f - p, 4f)); }

        public static AudioClip Rattle()
        {
            return Make("rattle", .45f, (t, p) =>
            {
                float g = (t % .15f) < .05f ? 1f : 0f;
                float env = (p < .75f) ? 1f : (1f - p) * 4f;
                return Rnd() * Mathf.Sin(2f * Mathf.PI * 1700f * t) * g * env * .5f;
            });
        }

        public static AudioClip Growl() { return Make("growl", .55f, (t, p) => ((Mathf.Repeat(t * Mathf.Lerp(70, 45, p), 1f) * 2f - 1f) * .45f + Rnd() * .2f) * Mathf.Sin(p * Mathf.PI)); }

        public static AudioClip Whisper() { return Make("whisper", 1.2f, (t, p) => Rnd() * (.5f + .5f * Mathf.Sin(2f * Mathf.PI * 1100f * t)) * Mathf.Sin(p * Mathf.PI) * (.5f + .5f * Mathf.Sin(t * 11f)) * .4f); }

        public static AudioClip Scream()
        {
            return Make("scream", 1.6f, (t, p) =>
            {
                float env = p < .05f ? p / .05f : Mathf.Pow(1f - p, 1.3f);
                float a = Rnd() * .8f;
                float b = Mathf.Sign(Sweep(t, 150, 60, 1.6f)) * .5f;
                float c = (Mathf.Repeat(t * Mathf.Lerp(420, 1100, p), 1f) * 2f - 1f) * .5f;
                return (a + b + c) * env * .55f;
            });
        }

        public static AudioClip Creak() { return Make("creak", .9f, (t, p) => (Mathf.Repeat(t * Mathf.Lerp(180, 90, p), 1f) * 2f - 1f) * Mathf.Sin(p * Mathf.PI) * .18f * (.6f + .4f * Mathf.Sin(t * 40f))); }

        public static AudioClip Note(float freq)
        {
            return Make("note" + (int)freq, .6f, (t, p) =>
            {
                float s = 2f * Mathf.Abs(Mathf.Repeat(t * freq, 1f) * 2f - 1f) - 1f;
                return s * Mathf.Exp(-p * 6f) * .5f;
            });
        }

        public static AudioClip Drone() { return Make("drone", 4f, (t, p) => (Mathf.Sin(2f * Mathf.PI * 46f * t) * .5f + Mathf.Sin(2f * Mathf.PI * 47.5f * t) * .5f) * .3f); }
    }
}
