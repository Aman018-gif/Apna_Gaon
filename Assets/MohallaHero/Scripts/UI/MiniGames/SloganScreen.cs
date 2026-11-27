using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MohallaHero
{
    /// <summary>
    /// Mini-game 3: slogan-making for the protest placards. Pick an opening and an ending; peaceful, factual or funny
    /// slogans become placards, threatening ones are refused (and cost Civic Sense). Three different placards are needed.
    /// </summary>
    public class SloganScreen : UIScreen
    {
        Text preview, feedback, countText;
        RectTransform placardRow;
        readonly List<Image> startButtons = new List<Image>(), endButtons = new List<Image>();
        int start = -1, end = -1;
        readonly HashSet<int> made = new HashSet<int>();
        GameObject makeButton, doneButton;

        protected override void Build()
        {
            var body = Window("Placard Workshop: Naara Banao", new Vector2(1320, 900), true, Theme.Magenta);
            body.VLayout(14, 0, TextAnchor.UpperCenter);

            var board = UIFactory.Panel(body, "Placard", Theme.Card);
            board.Size(-1, 150);
            preview = UIFactory.Label(board.transform, "", 40, TextAnchor.MiddleCenter, Theme.CardText, FontStyle.Bold);
            preview.rectTransform.Stretch(18);

            var cols = UIFactory.Rect(body, "Columns");
            cols.HLayout(24, 0, TextAnchor.UpperCenter);
            cols.Size(-1, 380);
            Column(cols, "Shuruaat (start)", MiniGameData.SloganStarts, startButtons, i => { start = i; Refresh(); });
            Column(cols, "Ant (end)", MiniGameData.SloganEnds, endButtons, i => { end = i; Refresh(); });

            feedback = UIFactory.Label(body, "", 25, TextAnchor.MiddleCenter, Theme.Text);
            feedback.Size(-1, 70);
            var bottom = UIFactory.Rect(body, "Bottom");
            bottom.HLayout(20, 0, TextAnchor.MiddleCenter);
            bottom.Size(-1, 80);
            countText = UIFactory.Label(bottom, "", 26, TextAnchor.MiddleRight, Theme.Accent, FontStyle.Bold);
            countText.Size(260);
            placardRow = UIFactory.Rect(bottom, "Placards");
            placardRow.HLayout(6, 0, TextAnchor.MiddleLeft);
            placardRow.Size(160, 70);
            var make = UIFactory.Button(bottom, "Placard banao!", Make, 30, Theme.AccentDark);
            make.Size(340, 76);
            makeButton = make.gameObject;
            var done = UIFactory.Button(bottom, "Ho gaya!", () => ui.Close(this), 30, Theme.Teal);
            done.Size(260, 76);
            doneButton = done.gameObject;
        }

        void Column(RectTransform parent, string title, SloganPart[] parts, List<Image> buttons, System.Action<int> pick)
        {
            var col = UIFactory.Rect(parent, title);
            col.VLayout(10, 0, TextAnchor.UpperCenter);
            col.Size(600);
            UIFactory.Label(col, title, 24, TextAnchor.MiddleCenter, Theme.TextDim, FontStyle.Bold);
            for (int i = 0; i < parts.Length; i++)
            {
                int k = i;
                var b = UIFactory.Button(col, parts[i].text, () => pick(k), 25, Theme.Button);
                b.Size(-1, 74);
                buttons.Add(b.GetComponent<Image>());
            }
        }

        public override void OnOpen()
        {
            if (gm.Story.Get("placards") == 0) made.Clear();   // a new game
            start = end = -1;
            feedback.text = "Ek shuruaat aur ek ant chuno. Sach bolo, hasao bhi, par dhamki nahi!";
            Refresh();
        }

        void Refresh()
        {
            for (int i = 0; i < startButtons.Count; i++) startButtons[i].color = i == start ? Theme.Teal : Theme.Button;
            for (int i = 0; i < endButtons.Count; i++) endButtons[i].color = i == end ? Theme.Teal : Theme.Button;
            string a = start >= 0 ? MiniGameData.SloganStarts[start].text : "_____";
            string b = end >= 0 ? MiniGameData.SloganEnds[end].text : "_____";
            preview.text = a + " " + b;
            int count = gm.Story.Get("placards");
            countText.text = $"Placards: {count}/{Balance.PlacardsNeeded}";
            UIFactory.Clear(placardRow);
            for (int i = 0; i < Mathf.Min(count, Balance.PlacardsNeeded); i++) UIFactory.Icon(placardRow, SpriteFactory.Placard(i), 48);
            bool full = count >= Balance.PlacardsNeeded;
            makeButton.SetActive(!full);
            doneButton.SetActive(full || count > 0);
        }

        void Make()
        {
            if (start < 0 || end < 0) { feedback.text = "Pehle dono hisse chuno!"; return; }
            int key = start * 10 + end;
            if (made.Contains(key)) { feedback.text = "Yeh placard ban chuka hai. Kuch naya try karo!"; return; }
            var s = MiniGameData.SloganStarts[start];
            var e = MiniGameData.SloganEnds[end];
            var r = MiniGameData.EvaluateSlogan(s, e);
            feedback.text = (r.accepted ? "<color=#5ce65c><b>Placard ready!</b></color> " : "<color=#ff5555><b>Nahi chalega.</b></color> ") + r.feedback;
            if (r.accepted)
            {
                made.Add(key);
                gm.ApplyMiniGameResult(MiniGame.Slogan, r.civic, 0, r.courage, 0, "placards", gm.Story.Get("placards") + 1);
            }
            else gm.ApplyMiniGameResult(MiniGame.Slogan, r.civic, 0, r.courage, 0, null, 0);
            start = end = -1;
            Refresh();
            if (gm.Story.Get("placards") >= Balance.PlacardsNeeded)
                feedback.text += "\n<color=#ffc20e>Teeno placards ready! Ab Meera Didi se Gulmohar Park mein milo.</color>";
        }

        void Update()
        {
            if (ui.CurrentModal != this || ui.JustOpened) return;
            if (GameInput.Down(GameKey.Interact) && gm.Story.Get("placards") >= Balance.PlacardsNeeded) ui.Close(this);
        }
    }
}
