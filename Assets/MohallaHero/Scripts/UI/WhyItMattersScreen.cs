using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MohallaHero
{
    /// <summary>
    /// The "Why it matters" card shown after every decision that changes karma or trust: what you chose,
    /// the stat changes (green up, red down) and a short real-world explanation.
    /// </summary>
    public class WhyItMattersScreen : UIScreen
    {
        Image card, header;
        Text headerText, choiceText, whyText;
        RectTransform effectsRow;
        Action onClose;
        int openedFrame;

        public override bool CloseOnEscape => false;

        protected override void Build()
        {
            var dim = UIFactory.Panel(transform, "Dim", new Color(0.05f, 0.02f, 0.1f, 0.5f), false);
            dim.rectTransform.Stretch();
            card = UIFactory.Panel(transform, "Card", Theme.Card);
            card.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(1060, 560));
            card.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            UIFactory.AddShadow(card, 10);

            header = UIFactory.Panel(card.transform, "Header", Theme.Teal);
            header.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -84), Vector2.zero);
            var hrow = UIFactory.Rect(header.transform, "Row").Stretch(10);
            hrow.HLayout(14, 0, TextAnchor.MiddleLeft);
            UIFactory.Icon(hrow, SpriteFactory.AppIcon("hint"), 56);
            headerText = UIFactory.Label(hrow, "", 36, TextAnchor.MiddleLeft, Theme.Text, FontStyle.Bold);
            headerText.Size(900, 64);

            var body = UIFactory.Rect(card.transform, "Body");
            body.Anchor(Vector2.zero, Vector2.one, new Vector2(34, 26), new Vector2(-34, -100));
            body.VLayout(14, 0, TextAnchor.UpperLeft);
            choiceText = UIFactory.Label(body, "", 24, TextAnchor.UpperLeft, new Color(0.35f, 0.25f, 0.45f), FontStyle.Italic);
            effectsRow = UIFactory.Rect(body, "Effects");
            effectsRow.HLayout(10, 0, TextAnchor.MiddleLeft);
            effectsRow.Size(-1, 52);
            whyText = UIFactory.Label(body, "", 28, TextAnchor.UpperLeft, Theme.CardText);
            whyText.Size(-1, 220);
            var btn = UIFactory.Button(body, "Samajh gaya!  <size=20>(E)</size>", Close, 30, Theme.Magenta);
            btn.Size(-1, 74);
        }

        public void Show(string choice, List<ChoiceEffect> effects, string why, bool violence, Action then)
        {
            onClose = then;
            ui.Open(this);
            openedFrame = Time.frameCount;
            header.color = violence ? Theme.Bad : Theme.Teal;
            int net = 0;
            foreach (var e in effects) net += e.delta;
            headerText.text = violence ? "Hinsa ka nateeja (Violence has a cost)" : net >= 0 ? "Kyun zaroori hai? (Why it matters)" : "Socho zara... (Why it matters)";
            choiceText.text = $"You chose: \"{choice}\"";
            UIFactory.Clear(effectsRow);
            foreach (var e in effects)
            {
                var bg = e.delta >= 0 ? new Color(e.color.r * 0.85f, e.color.g * 0.85f, e.color.b * 0.85f, 1f) : new Color(0.75f, 0.15f, 0.2f, 1f);
                UIFactory.Chip(effectsRow, e.icon, $"{Format.Signed(e.delta)} {e.label}", bg, 22, 48);
            }
            effectsRow.gameObject.SetActive(effects.Count > 0);
            whyText.text = string.IsNullOrEmpty(why) ? "Har choice ka asar hota hai. Mohalla dekh raha hai!" : why;
            whyText.fontSize = gm.Settings.BigText ? 31 : 28;
        }

        void Close()
        {
            if (Time.frameCount == openedFrame) return;
            ui.Close(this);
            var a = onClose;
            onClose = null;
            a?.Invoke();
        }

        void Update()
        {
            if (Time.frameCount == openedFrame || ui.CurrentModal != this) return;
            if (GameInput.Down(GameKey.Interact)) Close();
        }
    }
}
