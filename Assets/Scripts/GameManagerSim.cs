using System.Collections;
using UnityEngine;

namespace NightRoom
{
    public partial class GameManager
    {
        bool scareRunning;

        // ---------- ゲームの進行 ----------
        void Simulate(float dt)
        {
            t += dt;
            if (t >= Total) { Win(); return; }
            int h = Hour; float speed = 1f + h * tune.speedPerHour;

            // 懐中電灯: 扉の前で覗いている間だけ減る。部屋にいると充電される
            if (view == ViewState.Door && !dead)
            {
                battery -= tune.batteryDrain * dt;
                if (battery <= 0f) { battery = 0f; dead = true; AudioSourceUtil.Play(ProcAudio.Growl(), .25f); }
            }
            else if (view == ViewState.Room)
            {
                battery = Mathf.Min(100f, battery + tune.batteryRecharge * dt);
                if (dead && battery >= 25f) dead = false;
            }

            // オルゴール
            if (winding) box = Mathf.Min(100f, box + tune.windRate * dt);
            box = Mathf.Max(0f, box - (tune.boxDrain + h * tune.boxDrainPerHour) * dt);
            if (box > 0f)
            {
                noteT += dt;
                float iv = .34f + (1f - box / 100f) * .55f;
                if (noteT >= iv)
                {
                    noteT = 0f;
                    float f = Mel[noteIdx++ % Mel.Length] * (1f - (1f - box / 100f) * .09f);
                    AudioSourceUtil.Play(ProcAudio.Note(f), view == ViewState.Room ? .5f : .28f);
                }
                mimi = Mathf.Max(0f, mimi - dt * 2.5f);
            }
            else
            {
                mimi += dt;
                if (UnityEngine.Random.value < dt * .5f) AudioSourceUtil.Play(ProcAudio.Whisper(), .5f);
                if (mimi > Mathf.Max(6f, tune.dollBase - h * tune.dollPerHour)) { Jump(-1); return; }
            }

            // 停電(2時以降)
            if (blackT > 0f) blackT -= dt;
            else if (h >= 2 && UnityEngine.Random.value < dt * .018f * (h - 1)) { blackT = UnityEngine.Random.Range(4f, 7f); AudioSourceUtil.Play(ProcAudio.Whisper(), .6f); }

            // ニセの目(1時以降): 見に行くと消える
            if (fakeDoor >= 0) { fakeT -= dt; if (fakeT <= 0f || (view == ViewState.Door && viewDoor == fakeDoor)) fakeDoor = -1; }
            else if (view == ViewState.Room && h >= 1 && UnityEngine.Random.value < dt * .08f)
            { int i = UnityEngine.Random.Range(0, 3); if (m[i].stage < 3) { fakeDoor = i; fakeT = UnityEngine.Random.Range(1.2f, 2.2f); } }

            // 一瞬だけ顔が映る(3時以降)
            if (flashT > 0f) flashT -= dt;
            else if (h >= 3 && Lit && m[viewDoor].stage == 0 && UnityEngine.Random.value < dt * .04f) flashT = .07f;

            // 敵
            bool danger = false;
            for (int i = 0; i < 3; i++)
            {
                var e = m[i];
                bool watched = Lit && viewDoor == i;
                if (watched)
                {
                    e.grace = 0f;
                    if (e.stage > 0)
                    {
                        e.watch += dt;
                        float need = e.kind == 1 ? tune.watchNeedRound : tune.watchNeed;
                        if (e.watch > need)
                        {
                            e.watch = 0f; e.stage = Mathf.Max(0, e.stage - (e.kind == 1 ? 1 : 2));
                            e.wait = UnityEngine.Random.Range(tune.afterRetreat.x, tune.afterRetreat.y) / speed;
                            AudioSourceUtil.Play(ProcAudio.Growl(), .6f);
                            if (e.stage == 0 && e.kind == 2 && UnityEngine.Random.value < .45f) SneakToOtherDoor(i, speed);
                        }
                    }
                    else e.watch = 0f;
                    continue;
                }
                e.watch = Mathf.Max(0f, e.watch - dt);
                if (e.stage < 4)
                {
                    e.wait -= dt * speed;
                    if (e.wait <= 0f)
                    {
                        e.stage++;
                        Vector2 r = e.kind == 1 ? tune.advanceRound : tune.advance;
                        e.wait = UnityEngine.Random.Range(r.x, r.y) / speed;
                        AudioSourceUtil.Play(ProcAudio.Thump(), .25f + .15f * e.stage);
                        if (e.stage == 4) e.grace = UnityEngine.Random.Range(tune.finalGrace.x, tune.finalGrace.y) / (1f + h * .08f);
                    }
                }
                else
                {
                    danger = true; e.grace -= dt;
                    if (e.grace <= 0f) { Jump(i); return; }
                }
            }
            if (danger) { rattleT -= dt; if (rattleT <= 0f) { rattleT = UnityEngine.Random.Range(.9f, 1.5f); AudioSourceUtil.Play(ProcAudio.Rattle(), .6f); } }
        }

        void SneakToOtherDoor(int from, float speed)
        {
            for (int tries = 0; tries < 4; tries++)
            {
                int j = UnityEngine.Random.Range(0, 3);
                if (j == from || m[j].stage >= 2) continue;
                m[j].stage = 2; m[j].wait = UnityEngine.Random.Range(4f, 7f) / speed;
                return;
            }
        }

        void Win()
        {
            phase = Phase.Win; view = ViewState.Room; camCtl.SnapToRoom();
            if (droneSrc) droneSrc.Stop();
            lightMask.enabled = false; blackout.enabled = false; flashFace.enabled = false; dollSr.enabled = false;
            hud.ShowWin();
            StartCoroutine(Chime());
        }

        IEnumerator Chime()
        {
            foreach (float f in new float[] { 523, 659, 784, 1047 }) { AudioSourceUtil.Play(ProcAudio.Note(f), .8f); yield return new WaitForSeconds(.25f); }
        }

        // ---------- ジャンプスケア ----------
        void Jump(int who)
        {
            if (phase != Phase.Playing) return;
            phase = Phase.Lose; scareRunning = true;
            if (droneSrc) droneSrc.Stop();
            StartCoroutine(JumpRoutine(who));
        }

        IEnumerator JumpRoutine(int who)
        {
            camCtl.SnapToRoom(); view = ViewState.Room;
            lightMask.enabled = false; blackout.enabled = false; flashFace.enabled = false;
            AudioSourceUtil.Play(ProcAudio.Scream(), 1f);
            scare.sprite = who >= 0 ? CreepyArt.Monster(who) : CreepyArt.Doll();
            scare.enabled = true; redFlash.enabled = true;
            Transform cam = camCtl.cam.transform;
            float k = 0f;
            while (k < 1.5f)
            {
                k += Time.deltaTime;
                float e = Mathf.Min(1f, k / .28f);
                e = 1f - (1f - e) * (1f - e);
                Vector3 sh = new Vector3(UnityEngine.Random.Range(-.25f, .25f), UnityEngine.Random.Range(-.25f, .25f), 0f) * (k < 1.3f ? 1f : 0f);
                scare.transform.position = new Vector3(cam.position.x, cam.position.y - 5.2f, 0f) + sh;
                scare.transform.localScale = Vector3.one * Mathf.Lerp(.9f, 2.25f, e);
                float fl = k < .1f ? .85f : Mathf.Max(0f, .45f - k * .3f);
                redFlash.color = new Color(.78f, .08f, .14f, fl);
                // 画面が一瞬反転して見える(白いちらつき)
                if (((int)(k * 30f)) % 7 == 0) redFlash.color = new Color(1f, 1f, 1f, .5f);
                grainSr.color = new Color(1, 1, 1, .55f);
                yield return null;
            }
            scare.enabled = false; redFlash.enabled = false;
            scareRunning = false;
            string why = who < 0 ? "オルゴールが止まった。人形は、すぐ後ろにいた。" :
                who == 0 ? "ノッポが目の前にいた。扉から目を離しすぎた。" :
                who == 1 ? "まんまるの大きな目が、こちらを見ていた。" : "つのっこは、別の扉から来ていた。";
            hud.ShowLose(who >= 0 ? why : why);
        }

        // ---------- 見た目の更新 ----------
        void UpdateVisuals()
        {
            Camera cam = camCtl.cam;
            float size = cam.orthographicSize, aspect = cam.aspect;
            Vector3 cp = new Vector3(cam.transform.position.x, cam.transform.position.y, 0f);
            float time = Time.time;
            bool lit = Lit;

            // 停電
            float targetBlack = blackT > 0f ? .93f : 0f;
            blackAlpha = Mathf.MoveTowards(blackAlpha, targetBlack, Time.deltaTime * 4f);
            // 扉の前で電池切れ: ほぼ真っ暗
            bool deadDoor = view == ViewState.Door && !lit;
            float a = Mathf.Max(blackAlpha, deadDoor ? .985f : 0f);
            blackout.enabled = a > .01f;
            blackout.transform.position = cp;
            blackout.transform.localScale = new Vector3(size * 2f * aspect * 1.3f, size * 2.6f, 1f);
            blackout.color = new Color(0, 0, 0, a);

            // 懐中電灯の光(電池が少ないとチカチカ)
            lightMask.enabled = lit;
            if (lit)
            {
                float fl = battery < 25f && UnityEngine.Random.value < .18f ? .4f : 1f;
                float s = size * 2.6f * (fl < 1f ? 1.12f : 1f);
                lightMask.transform.position = cp + new Vector3(Mathf.Sin(time * 1.3f) * .03f * size, Mathf.Cos(time * 1.7f) * .02f * size, 0f);
                lightMask.transform.localScale = Vector3.one * s;
            }

            // 敵
            for (int i = 0; i < 3; i++)
            {
                bool watched = lit && viewDoor == i;
                bool eyes = (m[i].stage >= 3 && !watched) || fakeDoor == i;
                views[i].Apply(m[i].stage, watched ? 1f : 0f, eyes && !deadDoor || (eyes && blackAlpha > .1f), blackAlpha > .1f, time);
            }

            // 一瞬だけ映る顔
            flashFace.enabled = flashT > 0f && lit;
            if (flashFace.enabled)
            {
                flashFace.sprite = CreepyArt.Monster(viewDoor);
                flashFace.transform.position = cp + new Vector3(0, -size * 1.05f, 0);
                flashFace.transform.localScale = Vector3.one * size * .42f;
                flashFace.color = new Color(1, 1, 1, .85f);
            }

            // 棚の人形: オルゴールが弱るほど見え、止まると近づく
            float vis = Mathf.Clamp01(1f - box / 30f);
            float p = Mathf.Clamp01(mimi / Mathf.Max(6f, tune.dollBase - Hour * tune.dollPerHour));
            dollSr.enabled = vis > .01f && view == ViewState.Room;
            if (dollSr.enabled)
            {
                dollSr.transform.position = Vector3.Lerp(new Vector3(7.6f, 1.0f, 0), new Vector3(0.2f, -2.5f, 0), p);
                dollSr.transform.localScale = Vector3.one * Mathf.Lerp(.32f, .95f, p);
                dollSr.color = new Color(1, 1, 1, .25f + .75f * vis);
                dollSr.sortingOrder = blackAlpha > .1f ? 221 : 50;
            }
            if (boxLamp) boxLamp.color = new Color(.91f, .7f, .35f, box > 0f ? .6f + .2f * Mathf.Sin(time * 3f) : 0f);
        }

        void UpdateHud()
        {
            int h = Hour;
            hud.SetClock((h == 0 ? 12 : h) + ":" + Mathf.FloorToInt((t % tune.hourSeconds) / tune.hourSeconds * 60f).ToString("00") + " AM");
            hud.SetBars(battery / 100f, battery < 25f || dead, box / 100f, box < 25f);
            string msg = "扉をクリックして近づこう。懐中電灯で影を追い払える"; bool alert = false;
            bool knob = false; for (int i = 0; i < 3; i++) if (m[i].stage == 4 && !(Lit && viewDoor == i)) knob = true;
            if (box <= 0f) { msg = "オルゴールが止まった。人形が動いている。巻いて"; alert = true; }
            else if (blackT > 0f) { msg = "停電。目だけが光っている"; alert = true; }
            else if (knob) { msg = "ドアノブが揺れている。誰かが入ってくる"; alert = true; }
            else if (dead) { msg = "電池切れ。部屋に戻って充電を待とう"; alert = true; }
            else if (view == ViewState.ZoomIn) msg = "扉に近づいている…";
            else if (view == ViewState.Door) msg = "見つめ続けると遠ざかる。右クリックか0で部屋へ";
            else if (m[0].stage >= 3 || m[1].stage >= 3 || m[2].stage >= 3) msg = "扉の向こうに気配がする";
            hud.SetHint(msg, alert);
        }

        void UpdateGrain()
        {
            if (grainTex == null) return;
            grainTick += Time.deltaTime;
            if (grainTick < .05f) return;
            grainTick = 0f;
            var px = new Color32[grainTex.width * grainTex.height];
            for (int i = 0; i < px.Length; i++) { byte v = (byte)UnityEngine.Random.Range(0, 255); px[i] = new Color32(v, v, v, 255); }
            grainTex.SetPixels32(px); grainTex.Apply();
            if (!scareRunning) grainSr.color = new Color(1, 1, 1, blackAlpha > .1f ? .16f : .09f);
            grainSr.transform.position = new Vector3(camCtl.cam.transform.position.x, camCtl.cam.transform.position.y, 0f);
            float sz = camCtl.cam.orthographicSize;
            grainSr.transform.localScale = new Vector3(sz * 2f * camCtl.cam.aspect * 1.3f, sz * 2.6f, 1f);
        }
    }
}
