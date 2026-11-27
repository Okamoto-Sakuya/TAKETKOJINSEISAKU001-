using System;
using UnityEngine;

namespace NightRoom
{
    /// <summary>
    /// 扉をクリックすると、カメラがじわじわ扉へ近づき(ズーム)、到着してから覗けるようにする演出。
    /// 近づくほど手ブレ・傾きが強くなる。途中で戻る操作をしても、いまの位置から滑らかに戻る。
    /// </summary>
    public class DoorCameraController : MonoBehaviour
    {
        public Camera cam;
        public Vector2 roomPos = Vector2.zero;
        public float roomSize = 5.2f;
        public float doorSize = 2.25f;
        public Vector2[] doorFocus = new Vector2[3];

        /// <summary>ズーム中はtrue。</summary>
        public bool Busy { get; private set; }
        /// <summary>0=部屋 1=扉の前。</summary>
        public float Progress { get { return lin; } }
        public Action<float> OnProgress;

        float lin;          // 線形の進み具合 0..1
        int door;           // 向かっている(または戻っている)扉
        float target;       // 0 or 1
        float seconds = 1f; // 0→1にかかる時間
        Action done;
        int stepsPlayed;

        void Awake() { if (cam == null) cam = GetComponent<Camera>(); }

        public void SnapToRoom() { lin = 0f; target = 0f; Busy = false; done = null; Render(); }

        public void ZoomIn(int d, float sec, Action onDone)
        {
            if (lin <= 0.001f) { stepsPlayed = 0; AudioSourceUtil.Play(ProcAudio.Creak(), .5f); }
            door = d; target = 1f; seconds = Mathf.Max(0.05f, sec); done = onDone; Busy = true;
        }

        public void ZoomOut(float sec, Action onDone)
        {
            target = 0f; seconds = Mathf.Max(0.05f, sec); done = onDone; Busy = true;
        }

        void Update()
        {
            if (!Busy) return;
            float prev = lin;
            lin = Mathf.MoveTowards(lin, target, Time.deltaTime / seconds);
            // 近づく途中で足音が3回
            if (target > 0.5f && stepsPlayed < 3 && lin > (stepsPlayed + 1) * 0.26f)
            { stepsPlayed++; AudioSourceUtil.Play(ProcAudio.Footstep(), .3f + stepsPlayed * .12f); }
            Render();
            if (Mathf.Approximately(lin, target))
            {
                Busy = false;
                var cb = done; done = null;
                if (cb != null) cb();
            }
        }

        void Render()
        {
            // ゆっくり踏み出して、最後にぐっと寄る
            float e = target >= 0.5f || lin > 0.001f ? Mathf.Pow(Mathf.SmoothStep(0f, 1f, lin), 1.35f) : 0f;
            Vector2 f = (door >= 0 && door < doorFocus.Length) ? doorFocus[door] : roomPos;
            Vector2 pos = Vector2.Lerp(roomPos, f, e);
            float size = Mathf.Lerp(roomSize, doorSize, e);
            float t = Time.time;
            float moving = Busy ? Mathf.Sin(Mathf.Clamp01(lin) * Mathf.PI) : 0f;
            float amp = moving * 0.07f + e * 0.01f;
            pos += new Vector2((Mathf.PerlinNoise(t * 7f, 0f) - .5f) * amp, (Mathf.PerlinNoise(0f, t * 7f) - .5f) * amp);
            cam.transform.position = new Vector3(pos.x, pos.y, -10f);
            cam.orthographicSize = size;
            cam.transform.rotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 2.3f) * 1.4f * moving);
            if (OnProgress != null) OnProgress(e);
        }
    }

    /// <summary>どこからでも鳴らせる簡易効果音プレイヤー。</summary>
    public static class AudioSourceUtil
    {
        static AudioSource src;
        public static void Play(AudioClip c, float vol = 1f)
        {
            if (src == null)
            {
                var go = new GameObject("SFX");
                UnityEngine.Object.DontDestroyOnLoad(go);
                src = go.AddComponent<AudioSource>();
                src.spatialBlend = 0f;
            }
            if (c != null) src.PlayOneShot(c, vol);
        }
    }
}
