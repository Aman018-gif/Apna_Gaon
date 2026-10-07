using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MohallaHero
{
    /// <summary>
    /// Mini-game 1: waste sorting. Eight random items, three bins: Gila (wet, green), Sukha (dry, blue),
    /// Khatarnak (hazardous, red). Every answer explains where the item really goes. Keys 1–3 or tap a bin.
    /// </summary>
    public class WasteSortingScreen : UIScreen
    {
        Image itemIcon;
        Text itemName, progress, feedback, summary;
        GameObject bins, nextButton, finishButton;
        List<WasteItem> round;
        int index, correct;
        bool answered, finished;
        int shownFrame;

        public override bool CloseOnEscape => false;

        protected override void Build()
        {
            var body = Window("Kachra Sorting: Gila · Sukha · Khatarnak", new Vector2(1180, 860), false, new Color(0.18f, 0.55f, 0.25f));
            body.VLayout(16, 0, TextAnchor.UpperCenter);

            progress = UIFactory.Label(body, "", 24, TextAnchor.MiddleCenter, Theme.TextDim, FontStyle.Bold);

            var card = UIFactory.Panel(body, "Item", Theme.Card);
            card.Size(-1, 250);
            card.HLayout(30, 30, TextAnchor.MiddleCenter);
            itemIcon = UIFactory.Icon(card.transform, null, 180);
            itemName = UIFactory.Label(card.transform, "", 46, TextAnchor.MiddleLeft, Theme.CardText, FontStyle.Bold);
            itemName.Size(600, 180);

            feedback = UIFactory.Label(body, "", 26, TextAnchor.MiddleCenter, Theme.Text);
            feedback.Size(-1, 110);

            var binRow = UIFactory.Rect(body, "Bins");
            binRow.HLayout(30, 0, TextAnchor.MiddleCenter);
            binRow.Size(-1, 220);
            Bin(binRow, WasteBin.Wet, "1. GILA (Wet)", new Color(0.18f, 0.55f, 0.34f));
            Bin(binRow, WasteBin.Dry, "2. SUKHA (Dry)", new Color(0.11f, 0.44f, 0.72f));
            Bin(binRow, WasteBin.Hazardous, "3. KHATARNAK", new Color(0.8f, 0.16f, 0.16f));
            bins = binRow.gameObject;

            var next = UIFactory.Button(body, "Agla item  <size=20>(E)</size>", Next, 30, Theme.Magenta);
            next.Size(420, 76);
            nextButton = next.gameObject;
            summary = UIFactory.Label(body, "", 28, TextAnchor.MiddleCenter, Theme.Accent, FontStyle.Bold);
            var fin = UIFactory.Button(body, "Ho gaya!  <size=20>(E)</size>", Finish, 30, Theme.Teal);
            fin.Size(420, 76);
            finishButton = fin.gameObject;
        }

        void Bin(RectTransform parent, WasteBin bin, string label, Color color)
        {
            var b = UIFactory.Button(parent, "", () => Answer(bin), 24, color);
            b.Size(300, 210);
            var ic = UIFactory.Icon(b.transform, SpriteFactory.BinIcon(bin), 120);
            ic.rectTransform.anchorMin = ic.rectTransform.anchorMax = new Vector2(0.5f, 0.6f);
            ic.rectTransform.sizeDelta = new Vector2(120, 120);
            var t = b.GetComponentInChildren<Text>();
            t.text = label;
            t.alignment = TextAnchor.LowerCenter;
            t.rectTransform.offsetMin = new Vector2(6, 12);
        }

        public override void OnOpen()
        {
            round = MiniGameData.Pick(MiniGameData.Waste, Balance.WastePerRound, gm.Rng);
            index = 0;
            correct = 0;
            finished = false;
            ShowItem();
        }

        void ShowItem()
        {
            answered = false;
            shownFrame = Time.frameCount;
            var item = round[index];
            itemIcon.sprite = SpriteFactory.WasteIcon(item.icon);
            itemName.text = item.name;
            progress.text = $"Item {index + 1}/{round.Count}   ·   Sahi: {correct}";
            feedback.text = "Yeh kis dabbe mein jayega?";
            feedback.color = Theme.Text;
            bins.SetActive(true);
            nextButton.SetActive(false);
            summary.gameObject.SetActive(false);
            finishButton.SetActive(false);
        }

        void Answer(WasteBin bin)
        {
            if (answered || finished || Time.frameCount == shownFrame) return;
            answered = true;
            var item = round[index];
            bool ok = bin == item.bin;
            if (ok) correct++;
            feedback.text = (ok ? "<color=#5ce65c><b>Sahi!</b></color> " : $"<color=#ff5555><b>Oops!</b></color> Yeh <b>{MiniGameData.BinName(item.bin)}</b> mein jaata hai. ") + item.why;
            progress.text = $"Item {index + 1}/{round.Count}   ·   Sahi: {correct}";
            bins.SetActive(false);
            nextButton.SetActive(true);
            shownFrame = Time.frameCount;
        }

        void Next()
        {
            if (!answered || Time.frameCount == shownFrame) return;
            index++;
            if (index < round.Count) { ShowItem(); return; }
            finished = true;
            var (green, civic) = MiniGameData.WasteReward(correct, round.Count);
            nextButton.SetActive(false);
            summary.gameObject.SetActive(true);
            summary.text = $"{correct}/{round.Count} sahi!   Green +{green}{(civic > 0 ? $"   Civic +{civic}" : "")}\n<size=22><color=#fff8e7>Gila → khaad (compost), Sukha → recycling, Khatarnak → special handling.</color></size>";
            feedback.text = "";
            finishButton.SetActive(true);
            shownFrame = Time.frameCount;
        }

        void Finish()
        {
            if (!finished || Time.frameCount == shownFrame) return;
            var (green, civic) = MiniGameData.WasteReward(correct, round.Count);
            ui.Close(this);
            gm.ApplyMiniGameResult(MiniGame.WasteSorting, civic, green, 0, 0, "waste_best", Mathf.Max(gm.Story.Get("waste_best"), correct));
        }

        void Update()
        {
            if (ui.CurrentModal != this || ui.JustOpened || Time.frameCount == shownFrame) return;
            if (!answered && !finished)
            {
                if (GameInput.Down(GameKey.Choice1)) Answer(WasteBin.Wet);
                else if (GameInput.Down(GameKey.Choice2)) Answer(WasteBin.Dry);
                else if (GameInput.Down(GameKey.Choice3)) Answer(WasteBin.Hazardous);
            }
            else if (GameInput.Down(GameKey.Interact))
            {
                if (finished) Finish(); else Next();
            }
        }
    }
}
