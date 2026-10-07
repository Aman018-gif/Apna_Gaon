using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MohallaHero
{
    /// <summary>
    /// The always-on "city app" HUD:
    /// top bar (place · chapter tracker · clock · wallet), karma chips (top-left), Mohalla Rating card with minimap
    /// (top-right), interaction prompt (bottom-centre), objective marker + edge arrow, floating karma pops.
    /// </summary>
    public class HudScreen : UIScreen
    {
        Text placeText, chapterText, trackerText, clockText, moneyText, ratingText, promptText, controlsText;
        RectTransform ratingStars, edgeArrow;
        GameObject promptPanel;
        readonly Dictionary<KarmaStat, (Text value, Text rank, Image bar, RectTransform chip)> chips = new Dictionary<KarmaStat, (Text, Text, Image, RectTransform)>();
        readonly List<(Text text, float time, Vector2 start)> pops = new List<(Text, float, Vector2)>();
        float chapterPulse;

        // minimap
        RawImage miniMap;
        RectTransform miniPlayer;
        Texture2D miniTex;
        float miniRepaint;
        readonly Dictionary<NpcId, Image> miniNpcs = new Dictionary<NpcId, Image>();
        const float MiniW = 300, MiniH = 190, MiniTilesW = 46;

        SpriteRenderer worldMarker;

        protected override void Build()
        {
            // ---- top bar
            var bar = UIFactory.Panel(transform, "TopBar", Theme.Panel);
            bar.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(12, -96), new Vector2(-12, -10));
            UIFactory.AddShadow(bar, 4);

            var left = UIFactory.Rect(bar.transform, "Place");
            left.Anchor(new Vector2(0, 0), new Vector2(0.27f, 1), new Vector2(16, 6), new Vector2(0, -6));
            left.HLayout(10, 0, TextAnchor.MiddleLeft);
            UIFactory.Icon(left, SpriteFactory.AppIcon("map"), 52);
            placeText = UIFactory.Label(left, "", 26, TextAnchor.MiddleLeft, Theme.Text, FontStyle.Bold);
            placeText.Size(380, 70);

            var center = UIFactory.Panel(bar.transform, "Tracker", new Color(1, 1, 1, 0.08f));
            center.rectTransform.Anchor(new Vector2(0.27f, 0), new Vector2(0.73f, 1), new Vector2(0, 8), new Vector2(0, -8));
            center.VLayout(0, 6, TextAnchor.MiddleCenter);
            chapterText = UIFactory.Label(center.transform, "", 24, TextAnchor.MiddleCenter, Theme.Accent, FontStyle.Bold);
            trackerText = UIFactory.Label(center.transform, "", 23, TextAnchor.MiddleCenter, Theme.Text);

            var right = UIFactory.Rect(bar.transform, "Status");
            right.Anchor(new Vector2(0.73f, 0), new Vector2(1, 1), new Vector2(0, 14), new Vector2(-16, -14));
            right.HLayout(10, 0, TextAnchor.MiddleRight);
            right.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;
            clockText = UIFactory.Chip(right, null, "", new Color(1, 1, 1, 0.12f), 24, 52);
            moneyText = UIFactory.Chip(right, null, "", Theme.AccentDark, 26, 52);

            // ---- karma chips (top-left, under the bar)
            var stats = UIFactory.Rect(transform, "Karma");
            stats.Place(new Vector2(0, 1), new Vector2(16, -110), new Vector2(350, 4 * 66 + 3 * 8));
            stats.VLayout(8, 0, TextAnchor.UpperLeft);
            foreach (var s in KarmaSystem.All)
            {
                var chip = UIFactory.Panel(stats, "Chip_" + s, new Color(Theme.Panel.r, Theme.Panel.g, Theme.Panel.b, 0.9f));
                chip.Size(-1, 66);
                chip.HLayout(10, 8, TextAnchor.MiddleLeft);
                var iconBg = UIFactory.Panel(chip.transform, "IconBg", Theme.StatColor(s));
                iconBg.Size(48, 48);
                var ic = UIFactory.Icon(iconBg.transform, SpriteFactory.StatIcon(s), 32);
                ic.rectTransform.anchorMin = ic.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                ic.rectTransform.sizeDelta = new Vector2(32, 32);
                var col = UIFactory.Rect(chip.transform, "Col");
                col.VLayout(1, 0, TextAnchor.MiddleLeft);
                col.Size(262);
                var value = UIFactory.Label(col, "", 21, TextAnchor.MiddleLeft, Theme.Text, FontStyle.Bold);
                value.horizontalOverflow = HorizontalWrapMode.Overflow;
                var rank = UIFactory.Label(col, "", 15, TextAnchor.MiddleLeft, Theme.TextDim);
                rank.horizontalOverflow = HorizontalWrapMode.Overflow;
                var fill = UIFactory.Bar(col, Theme.StatColor(s), 250, 8);
                chips[s] = (value, rank, fill, chip.rectTransform);
            }

            // ---- rating card + minimap (top-right)
            var card = UIFactory.Panel(transform, "Rating", Theme.Panel);
            card.rectTransform.Place(new Vector2(1, 1), new Vector2(-16, -110), new Vector2(MiniW + 28, MiniH + 112));
            UIFactory.AddShadow(card, 4);
            var title = UIFactory.Label(card.transform, "MOHALLA RATING", 18, TextAnchor.UpperLeft, Theme.TextDim, FontStyle.Bold);
            title.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -36), new Vector2(-16, -10));
            var starsRow = UIFactory.Rect(card.transform, "StarsRow");
            starsRow.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(14, -84), new Vector2(-14, -38));
            starsRow.HLayout(8, 0, TextAnchor.MiddleLeft);
            ratingStars = UIFactory.Stars(starsRow, 1, 30);
            ratingText = UIFactory.Label(starsRow, "", 24, TextAnchor.MiddleRight, Theme.Accent, FontStyle.Bold);
            ratingText.Size(112);

            var frame = UIFactory.Rect(card.transform, "MiniFrame");
            frame.Place(new Vector2(0.5f, 0), new Vector2(0, 14), new Vector2(MiniW, MiniH));
            frame.gameObject.AddComponent<RectMask2D>();
            miniMap = UIFactory.Rect(frame, "Map").Stretch().gameObject.AddComponent<RawImage>();
            foreach (var npc in NpcDatabase.All)
            {
                var dot = UIFactory.Panel(frame, "Npc", Color.yellow, false);
                dot.rectTransform.anchorMin = dot.rectTransform.anchorMax = Vector2.zero;
                dot.rectTransform.sizeDelta = new Vector2(9, 9);
                dot.gameObject.AddComponent<Outline>().effectColor = Color.black;
                miniNpcs[npc.id] = dot;
            }
            miniPlayer = UIFactory.Panel(frame, "You", new Color(1f, 0.15f, 0.35f), false).rectTransform;
            miniPlayer.anchorMin = miniPlayer.anchorMax = Vector2.zero;
            miniPlayer.sizeDelta = new Vector2(13, 13);
            miniPlayer.gameObject.AddComponent<Outline>().effectColor = Color.white;

            // ---- interaction prompt
            var pp = UIFactory.Panel(transform, "Prompt", new Color(Theme.Panel.r, Theme.Panel.g, Theme.Panel.b, 0.92f));
            pp.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0, 150), new Vector2(820, 64));
            UIFactory.AddShadow(pp, 3);
            promptText = UIFactory.Label(pp.transform, "", 28, TextAnchor.MiddleCenter);
            promptText.rectTransform.Stretch(8);
            promptPanel = pp.gameObject;

            // ---- keyboard help (desktop)
            controlsText = UIFactory.Label(transform,
                "<b>WASD</b> chalo  <b>Shift</b> daudo  <b>E</b> baat/use  <b>Tab</b> phone  <b>M</b> map  <b>H</b> Pintu hint  <b>Esc</b> menu",
                19, TextAnchor.LowerRight, Theme.TextDim);
            controlsText.rectTransform.Place(new Vector2(1, 0), new Vector2(-20, 14), new Vector2(980, 40));
            var sh = controlsText.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0, 0, 0, 0.9f);

            // ---- objective arrow at the screen edge
            var arrow = UIFactory.Icon(transform, SpriteFactory.EdgeArrow, 44);
            edgeArrow = arrow.rectTransform;
            edgeArrow.anchorMin = edgeArrow.anchorMax = Vector2.zero;
            edgeArrow.sizeDelta = new Vector2(52, 52);
        }

        // ------------------------------------------------------------------ effects

        public void FloatKarma(KarmaStat s, int delta)
        {
            if (!chips.TryGetValue(s, out var c)) return;
            var t = UIFactory.Label(transform, $"{Format.Signed(delta)} {Format.Stat(s)}", 30, TextAnchor.MiddleLeft,
                delta >= 0 ? Theme.StatColor(s) : Theme.Bad, FontStyle.Bold);
            t.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.8f);
            t.rectTransform.anchorMin = t.rectTransform.anchorMax = new Vector2(0, 1);
            t.rectTransform.sizeDelta = new Vector2(320, 40);
            var start = new Vector2(380, -110 - (int)s * 74 - 33);
            t.rectTransform.pivot = new Vector2(0, 0.5f);
            t.rectTransform.anchoredPosition = start;
            pops.Add((t, 1.6f, start));
        }

        public void PlayChapterJingle() => chapterPulse = 2f;

        // ------------------------------------------------------------------ update

        void Update()
        {
            if (gm == null || !gm.InWorld || gm.Player == null) return;

            var cell = WorldLayout.ToCell(gm.Player.transform.position);
            placeText.text = $"{WorldLayout.PlaceName(cell)}\n<size=18><color=#d8c8f0>Shanti Nagar · Ward 7</color></size>";

            var m = gm.Missions.Current;
            chapterText.text = m != null ? $"CHAPTER {m.chapter}: {m.title.ToUpperInvariant()}" : "SHANTI NAGAR · ELECTION RESULT";
            trackerText.text = gm.Missions.TrackerText();
            if (chapterPulse > 0f)
            {
                chapterPulse -= Time.unscaledDeltaTime;
                float k = 1f + Mathf.Sin(chapterPulse * 10f) * 0.06f * chapterPulse;
                chapterText.transform.localScale = new Vector3(k, k, 1);
            }
            else chapterText.transform.localScale = Vector3.one;

            clockText.text = $"Din {gm.Clock.Day} · {Format.Clock(gm.Clock.Minute)}";
            moneyText.text = Format.Money(gm.Money);

            foreach (var s in KarmaSystem.All)
            {
                var c = chips[s];
                var a = gm.Karma.Get(s);
                c.value.text = $"{Format.Stat(s)}  {a.xp}";
                c.rank.text = $"Level {a.Level} · {a.Rank}";
                c.bar.fillAmount = a.LevelProgress;
            }

            int rating = gm.Rating;
            UIFactory.SetStars(ratingStars, EndingEvaluator.Stars(rating));
            ratingText.text = $"{rating}/100";

            for (int i = pops.Count - 1; i >= 0; i--)
            {
                var (t, time, start) = pops[i];
                time -= Time.unscaledDeltaTime;
                if (time <= 0f) { Destroy(t.gameObject); pops.RemoveAt(i); continue; }
                pops[i] = (t, time, start);
                t.rectTransform.anchoredPosition = start + new Vector2(0, (1.6f - time) * 40f);
                var col = t.color;
                col.a = Mathf.Clamp01(time / 0.6f);
                t.color = col;
            }

            UpdateMinimap();
            UpdateObjective();

            var focus = gm.Player.Focus;
            bool showPrompt = focus != null && !gm.IsModalOpen;
            promptPanel.SetActive(showPrompt);
            bool touch = gm.Settings.TouchControlsVisible;
            if (showPrompt) promptText.text = touch ? $"{focus.Prompt}  <color=#ffc20e><b>[Tap]</b></color>" : $"<color=#ffc20e><b>[E]</b></color>  {focus.Prompt}";
            controlsText.enabled = !touch;
        }

        void UpdateMinimap()
        {
            var map = gm.Map;
            if (miniTex == null || miniTex.width != map.W) miniTex = MapTexture.Create(map);
            miniRepaint -= Time.unscaledDeltaTime;
            if (miniRepaint <= 0f)
            {
                miniRepaint = 2f;
                MapTexture.Paint(miniTex, map);
                miniMap.texture = miniTex;
            }

            float tilesW = MiniTilesW, tilesH = MiniTilesW * MiniH / MiniW;
            Vector2 p = gm.Player.transform.position;
            float x0 = Mathf.Clamp(p.x - tilesW / 2f, 0, map.W - tilesW);
            float y0 = Mathf.Clamp(p.y - tilesH / 2f, 0, map.H - tilesH);
            miniMap.uvRect = new Rect(x0 / map.W, y0 / map.H, tilesW / map.W, tilesH / map.H);
            float sx = MiniW / tilesW, sy = MiniH / tilesH;
            miniPlayer.anchoredPosition = new Vector2((p.x - x0) * sx, (p.y - y0) * sy);
            miniPlayer.SetAsLastSibling();

            foreach (var npc in gm.Npcs.All)
            {
                var dot = miniNpcs[npc.Def.id];
                Vector2 q = npc.transform.position;
                bool inView = !npc.Hidden && q.x >= x0 && q.x <= x0 + tilesW && q.y >= y0 && q.y <= y0 + tilesH;
                dot.enabled = inView;
                if (!inView) continue;
                dot.rectTransform.anchoredPosition = new Vector2((q.x - x0) * sx, (q.y - y0) * sy);
                dot.color = Guidance.WantsToTalk(gm, npc.Def.id) ? new Color(1f, 0.5f, 0f) : new Color(1f, 0.95f, 0.4f);
            }
        }

        /// <summary>A golden diamond above the next objective, and an arrow at the screen edge when it's off-screen.</summary>
        void UpdateObjective()
        {
            if (worldMarker == null && gm.WorldRoot != null)
                worldMarker = WorldObjects.CreateSprite("ObjectiveMarker", SpriteFactory.ObjectiveArrow, Vector2.zero, gm.WorldRoot, 30);
            var target = gm.IsModalOpen ? null : Guidance.ObjectiveTarget(gm);
            if (worldMarker != null)
            {
                worldMarker.enabled = target.HasValue;
                if (target.HasValue) worldMarker.transform.position = target.Value + new Vector2(0, 2.4f + Mathf.Sin(Time.time * 4f) * 0.12f);
            }
            if (!target.HasValue) { edgeArrow.gameObject.SetActive(false); return; }

            var cam = gm.CameraRig.Cam;
            Vector3 vp = cam.WorldToViewportPoint(target.Value + new Vector2(0, 1f));
            bool onScreen = vp.x > 0.05f && vp.x < 0.95f && vp.y > 0.08f && vp.y < 0.85f;
            edgeArrow.gameObject.SetActive(!onScreen);
            if (onScreen) return;
            var canvas = (RectTransform)ui.Root;
            Vector2 size = canvas.rect.size;
            Vector2 dir = new Vector2(vp.x - 0.5f, vp.y - 0.5f);
            if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up;
            float scale = Mathf.Min(0.42f / Mathf.Max(0.001f, Mathf.Abs(dir.x)), 0.36f / Mathf.Max(0.001f, Mathf.Abs(dir.y)));
            Vector2 pos = new Vector2(0.5f, 0.5f) + dir * scale;
            edgeArrow.anchoredPosition = new Vector2(pos.x * size.x, pos.y * size.y);
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;   // the sprite points up
            edgeArrow.localRotation = Quaternion.Euler(0, 0, angle);
        }
    }
}
