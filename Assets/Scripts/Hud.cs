using UnityEngine;

namespace NightRoom
{
    /// <summary>
    /// 画面の文字とゲージ。UnityEngine.UI(uGUI)パッケージに依存しない OnGUI 方式なので、
    /// どのプロジェクトでも追加設定なしで動く。
    /// </summary>
    public class Hud : MonoBehaviour
    {
        Font font;
        GUIStyle sClock, sLabel, sHint, sTitle, sBody, sTap;
        string clock = "12:00 AM", hint = "";
        bool hintAlert;
        float battery01 = 1f, box01 = 1f;
        bool lowB, lowM;
        bool overlayOn;
        string title = "", body = "", tap = "";
        Color titleColor = Color.white;
        bool styled;

        public static Hud Create()
        {
            var go = new GameObject("HUD");
            return go.AddComponent<Hud>();
        }

        void Awake()
        {
            // Unity標準フォントは日本語非対応のため、OSの日本語フォントを探す
            font = Font.CreateDynamicFontFromOSFont(new[] { "Yu Gothic UI", "Yu Gothic", "Meiryo", "Hiragino Sans", "Hiragino Kaku Gothic ProN", "Noto Sans CJK JP", "Arial" }, 32);
        }

        GUIStyle Style(int size, TextAnchor al, Color c, bool wrap = false)
        {
            var s = new GUIStyle(GUI.skin.label);
            s.font = font; s.fontSize = size; s.alignment = al; s.wordWrap = wrap;
            s.normal.textColor = c;
            return s;
        }

        void BuildStyles()
        {
            styled = true;
            sClock = Style(36, TextAnchor.UpperLeft, new Color(.91f, .7f, .35f));
            sLabel = Style(18, TextAnchor.UpperLeft, new Color(.6f, .62f, .7f));
            sHint = Style(20, TextAnchor.UpperLeft, new Color(.55f, .58f, .65f));
            sTitle = Style(64, TextAnchor.MiddleCenter, Color.white);
            sBody = Style(24, TextAnchor.UpperCenter, new Color(.65f, .67f, .75f), true);
            sTap = Style(26, TextAnchor.MiddleCenter, new Color(.91f, .7f, .35f));
        }

        static void Rect(Rect r, Color c)
        {
            var old = GUI.color; GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
        }

        void Bar(float x, float y, float v01, bool low)
        {
            Rect(new Rect(x, y, 300, 12), new Color(.09f, .1f, .15f));
            Rect(new Rect(x, y, 300 * Mathf.Clamp01(v01), 12), low ? new Color(.78f, .13f, .18f) : new Color(.5f, .69f, .41f));
        }

        void OnGUI()
        {
            if (!styled) BuildStyles();
            if (Event.current.type != EventType.Repaint) return;
            // 高さ720を基準に拡大縮小
            float s = Screen.height / 720f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
            float w = Screen.width / s;

            GUI.Label(new Rect(24, 12, 300, 50), clock, sClock);
            GUI.Label(new Rect(330, 14, 300, 24), "懐中電灯", sLabel);
            Bar(330, 42, battery01, lowB);
            GUI.Label(new Rect(680, 14, 300, 24), "オルゴール", sLabel);
            Bar(680, 42, box01, lowM);
            sHint.normal.textColor = hintAlert ? new Color(.9f, .28f, .25f) : new Color(.55f, .58f, .65f);
            GUI.Label(new Rect(24, 64, w - 48, 30), hint, sHint);

            if (overlayOn)
            {
                Rect(new Rect(0, 0, w, 720), new Color(.01f, .012f, .03f, .92f));
                sTitle.normal.textColor = titleColor;
                GUI.Label(new Rect(w * .5f - 500, 150, 1000, 120), title, sTitle);
                GUI.Label(new Rect(w * .5f - 450, 290, 900, 260), body, sBody);
                GUI.Label(new Rect(w * .5f - 450, 590, 900, 50), tap, sTap);
            }
        }

        public void SetClock(string s) { clock = s; }
        public void SetBars(float b01, bool lb, float m01, bool lm) { battery01 = b01; lowB = lb; box01 = m01; lowM = lm; }
        public void SetHint(string s, bool alert) { hint = s; hintAlert = alert; }

        void Overlay(bool on, string t, string b, string tp, Color c) { overlayOn = on; title = t; body = b; tap = tp; titleColor = c; }

        public void ShowTitle()
        {
            Overlay(true, "ねむれない子供部屋",
                "午前0時から6時まで、3つの扉の向こうから影が近づいてくる。\n扉をクリックすると、ゆっくり近づいて覗ける。懐中電灯で影を追い払おう。\nオルゴールは部屋にいる間だけ、長押しで巻ける。止まると棚の人形が動き出す。\n\n1・2・3 扉へ / 右クリック・0・Esc 部屋へ / W・オルゴール長押し 巻く",
                "クリックではじめる", new Color(.9f, .88f, .8f));
        }
        public void ShowWin() { Overlay(true, "午前6時", "朝日がカーテンから差しこんだ。影はもういない。", "クリックでもう一度", new Color(.91f, .7f, .35f)); }
        public void ShowLose(string why) { Overlay(true, "GAME OVER", why, "クリックでもう一度", new Color(.78f, .13f, .18f)); }
        public void HideOverlay() { Overlay(false, "", "", "", Color.white); }
    }
}
