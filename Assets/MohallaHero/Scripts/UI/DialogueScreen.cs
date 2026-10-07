using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MohallaHero
{
    /// <summary>
    /// Bottom dialogue box: portrait, name plate, line of text (which doubles as the subtitle of the spoken line)
    /// and up to four large choice buttons (keys 1–4, E/Space for the first one, or tap).
    /// </summary>
    public class DialogueScreen : UIScreen
    {
        Image portrait, namePlate;
        GameObject portraitFrame;
        Text nameText, bodyText;
        RectTransform options;
        readonly List<Action> actions = new List<Action>();
        int pickedFrame;

        public override bool CloseOnEscape => GameManager.I != null && GameManager.I.Dialogue.CanCancel;

        protected override void Build()
        {
            var box = UIFactory.Panel(transform, "Box", Theme.Panel);
            box.rectTransform.Anchor(new Vector2(0.04f, 0), new Vector2(0.96f, 0), new Vector2(0, 20), new Vector2(0, 400));
            UIFactory.AddShadow(box, 6);
            var stripe = UIFactory.Panel(box.transform, "Stripe", Theme.Accent);
            stripe.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(18, -8), new Vector2(-18, -2));

            var frame = UIFactory.Panel(box.transform, "PortraitFrame", Theme.PanelLight);
            frame.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(24, 0), new Vector2(200, 300));
            portrait = UIFactory.Icon(frame.transform, null, 160);
            portrait.rectTransform.Stretch(16);
            portraitFrame = frame.gameObject;

            namePlate = UIFactory.Panel(box.transform, "NamePlate", Theme.Magenta);
            namePlate.rectTransform.Anchor(new Vector2(0, 1), new Vector2(0, 1), new Vector2(248, -76), new Vector2(900, -20));
            nameText = UIFactory.Label(namePlate.transform, "", 30, TextAnchor.MiddleLeft, Theme.Text, FontStyle.Bold);
            nameText.rectTransform.Stretch(14);

            bodyText = UIFactory.Label(box.transform, "", 30, TextAnchor.UpperLeft);
            bodyText.rectTransform.Anchor(new Vector2(0, 0), new Vector2(0.58f, 1), new Vector2(250, 22), new Vector2(-14, -92));
            bodyText.lineSpacing = 1.1f;

            options = UIFactory.Rect(box.transform, "Options");
            options.Anchor(new Vector2(0.58f, 0), new Vector2(1, 1), new Vector2(8, 22), new Vector2(-24, -24));
            options.VLayout(10, 0, TextAnchor.LowerCenter);
        }

        /// <summary>Shows a page. Options are (label, action) pairs.</summary>
        public void Show(string speaker, Sprite face, bool isPlayer, string text, List<(string label, Action action)> opts)
        {
            if (!gameObject.activeSelf || ui.CurrentModal != this) ui.Open(this);
            bool big = gm.Settings.BigText;
            nameText.text = speaker;
            namePlate.gameObject.SetActive(!string.IsNullOrEmpty(speaker));
            namePlate.color = isPlayer ? Theme.Teal : Theme.Magenta;
            portrait.sprite = face;
            portraitFrame.SetActive(face != null);
            bodyText.rectTransform.offsetMin = new Vector2(face != null ? 250 : 40, 22);
            bodyText.fontSize = big ? 34 : 30;
            bodyText.fontStyle = string.IsNullOrEmpty(speaker) ? FontStyle.Italic : FontStyle.Normal;
            bodyText.text = text;

            UIFactory.Clear(options);
            actions.Clear();
            bool single = opts.Count == 1;
            for (int i = 0; i < opts.Count; i++)
            {
                var a = opts[i].action;
                actions.Add(a);
                string label = single ? $"{opts[i].label}  <size=20><color=#ffe9a8>(E)</color></size>" : $"<color=#ffc20e>{i + 1}.</color> {opts[i].label}";
                var b = UIFactory.Button(options, label, () => Pick(a), big ? 27 : 24, single ? Theme.AccentDark : Theme.Button);
                b.GetComponentInChildren<Text>().alignment = single ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
                b.Size(-1, opts.Count > 3 ? 70 : 78);
            }
            pickedFrame = Time.frameCount;
        }

        void Pick(Action a)
        {
            if (Time.frameCount == pickedFrame) return;   // the key press that opened this page must not also pick
            pickedFrame = Time.frameCount;
            a?.Invoke();
        }

        void Update()
        {
            if (ui.JustOpened || Time.frameCount == pickedFrame || actions.Count == 0 || ui.CurrentModal != this) return;
            if (GameInput.Down(GameKey.Interact)) { Pick(actions[0]); return; }
            for (int i = 0; i < 4 && i < actions.Count; i++)
                if (GameInput.Down(GameKey.Choice1 + i)) { Pick(actions[i]); return; }
        }
    }
}
