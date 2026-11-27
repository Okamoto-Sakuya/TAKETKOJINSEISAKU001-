using UnityEngine;

namespace NightRoom
{
    /// <summary>
    /// 空のシーンで再生するだけで、部屋・扉・敵・UIを全部コードで組み立てる。
    /// 素材(画像/音)は不要。後から自前の絵に差し替えたいときは CreepyArt の各関数を置き換える。
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        static GameBootstrap instance;
        const float DoorW = 2.2f, DoorH = 3.6f, DoorY = 0.35f;
        static readonly float[] DoorX = { -5.6f, 0f, 5.6f };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoStart()
        {
            if (instance == null) new GameObject("NightRoom").AddComponent<GameBootstrap>();
        }

        void Awake() { instance = this; Build(); }

        SpriteRenderer Spr(string name, Sprite s, Vector2 pos, Vector2 scale, Color c, int order, Transform parent = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent != null ? parent : transform, false);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s; sr.color = c; sr.sortingOrder = order;
            return sr;
        }

        Collider2D Hit(string name, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false);
            go.transform.position = pos;
            var b = go.AddComponent<BoxCollider2D>(); b.size = size;
            return b;
        }

        void Build()
        {
            Application.targetFrameRate = 60;
            // ---- カメラ ----
            Camera cam = Camera.main;
            if (cam == null) { var cg = new GameObject("Main Camera"); cg.tag = "MainCamera"; cam = cg.AddComponent<Camera>(); }
            cam.orthographic = true; cam.orthographicSize = 5.2f;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
            cam.transform.position = new Vector3(0, 0, -10); cam.transform.rotation = Quaternion.identity;
            if (cam.GetComponent<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();
            var cc = cam.gameObject.GetComponent<DoorCameraController>();
            if (cc == null) cc = cam.gameObject.AddComponent<DoorCameraController>();
            cc.cam = cam; cc.roomPos = Vector2.zero; cc.roomSize = 5.2f; cc.doorSize = 2.25f;
            cc.doorFocus = new Vector2[3];
            for (int i = 0; i < 3; i++) cc.doorFocus[i] = new Vector2(DoorX[i], DoorY);

            var sq = CreepyArt.Square(); var glow = CreepyArt.Glow();

            // ---- 部屋(壁・床・家具) ----
            Spr("Wall", sq, new Vector2(0, 2.6f), new Vector2(40, 10), new Color(.09f, .15f, .19f), 0);
            for (int i = -12; i <= 12; i++) Spr("Stripe", sq, new Vector2(i * 1.5f, 2.6f), new Vector2(.5f, 10), new Color(1, 1, 1, .03f), 1);
            for (int i = 0; i < 14; i++) Spr("Star", glow, new Vector2(-8.5f + i * 1.3f, 4.1f + (i % 3) * .35f), Vector2.one * .22f, new Color(1f, .85f, .5f, .22f), 1);
            Spr("Floor", sq, new Vector2(0, -4.2f), new Vector2(40, 3.4f), new Color(.1f, .07f, .06f), 0);
            Spr("Skirting", sq, new Vector2(0, -2.5f), new Vector2(40, .18f), new Color(.05f, .07f, .1f), 1);
            Spr("Rug", glow, new Vector2(0, -3.7f), new Vector2(9, 2.2f), new Color(.45f, .1f, .14f, .45f), 1);
            Spr("Chest", sq, new Vector2(-7.4f, -3.2f), new Vector2(2.2f, 1.2f), new Color(.23f, .14f, .09f), 3);
            Spr("ChestLid", sq, new Vector2(-7.4f, -2.65f), new Vector2(2.3f, .25f), new Color(.34f, .21f, .12f), 3);
            Spr("Block1", sq, new Vector2(-5.4f, -3.9f), new Vector2(.5f, .5f), new Color(.75f, .27f, .24f), 3);
            Spr("Block2", sq, new Vector2(-4.7f, -4.1f), new Vector2(.5f, .5f), new Color(.24f, .49f, .75f), 3);
            Spr("Shelf", sq, new Vector2(7.6f, .95f), new Vector2(2.3f, .12f), new Color(.17f, .12f, .09f), 3);
            Spr("NightLightGlow", glow, new Vector2(-7.6f, -3.1f), new Vector2(11, 8), new Color(.91f, .7f, .35f, .1f), 2);
            Spr("NightLight", sq, new Vector2(-7.6f, -3.2f), new Vector2(.16f, .22f), new Color(.91f, .7f, .35f), 4);

            // ---- 扉3つ ----
            var views = new MonsterView[3]; var hits = new Collider2D[3];
            for (int i = 0; i < 3; i++)
            {
                Vector2 c = new Vector2(DoorX[i], DoorY);
                Spr("Frame" + i, sq, c, new Vector2(DoorW + .4f, DoorH + .3f), new Color(.16f, .12f, .19f), 2);
                var mg = new GameObject("Mask" + i); mg.transform.SetParent(transform, false);
                mg.transform.position = c; mg.transform.localScale = new Vector3(DoorW, DoorH, 1f);
                var mask = mg.AddComponent<SpriteMask>(); mask.sprite = sq;
                var hall = Spr("Hall" + i, CreepyArt.Hallway(), c, Vector2.one, Color.white, 10);
                hall.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                Spr("Knob" + i, glow, new Vector2(DoorX[i] + (i == 2 ? -.8f : .8f), DoorY - .2f), Vector2.one * .22f, new Color(.8f, .65f, .3f, .9f), 4);
                var mv = new GameObject("Monster" + i); mv.transform.SetParent(transform, false);
                var view = mv.AddComponent<MonsterView>();
                view.Setup(i, c, DoorH, SpriteMaskInteraction.VisibleInsideMask);
                views[i] = view;
                hits[i] = Hit("DoorHit" + i, c, new Vector2(DoorW + .4f, DoorH + .3f));
            }

            // ---- オルゴール ----
            Spr("Table", sq, new Vector2(0, -3.95f), new Vector2(4.2f, 1.1f), new Color(.11f, .08f, .06f), 3);
            var boxLamp = Spr("BoxLamp", glow, new Vector2(0, -3.1f), new Vector2(3.4f, 2.4f), new Color(.91f, .7f, .35f, .6f), 4);
            Spr("BoxBody", sq, new Vector2(0, -3.25f), new Vector2(1.9f, .7f), new Color(.42f, .29f, .16f), 5);
            Spr("BoxLid", sq, new Vector2(0, -2.82f), new Vector2(2.05f, .2f), new Color(.54f, .38f, .22f), 5);
            Spr("Crank", sq, new Vector2(1.15f, -3.2f), new Vector2(.12f, .55f), new Color(.8f, .64f, .29f), 6);
            var boxHit = Hit("BoxHit", new Vector2(0, -3.15f), new Vector2(2.8f, 1.5f));

            // ---- 人形 ----
            var doll = Spr("Doll", CreepyArt.Doll(), new Vector2(7.6f, 1.0f), Vector2.one * .32f, Color.white, 6);
            doll.enabled = false;

            // ---- 暗闇・光・演出レイヤー ----
            var light = Spr("LightMask", CreepyArt.LightMask(), Vector2.zero, Vector2.one * 10f, Color.white, 100);
            var black = Spr("Blackout", sq, Vector2.zero, new Vector2(30, 14), new Color(0, 0, 0, 0), 150);
            var flash = Spr("FlashFace", CreepyArt.Monster(0), Vector2.zero, Vector2.one, new Color(1, 1, 1, 0), 300);
            var scare = Spr("Scare", CreepyArt.Monster(0), Vector2.zero, Vector2.one, Color.white, 500);
            var red = Spr("RedFlash", sq, Vector2.zero, new Vector2(30, 16), new Color(.78f, .08f, .14f, 0), 600);
            var grainTex = new Texture2D(96, 96, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var grain = Spr("Grain", Sprite.Create(grainTex, new Rect(0, 0, 96, 96), new Vector2(.5f, .5f), 96f), Vector2.zero, Vector2.one * 20f, new Color(1, 1, 1, .09f), 400);
            light.enabled = black.enabled = flash.enabled = scare.enabled = red.enabled = false;

            // ---- 音・UI・ゲーム本体 ----
            var drone = new GameObject("Drone").AddComponent<AudioSource>(); drone.transform.SetParent(transform, false);
            var gm = gameObject.AddComponent<GameManager>();
            gm.camCtl = cc; gm.views = views; gm.hud = Hud.Create();
            gm.doorHits = hits; gm.boxHit = boxHit;
            gm.lightMask = light; gm.blackout = black; gm.flashFace = flash; gm.scare = scare; gm.redFlash = red;
            gm.dollSr = doll; gm.grainSr = grain; gm.grainTex = grainTex; gm.boxLamp = boxLamp; gm.droneSrc = drone;
        }
    }
}
