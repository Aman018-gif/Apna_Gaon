using System;
using UnityEngine;
using UnityEngine.UI;

namespace MohallaHero
{
    /// <summary>
    /// "Mohalla Phone": the game's menu as a smartphone (Tab / phone button). Home screen of app icons; each app is a page:
    /// Missions, Karma, Saboot (evidence), Bharosa (trust), Map, Pintu (hint), Settings and, after the vote, Result.
    /// </summary>
    public class PhoneScreen : UIScreen
    {
        RectTransform screen, home, page, pageContent;
        Text pageTitle, statusText;
        Image pageHeader;
        Action refreshPage;

        protected override void Build()
        {
            var dim = UIFactory.Panel(transform, "Dim", new Color(0.05f, 0.02f, 0.1f, 0.55f), false);
            dim.rectTransform.Stretch();
            var dimBtn = dim.gameObject.AddComponent<Button>();
            dimBtn.transition = Selectable.Transition.None;
            dimBtn.onClick.AddListener(() => ui.Close(this));

            // phone body
            var body = UIFactory.Panel(transform, "PhoneBody", new Color(0.1f, 0.1f, 0.14f, 1f));
            body.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(620, 1000));
            body.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            UIFactory.AddShadow(body, 12);
            var bodyBtn = body.gameObject.AddComponent<Button>();   // swallow clicks so they don't close the phone
            bodyBtn.transition = Selectable.Transition.None;

            var scr = UIFactory.Panel(body.transform, "Screen", new Color(0.13f, 0.05f, 0.27f, 1f));
            scr.rectTransform.Stretch(18);
            scr.rectTransform.offsetMax = new Vector2(-18, -54);
            scr.rectTransform.offsetMin = new Vector2(18, 70);
            screen = scr.rectTransform;
            var notch = UIFactory.Panel(body.transform, "Notch", new Color(0.05f, 0.05f, 0.07f, 1f));
            notch.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -14), new Vector2(160, 26));
            var homeBtn = UIFactory.Button(body.transform, "", ShowHome, 20, new Color(0.3f, 0.3f, 0.36f, 1f));
            homeBtn.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0), new Vector2(0, 16), new Vector2(170, 40));

            // status bar
            var status = UIFactory.Rect(screen, "Status");
            status.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(18, -40), new Vector2(-18, -6));
            statusText = UIFactory.Label(status, "", 20, TextAnchor.MiddleLeft, Theme.TextDim, FontStyle.Bold);
            statusText.rectTransform.Stretch();

            // home screen
            home = UIFactory.Rect(screen, "Home");
            home.Anchor(Vector2.zero, Vector2.one, new Vector2(20, 20), new Vector2(-20, -48));
            var wall = UIFactory.Label(home, "MOHALLA PHONE", 30, TextAnchor.UpperCenter, Theme.Accent, FontStyle.Bold);
            wall.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -60), new Vector2(0, -10));
            var grid = UIFactory.Rect(home, "Grid");
            grid.Anchor(Vector2.zero, Vector2.one, new Vector2(0, 0), new Vector2(0, -80));
            var g = grid.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = new Vector2(160, 170);
            g.spacing = new Vector2(16, 18);
            g.childAlignment = TextAnchor.UpperCenter;
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            g.constraintCount = 3;
            App(grid, "missions", "Missions", Theme.Magenta, ShowMissions);
            App(grid, "karma", "Karma", Theme.AccentDark, ShowKarma);
            App(grid, "evidence", "Saboot", new Color(0.85f, 0.6f, 0.05f), ShowEvidence);
            App(grid, "trust", "Bharosa", Theme.Teal, ShowTrust);
            App(grid, "map", "Map", new Color(0.2f, 0.45f, 0.85f), () => ui.Open(ui.Map));
            App(grid, "hint", "Pintu", new Color(0.95f, 0.75f, 0.1f), ShowHint);
            App(grid, "settings", "Settings", new Color(0.4f, 0.4f, 0.48f), ShowSettings);
            App(grid, "save", "Save", new Color(0.12f, 0.44f, 0.72f), () => { gm.SaveGame(); ui.Notify("Game saved!"); });
            App(grid, "karma", "Result", new Color(0.55f, 0.2f, 0.75f), () =>
            {
                if (gm.GameOver) ui.Open(ui.Ending);
                else ui.Notify("Result election ke baad aayega!");
            });

            // page
            page = UIFactory.Rect(screen, "Page");
            page.Anchor(Vector2.zero, Vector2.one, new Vector2(0, 0), new Vector2(0, -46));
            pageHeader = UIFactory.Panel(page, "Header", Theme.Magenta);
            pageHeader.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(10, -70), new Vector2(-10, 0));
            var back = UIFactory.Button(pageHeader.transform, "<", ShowHome, 30, new Color(0, 0, 0, 0.2f));
            back.GetComponent<RectTransform>().Place(new Vector2(0, 0.5f), new Vector2(8, 0), new Vector2(60, 54));
            pageTitle = UIFactory.Label(pageHeader.transform, "", 30, TextAnchor.MiddleLeft, Theme.Text, FontStyle.Bold);
            pageTitle.rectTransform.Anchor(Vector2.zero, Vector2.one, new Vector2(80, 0), new Vector2(-10, 0));
            pageContent = UIFactory.ScrollList(page, "List", 10);
            ((RectTransform)pageContent.parent.parent).Anchor(Vector2.zero, Vector2.one, new Vector2(14, 10), new Vector2(-14, -80));
        }

        void App(RectTransform grid, string icon, string label, Color color, Action open)
        {
            var cell = UIFactory.Rect(grid, "App_" + label);
            var btn = UIFactory.IconButton(cell, SpriteFactory.AppIcon(icon), color, 120, () => open());
            btn.GetComponent<RectTransform>().Place(new Vector2(0.5f, 1), Vector2.zero, new Vector2(120, 120));
            var t = UIFactory.Label(cell, label, 22, TextAnchor.LowerCenter, Theme.Text, FontStyle.Bold);
            t.rectTransform.Stretch();
        }

        public override void OnOpen() => ShowHome();

        void ShowHome()
        {
            home.gameObject.SetActive(true);
            page.gameObject.SetActive(false);
            refreshPage = null;
        }

        void OpenPage(string title, Color color, Action fill)
        {
            home.gameObject.SetActive(false);
            page.gameObject.SetActive(true);
            pageTitle.text = title;
            pageHeader.color = color;
            refreshPage = () => { UIFactory.Clear(pageContent); fill(); };
            refreshPage();
            ((ScrollRect)pageContent.parent.parent.GetComponent<ScrollRect>()).verticalNormalizedPosition = 1f;
        }

        void Update()
        {
            if (gm == null || !gm.InWorld) return;
            statusText.text = $"{Format.Clock(gm.Clock.Minute)}      ShantiNet 4G      Din {gm.Clock.Day}      {Format.Money(gm.Money)}";
        }

        // ------------------------------------------------------------------ helpers

        Text Para(string text, int size = 22, Color? color = null, FontStyle style = FontStyle.Normal)
        {
            var t = UIFactory.Label(pageContent, text, size, TextAnchor.UpperLeft, color ?? Theme.Text, style);
            return t;
        }

        RectTransform Card(Color? bg = null)
        {
            var c = UIFactory.Panel(pageContent, "Card", bg ?? new Color(1, 1, 1, 0.08f));
            c.VLayout(6, 14, TextAnchor.UpperLeft);
            return c.rectTransform;
        }

        // ------------------------------------------------------------------ apps

        void ShowMissions() => OpenPage("Missions", Theme.Magenta, () =>
        {
            var q = gm.Missions;
            foreach (var m in MissionDatabase.All)
            {
                bool done = q.IsDone(m);
                bool current = q.Current == m;
                bool locked = !done && !current;
                var c = Card(current ? new Color(0.9f, 0.08f, 0.48f, 0.25f) : new Color(1, 1, 1, 0.07f));
                string state = done ? "<color=#5ce65c>DONE</color>" : current ? "<color=#ffc20e>NOW</color>" : "<color=#9a8fb0>LOCKED</color>";
                UIFactory.Label(c, $"<b>Chapter {m.chapter}: {m.title}</b>   {state}", 24, TextAnchor.UpperLeft);
                if (locked) continue;
                UIFactory.Label(c, $"<i>\"{m.tagline}\"</i>", 20, TextAnchor.UpperLeft, Theme.Accent);
                if (current)
                {
                    if (!q.IsStarted(m)) UIFactory.Label(c, $"> Talk to {NpcDatabase.FirstName(m.giver)} to begin.", 21, TextAnchor.UpperLeft, Theme.Text);
                    else
                        foreach (var s in m.steps)
                        {
                            bool ok = q.IsDone(s);
                            UIFactory.Label(c, $"{(ok ? "<color=#5ce65c>[x]</color>" : "<color=#ffc20e>[ ]</color>")} {q.StepLabel(s)}", 21, TextAnchor.UpperLeft, ok ? Theme.TextDim : Theme.Text);
                        }
                }
                UIFactory.Label(c, $"Seekh: {m.lesson}", 19, TextAnchor.UpperLeft, Theme.TextDim, FontStyle.Italic);
            }
        });

        void ShowKarma() => OpenPage("Karma", Theme.AccentDark, () =>
        {
            int rating = gm.Rating;
            var top = Card(new Color(1f, 0.76f, 0.05f, 0.18f));
            UIFactory.Label(top, "<b>MOHALLA RATING</b>", 22, TextAnchor.UpperLeft, Theme.Accent);
            var row = UIFactory.Rect(top, "Row");
            row.HLayout(12, 0, TextAnchor.MiddleLeft);
            UIFactory.Stars(row, EndingEvaluator.Stars(rating), 40);
            UIFactory.Label(row, $"<b>{rating}/100</b>", 32, TextAnchor.MiddleLeft, Theme.Text).Size(160);
            UIFactory.Label(top, $"Neighbours with you: {gm.Trust.JoinedCount}/{TrustSystem.Convincible.Length}   Voters informed: {gm.Story.Get("voters_informed")}/{Balance.VoterDoors}" +
                                  (gm.Story.Get("violence") > 0 ? $"   <color=#ff5555>Violence: {gm.Story.Get("violence")}</color>" : ""), 19, TextAnchor.UpperLeft, Theme.TextDim);
            foreach (var s in KarmaSystem.All)
            {
                var a = gm.Karma.Get(s);
                var c = Card();
                var head = UIFactory.Rect(c, "Head");
                head.HLayout(10, 0, TextAnchor.MiddleLeft);
                var iconBg = UIFactory.Panel(head, "IconBg", a.color);
                iconBg.Size(52, 52);
                var ic = UIFactory.Icon(iconBg.transform, SpriteFactory.StatIcon(s), 36);
                ic.rectTransform.anchorMin = ic.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                ic.rectTransform.sizeDelta = new Vector2(36, 36);
                UIFactory.Label(head, $"<b>{a.displayName}</b> <size=18>({a.hindiName})</size>\n<color=#ffc20e>{a.xp} XP · Level {a.Level}: {a.Rank}</color>", 22, TextAnchor.MiddleLeft).Size(440);
                var fill = UIFactory.Bar(c, a.color, 500, 12);
                fill.fillAmount = a.LevelProgress;
                UIFactory.Label(c, a.description, 19, TextAnchor.UpperLeft, Theme.TextDim);
            }
            Para($"Total karma earned: +{gm.Karma.TotalGained}    lost: -{gm.Karma.TotalLost}", 19, Theme.TextDim);
        });

        void ShowEvidence() => OpenPage("Saboot (Evidence)", new Color(0.85f, 0.6f, 0.05f), () =>
        {
            int count = 0;
            foreach (var item in ItemDatabase.All)
            {
                if (!gm.HasItem(item.id)) continue;
                count++;
                var c = Card(item.evidence ? new Color(1f, 0.76f, 0.05f, 0.15f) : new Color(1, 1, 1, 0.07f));
                var head = UIFactory.Rect(c, "Head");
                head.HLayout(12, 0, TextAnchor.MiddleLeft);
                UIFactory.Icon(head, SpriteFactory.ItemIcon(item.id), 56);
                UIFactory.Label(head, $"<b>{item.name}</b>{(item.evidence ? "\n<color=#ffc20e><size=18>EVIDENCE</size></color>" : "")}", 23, TextAnchor.MiddleLeft).Size(430);
                UIFactory.Label(c, item.description, 20, TextAnchor.UpperLeft, Theme.TextDim);
            }
            if (count == 0) Para("Abhi tak koi saboot ya saamaan nahi. Chapter 4 mein yahan photo, bill aur RTI reply aayenge.", 22, Theme.TextDim);
            if (gm.Story.Is("photo_fake")) Para("<color=#ff5555>Note: your park photo was edited. People who notice will trust you less.</color>", 19);
        });

        void ShowTrust() => OpenPage("Bharosa (Trust)", Theme.Teal, () =>
        {
            Para($"Trust {Balance.TrustJoinThreshold}+ = joins the march. Needed: {Balance.SupportersNeeded}. Honest arguments raise trust; lies and threats lower it.", 19, Theme.TextDim);
            foreach (var d in NpcDatabase.All)
            {
                int trust = gm.Trust.Get(d.id);
                bool convincible = Array.IndexOf(TrustSystem.Convincible, d.id) >= 0;
                var c = Card(convincible && gm.Trust.HasJoined(d.id) ? new Color(0f, 0.65f, 0.65f, 0.25f) : new Color(1, 1, 1, 0.07f));
                var head = UIFactory.Rect(c, "Head");
                head.HLayout(12, 0, TextAnchor.MiddleLeft);
                var face = UIFactory.Icon(head, SpriteFactory.Character(d, 0, 0), 56);
                string joined = convincible ? (gm.Trust.HasJoined(d.id) ? "  <color=#5ce65c>SAATH HAI</color>" : "") : "";
                UIFactory.Label(head, $"<b>{d.name}</b>{joined}\n<size=18><color=#d8c8f0>{d.role}</color></size>", 22, TextAnchor.MiddleLeft).Size(330);
                UIFactory.Label(head, $"{trust}\n<size=16>{TrustSystem.Label(trust)}</size>", 22, TextAnchor.MiddleRight, Theme.Trust, FontStyle.Bold).Size(110);
                var bar = UIFactory.Bar(c, convincible ? Theme.Teal : new Color(0.5f, 0.6f, 0.8f), 500, 10);
                bar.fillAmount = trust / 100f;
            }
        });

        void ShowHint() => OpenPage("Pintu ke Hints", new Color(0.95f, 0.75f, 0.1f), () =>
        {
            var c = Card(new Color(1f, 0.76f, 0.05f, 0.18f));
            var head = UIFactory.Rect(c, "Head");
            head.HLayout(12, 0, TextAnchor.MiddleLeft);
            UIFactory.Icon(head, SpriteFactory.Character(NpcDatabase.Get(NpcId.Pintu), 0, 0), 64);
            UIFactory.Label(head, "<b>Pintu:</b> \"Bhai, sun. Pro tip incoming:\"", 22, TextAnchor.MiddleLeft).Size(420);
            UIFactory.Label(c, gm.Missions.Hint(), 24, TextAnchor.UpperLeft, Theme.Text);
            Para("Golden diamond = next goal. Orange dot on the map / \"!\" above a head = someone has work for you.", 19, Theme.TextDim);
            Para(Meme(), 21, Theme.Accent, FontStyle.Italic);
        });

        string Meme()
        {
            string[] memes =
            {
                "Meme of the day: \"Netaji: chaand pe WiFi. Mohalla: pehle naali saaf karo.\"",
                "Meme of the day: \"Me switching off fans in empty classrooms: main character energy.\"",
                "Meme of the day: \"Forward karne se pehle check karo. Chacha: *already forwarded*\"",
                "Meme of the day: \"Golden dustbin? Bhai humare park mein toh dustbin bhi nahi.\"",
            };
            return memes[(gm.Clock.Day + gm.Missions.CurrentChapter) % memes.Length];
        }

        void ShowSettings() => OpenPage("Settings", new Color(0.4f, 0.4f, 0.48f), () =>
        {
            Toggle(() => $"Character voices: {(VoiceSystem.Supported ? (VoiceSystem.Enabled ? "On" : "Off") : "not supported here")}", () =>
            {
                VoiceSystem.Enabled = !VoiceSystem.Enabled;
                if (!VoiceSystem.Enabled) gm.Voice.Stop();
            });
            Toggle(() => $"Subtitles (loudspeaker, crowd): {(gm.Settings.Subtitles ? "On" : "Off")}", () => gm.Settings.Subtitles = !gm.Settings.Subtitles);
            Toggle(() => $"Touch controls: {Settings.TouchLabel(gm.Settings.TouchMode)}", () => gm.Settings.TouchMode = (gm.Settings.TouchMode + 1) % 3);
            Toggle(() => $"Always run: {(gm.Settings.AlwaysRun ? "On" : "Off")}", () => gm.Settings.AlwaysRun = !gm.Settings.AlwaysRun);
            Toggle(() => $"Bigger dialogue text: {(gm.Settings.BigText ? "On" : "Off")}", () => gm.Settings.BigText = !gm.Settings.BigText);
            Para("Keyboard: WASD/arrows move · Shift run · E/Space talk & use · 1-4 choices · Tab phone · M map · H hint · F5 save · Esc menu", 18, Theme.TextDim);
        });

        void Toggle(Func<string> label, Action toggle)
        {
            Button b = null;
            b = UIFactory.Button(pageContent, label(), () => { toggle(); b.SetLabel(label()); }, 22, Theme.Button);
            b.Size(-1, 66);
        }
    }
}
