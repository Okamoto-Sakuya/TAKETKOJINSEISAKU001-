using System;
using System.Collections;
using UnityEngine;

namespace NightRoom
{
    public enum Phase { Title, Playing, Win, Lose }
    public enum ViewState { Room, ZoomIn, Door, ZoomOut }

    /// <summary>難易度の調整値。Inspectorから変更できる。</summary>
    [Serializable]
    public class Tuning
    {
        [Header("時間")]
        public float hourSeconds = 45f;           // 1時間ぶんの実時間(6時間で約4.5分)
        [Header("敵の出現(前より遅く調整済み)")]
        public float opening = 25f;               // 夜の始まりは敵が動かない猶予(秒)
        public Vector2 firstAppear = new Vector2(14f, 22f);   // 最初に動き出すまで
        public Vector2 advance = new Vector2(7f, 13f);        // 1段階近づくまで(ノッポ/つのっこ)
        public Vector2 advanceRound = new Vector2(6f, 11f);   // まんまる
        public Vector2 afterRetreat = new Vector2(9f, 14f);   // 追い払った後の再出現
        public float speedPerHour = 0.1f;                    // 時間が進むごとの加速(前は0.3)
        public Vector2 finalGrace = new Vector2(3.5f, 5.5f);  // 扉の前に来てから襲うまで
        [Header("追い払い")]
        public float watchNeed = 0.4f;
        public float watchNeedRound = 0.7f;
        [Header("電池とオルゴール")]
        public float batteryDrain = 2.0f;
        public float batteryRecharge = 6f;
        public float boxDrain = 2.6f;
        public float boxDrainPerHour = 0.5f;
        public float windRate = 30f;
        public float dollBase = 3f;              // オルゴール停止から人形が来るまで
        public float dollPerHour = 0.8f;
        [Header("ドアへ近づく演出")]
        public float zoomInSeconds = 0.9f;
        public float zoomOutSeconds = 0.45f;
    }

    public class MonsterState { public int kind, stage; public float wait, watch, grace; }

    public partial class GameManager : MonoBehaviour
    {
        public Tuning tune = new Tuning();

        // GameBootstrap が配線する
        [HideInInspector] public DoorCameraController camCtl;
        [HideInInspector] public MonsterView[] views;
        [HideInInspector] public Hud hud;
        [HideInInspector] public Collider2D[] doorHits;
        [HideInInspector] public Collider2D boxHit;
        [HideInInspector] public SpriteRenderer lightMask, blackout, flashFace, scare, redFlash, dollSr, grainSr, boxLamp;
        [HideInInspector] public Texture2D grainTex;
        [HideInInspector] public AudioSource droneSrc;

        public Phase phase = Phase.Title;
        public ViewState view = ViewState.Room;
        int viewDoor = -1, pendingDoor = -1;

        MonsterState[] m;
        float t, battery, box, mimi, blackT, flashT, rattleT, noteT, fakeT;
        bool dead, winding;
        int noteIdx, fakeDoor = -1;
        float blackAlpha, grainTick;
        static readonly float[] Mel = { 659, 784, 988, 880, 784, 659, 740, 659, 587, 659, 784, 659, 587, 523, 587, 494 };
        static readonly string[] Names = { "ノッポ", "まんまる", "つのっこ" };

        int Hour { get { return Mathf.Min(5, Mathf.FloorToInt(t / tune.hourSeconds)); } }
        float Total { get { return tune.hourSeconds * 6f; } }
        bool Lit { get { return view == ViewState.Door && !dead && battery > 0f; } }

        void Start() { ShowTitle(); }

        void ShowTitle()
        {
            phase = Phase.Title; ResetState(); hud.ShowTitle();
            if (droneSrc) droneSrc.Stop();
        }

        void ResetState()
        {
            t = 0; battery = 100; box = 100; mimi = 0; blackT = 0; flashT = 0; rattleT = 0; noteT = 0; noteIdx = 0;
            dead = false; winding = false; fakeDoor = -1; fakeT = 0; blackAlpha = 0;
            view = ViewState.Room; viewDoor = pendingDoor = -1;
            camCtl.SnapToRoom();
            m = new MonsterState[3];
            for (int i = 0; i < 3; i++)
                m[i] = new MonsterState { kind = i, stage = 0, wait = tune.opening + UnityEngine.Random.Range(tune.firstAppear.x, tune.firstAppear.y) };
            scare.enabled = false; redFlash.enabled = false; flashFace.enabled = false;
        }

        void Begin()
        {
            StopAllCoroutines();
            ResetState();
            hud.HideOverlay();
            phase = Phase.Playing;
            if (droneSrc) { droneSrc.clip = ProcAudio.Drone(); droneSrc.loop = true; droneSrc.volume = .35f; droneSrc.Play(); }
        }

        // ---------- 入力 ----------
        void Update()
        {
            UpdateGrain();
            if (phase == Phase.Title || phase == Phase.Win || phase == Phase.Lose)
            {
                if (phase == Phase.Lose && scareRunning) return;
                if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Return)) { if (phase == Phase.Title) Begin(); else { ShowTitleThenBegin(); } }
                return;
            }
            HandleInput();
            Simulate(Time.deltaTime);
            if (phase != Phase.Playing) return;   // ジャンプスケア/クリア直後は見た目を更新しない
            UpdateVisuals();
            UpdateHud();
        }

        void ShowTitleThenBegin() { Begin(); }

        void HandleInput()
        {
            for (int i = 0; i < 3; i++) if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i)) RequestDoor(i);
            if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1)) LeaveDoor();

            bool wantWind = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.Space);
            Vector2 wp = camCtl.cam.ScreenToWorldPoint(Input.mousePosition);
            if (Input.GetMouseButtonDown(0))
            {
                for (int i = 0; i < doorHits.Length; i++) if (doorHits[i].OverlapPoint(wp)) { RequestDoor(i); break; }
            }
            if (Input.GetMouseButton(0) && boxHit.OverlapPoint(wp)) wantWind = true;
            if (wantWind && view != ViewState.Room && view != ViewState.ZoomOut) LeaveDoor();
            winding = wantWind && view == ViewState.Room;
        }

        // ---------- ドアへ近づく演出 ----------
        void RequestDoor(int i)
        {
            if (phase != Phase.Playing) return;
            if (view == ViewState.Room || view == ViewState.ZoomOut)
            {
                if (view == ViewState.ZoomOut) { pendingDoor = i; return; }
                StartZoomIn(i);
            }
            else if (viewDoor != i) { pendingDoor = i; LeaveDoor(); }
        }

        void StartZoomIn(int i)
        {
            viewDoor = i; pendingDoor = -1; view = ViewState.ZoomIn;
            camCtl.ZoomIn(i, tune.zoomInSeconds, () => { if (view == ViewState.ZoomIn) view = ViewState.Door; });
        }

        void LeaveDoor()
        {
            if (view == ViewState.Room || view == ViewState.ZoomOut) return;
            view = ViewState.ZoomOut;
            camCtl.ZoomOut(tune.zoomOutSeconds, () =>
            {
                view = ViewState.Room; viewDoor = -1;
                if (pendingDoor >= 0 && phase == Phase.Playing) StartZoomIn(pendingDoor);
            });
        }
    }
}
