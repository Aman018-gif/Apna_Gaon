using UnityEngine;
using UnityEngine.UI;

namespace MohallaHero
{
    public class MainMenuScreen : UIScreen
    {
        GameObject mainButtons, newGamePanel;
        Button continueButton;
        Text continueInfo;
        InputField nameField;
        Gender gender = Gender.Male;
        Button maleBtn, femaleBtn;

        public override bool CloseOnEscape => false;

        protected override void Build()
        {
            var backdrop = UIFactory.Rect(transform, "Backdrop").Stretch().gameObject.AddComponent<RawImage>();
            backdrop.texture = MakeBackdrop();
            var shade = UIFactory.Panel(transform, "Shade", new Color(0.1f, 0.03f, 0.2f, 0.35f), false);
            shade.rectTransform.Stretch();

            var title = UIFactory.Label(transform, "MOHALLA HERO", 124, TextAnchor.MiddleCenter, Theme.Accent, FontStyle.Bold);
            title.rectTransform.Anchor(new Vector2(0, 0.7f), new Vector2(1, 0.93f), Vector2.zero, Vector2.zero);
            var ol = title.gameObject.AddComponent<Outline>();
            ol.effectColor = new Color(0.55f, 0.03f, 0.3f, 1f);
            ol.effectDistance = new Vector2(6, -6);
            var sub = UIFactory.Label(transform, "Choices That Count  ·  Har choice ka asar hota hai!", 36, TextAnchor.MiddleCenter, Theme.Text, FontStyle.Bold);
            sub.rectTransform.Anchor(new Vector2(0, 0.64f), new Vector2(1, 0.71f), Vector2.zero, Vector2.zero);
            sub.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.6f);

            // main buttons
            var mb = UIFactory.Rect(transform, "MainButtons");
            mb.Place(new Vector2(0.5f, 0.5f), new Vector2(0, -80), new Vector2(520, 360));
            mb.pivot = new Vector2(0.5f, 0.5f);
            mb.VLayout(16, 0, TextAnchor.UpperCenter);
            continueButton = UIFactory.Button(mb, "Continue", () => gm.ContinueGame(), 34, Theme.Teal);
            continueButton.Size(-1, 80);
            continueInfo = UIFactory.Label(mb, "", 20, TextAnchor.MiddleCenter, Theme.Text);
            UIFactory.Button(mb, "New Game", ShowNewGame, 34, Theme.Magenta).Size(-1, 80);
            UIFactory.Button(mb, "Quit", Quit, 28, Theme.Button).Size(-1, 64);
            mainButtons = mb.gameObject;

            // new game panel
            var ng = UIFactory.Panel(transform, "NewGame", Theme.Panel);
            ng.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0, -90), new Vector2(680, 470));
            ng.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            ng.VLayout(16, 30, TextAnchor.UpperCenter);
            UIFactory.Label(ng.transform, "Aap kaun ho? (Who are you?)", 36, TextAnchor.MiddleCenter, Theme.Accent, FontStyle.Bold);
            UIFactory.Label(ng.transform, "A newcomer who just moved into Shanti Nagar.", 22, TextAnchor.MiddleCenter, Theme.TextDim);
            nameField = CreateInputField(ng.transform);
            var genderRow = UIFactory.Rect(ng.transform, "Gender");
            genderRow.HLayout(16, 0, TextAnchor.MiddleCenter);
            maleBtn = UIFactory.Button(genderRow, "Ladka (Boy)", () => SetGender(Gender.Male), 26);
            maleBtn.Size(240, 64);
            femaleBtn = UIFactory.Button(genderRow, "Ladki (Girl)", () => SetGender(Gender.Female), 26);
            femaleBtn.Size(240, 64);
            var actions = UIFactory.Rect(ng.transform, "Actions");
            actions.HLayout(16, 0, TextAnchor.MiddleCenter);
            UIFactory.Button(actions, "Back", () => { newGamePanel.SetActive(false); mainButtons.SetActive(true); }, 26, Theme.ButtonDisabled).Size(190, 70);
            UIFactory.Button(actions, "Chalo! (Start)", StartNewGame, 30, Theme.Magenta).Size(300, 70);
            newGamePanel = ng.gameObject;
            newGamePanel.SetActive(false);
            SetGender(Gender.Male);

            var credit = UIFactory.Label(transform, "A game about civic sense, honesty, environment, safety and voting · All characters are fictional · Unity 6 · C#",
                19, TextAnchor.LowerCenter, Theme.Text);
            credit.rectTransform.Anchor(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 16), new Vector2(0, 50));
            credit.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.6f);
        }

        InputField CreateInputField(Transform parent)
        {
            var bg = UIFactory.Panel(parent, "NameField", new Color(1, 0.98f, 0.92f, 1f));
            bg.Size(-1, 70);
            var field = bg.gameObject.AddComponent<InputField>();
            var text = UIFactory.Label(bg.transform, "", 32, TextAnchor.MiddleLeft, Theme.CardText);
            text.rectTransform.Stretch(16);
            text.supportRichText = false;
            var placeholder = UIFactory.Label(bg.transform, "Apna naam likho", 32, TextAnchor.MiddleLeft, new Color(0.5f, 0.42f, 0.55f), FontStyle.Italic);
            placeholder.rectTransform.Stretch(16);
            field.textComponent = text;
            field.placeholder = placeholder;
            field.characterLimit = 14;
            field.targetGraphic = bg;
            return field;
        }

        void SetGender(Gender g)
        {
            gender = g;
            maleBtn.GetComponent<Image>().color = g == Gender.Male ? Theme.Teal : Theme.Button;
            femaleBtn.GetComponent<Image>().color = g == Gender.Female ? Theme.Teal : Theme.Button;
        }

        void ShowNewGame()
        {
            mainButtons.SetActive(false);
            newGamePanel.SetActive(true);
            nameField.text = "";
            nameField.Select();
            nameField.ActivateInputField();
        }

        void StartNewGame()
        {
            string n = nameField.text.Trim();
            if (string.IsNullOrEmpty(n)) n = gender == Gender.Male ? "Arjun" : "Priya";
            gm.NewGame(n, gender);
        }

        public override void OnOpen()
        {
            mainButtons.SetActive(true);
            newGamePanel.SetActive(false);
            bool has = SaveSystem.HasSave();
            continueButton.interactable = has;
            var save = has ? SaveSystem.Load() : null;
            continueInfo.text = save != null ? $"{save.playerName} · Din {save.day} · saved {save.savedAt}" : "Koi saved game nahi";
        }

        void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>A city skyline at dusk with apartment lights, a water tank and festival bunting, drawn procedurally.</summary>
        static Texture2D MakeBackdrop()
        {
            var c = new PixelCanvas(320, 180, 99);
            for (int y = 0; y < 180; y++)
            {
                Color sky = Color.Lerp(new Color(1f, 0.55f, 0.25f), new Color(0.32f, 0.12f, 0.5f), y / 150f);
                c.Rect(0, y, 320, 1, sky);
            }
            c.Circle(250, 92, 16, new Color(1f, 0.85f, 0.45f));
            // far skyline
            var rng = new System.Random(5);
            for (int x = 0; x < 320;)
            {
                int w = rng.Next(14, 30), h = rng.Next(40, 95);
                c.Rect(x, 30, w, h, new Color(0.38f, 0.2f, 0.45f));
                for (int wy = 38; wy < 30 + h - 6; wy += 8)
                    for (int wx = x + 3; wx < x + w - 3; wx += 6)
                        if (rng.NextDouble() < 0.45) c.Rect(wx, wy, 2, 3, new Color(1f, 0.85f, 0.45f));
                x += w + rng.Next(1, 5);
            }
            // near buildings
            for (int x = -10; x < 320;)
            {
                int w = rng.Next(30, 52), h = rng.Next(30, 60);
                Color wall = new[] { new Color(0.95f, 0.65f, 0.55f), new Color(0.6f, 0.82f, 0.85f), new Color(1f, 0.82f, 0.55f), new Color(0.75f, 0.68f, 0.95f) }[rng.Next(4)];
                c.Rect(x, 20, w, h, wall * 0.75f + new Color(0, 0, 0, 0.25f));
                for (int wy = 26; wy < 20 + h - 6; wy += 10)
                    for (int wx = x + 4; wx < x + w - 4; wx += 9)
                        c.Rect(wx, wy, 4, 5, rng.NextDouble() < 0.6 ? new Color(1f, 0.88f, 0.5f) : new Color(0.25f, 0.2f, 0.35f));
                c.Ellipse(x + w - 9, 20 + h + 4, 5, 5, new Color(0.12f, 0.12f, 0.12f));   // water tank
                x += w + rng.Next(2, 8);
            }
            // road
            c.Rect(0, 0, 320, 20, new Color(0.25f, 0.25f, 0.3f));
            for (int x = 4; x < 320; x += 20) c.Rect(x, 9, 10, 2, new Color(0.95f, 0.95f, 0.85f));
            // bunting
            var cols = new[] { new Color(0.9f, 0.08f, 0.48f), new Color(1f, 0.76f, 0.05f), new Color(0f, 0.65f, 0.65f), new Color(1f, 0.5f, 0.07f) };
            for (int x = 0; x < 320; x++)
            {
                int y = 128 + Mathf.RoundToInt(Mathf.Sin(x / 320f * Mathf.PI * 3f) * 6);
                c.Set(x, y, new Color(0.2f, 0.15f, 0.25f));
                if (x % 8 == 0)
                    for (int k = 0; k < 4; k++) c.Rect(x + k / 2, y - k - 1, 4 - k, 1, cols[(x / 8) % cols.Length]);
            }
            return c.ToTexture();
        }
    }

    public class PauseScreen : UIScreen
    {
        protected override void Build()
        {
            var body = Window("Ruko zara! (Paused)", new Vector2(640, 760));
            body.VLayout(14, 10, TextAnchor.UpperCenter);
            UIFactory.Button(body, "Resume", () => ui.CloseModal(), 30, Theme.Teal).Size(-1, 76);
            UIFactory.Button(body, "Mohalla Phone", () => ui.Open(ui.Phone), 28, Theme.Magenta).Size(-1, 72);
            UIFactory.Button(body, "Map", () => ui.Open(ui.Map), 28).Size(-1, 72);
            UIFactory.Button(body, "Save Game", () => { gm.SaveGame(); ui.Notify("Game saved."); }, 28).Size(-1, 72);
            Button voiceBtn = null;
            voiceBtn = UIFactory.Button(body, VoiceLabel(), () =>
            {
                VoiceSystem.Enabled = !VoiceSystem.Enabled;
                if (!VoiceSystem.Enabled) gm.Voice.Stop();
                voiceBtn.SetLabel(VoiceLabel());
            }, 26);
            voiceBtn.Size(-1, 66);
            UIFactory.Button(body, "Save & Main Menu", () => { gm.SaveGame(); gm.QuitToMenu(); }, 28, Theme.AccentDark).Size(-1, 72);
            UIFactory.Label(body, "More settings (touch controls, subtitles, text size) in Phone → Settings.", 19, TextAnchor.UpperCenter, Theme.TextDim);
        }

        static string VoiceLabel() => VoiceSystem.Supported
            ? $"Character voices: {(VoiceSystem.Enabled ? "On" : "Off")}"
            : "Character voices: not supported here";
    }
}
