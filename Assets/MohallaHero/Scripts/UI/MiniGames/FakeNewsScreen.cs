using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MohallaHero
{
    /// <summary>
    /// Mini-game 2: fake-news spotting with Chacha. Six forwarded messages; for each, optionally check the source,
    /// then decide: Sach (real) or Afwaah (rumour). Every answer explains the tell-tale signs.
    /// </summary>
    public class FakeNewsScreen : UIScreen
    {
        Text sender, headline, clue, feedback, progress, summary;
        GameObject answerRow, checkButton, nextButton, finishButton;
        List<NewsItem> round;
        int index, correct;
        bool answered, finished;
        int shownFrame;

        public override bool CloseOnEscape => false;

        protected override void Build()
        {
            var body = Window("Fact-Check Chacha: Sach ya Afwaah?", new Vector2(1180, 880), false, Theme.Teal);
            body.VLayout(14, 0, TextAnchor.UpperCenter);
            progress = UIFactory.Label(body, "", 24, TextAnchor.MiddleCenter, Theme.TextDim, FontStyle.Bold);

            // chat bubble
            var bubble = UIFactory.Panel(body, "Bubble", new Color(0.86f, 0.97f, 0.84f, 1f));
            bubble.Size(-1, 280);
            bubble.VLayout(10, 28, TextAnchor.UpperLeft);
            sender = UIFactory.Label(bubble.transform, "", 22, TextAnchor.UpperLeft, new Color(0.1f, 0.45f, 0.3f), FontStyle.Bold);
            headline = UIFactory.Label(bubble.transform, "", 32, TextAnchor.UpperLeft, Theme.CardText);
            var fwd = UIFactory.Label(bubble.transform, "<i>Forwarded many times</i>", 18, TextAnchor.LowerRight, new Color(0.4f, 0.4f, 0.4f));

            clue = UIFactory.Label(body, "", 24, TextAnchor.MiddleCenter, Theme.Accent);
            clue.Size(-1, 70);

            var row = UIFactory.Rect(body, "Answers");
            row.HLayout(24, 0, TextAnchor.MiddleCenter);
            row.Size(-1, 96);
            UIFactory.Button(row, "1. SACH (Real)", () => Answer(false), 30, new Color(0.18f, 0.55f, 0.34f)).Size(330, 90);
            UIFactory.Button(row, "2. AFWAAH (Fake)", () => Answer(true), 30, new Color(0.8f, 0.16f, 0.16f)).Size(330, 90);
            var check = UIFactory.Button(row, "3. Source check karo", CheckSource, 26, Theme.Button);
            check.Size(330, 90);
            checkButton = check.gameObject;
            answerRow = row.gameObject;

            feedback = UIFactory.Label(body, "", 25, TextAnchor.MiddleCenter, Theme.Text);
            feedback.Size(-1, 110);
            var next = UIFactory.Button(body, "Agli khabar  <size=20>(E)</size>", Next, 30, Theme.Magenta);
            next.Size(420, 76);
            nextButton = next.gameObject;
            summary = UIFactory.Label(body, "", 28, TextAnchor.MiddleCenter, Theme.Accent, FontStyle.Bold);
            var fin = UIFactory.Button(body, "Ho gaya!  <size=20>(E)</size>", Finish, 30, Theme.Teal);
            fin.Size(420, 76);
            finishButton = fin.gameObject;
        }

        public override void OnOpen()
        {
            round = MiniGameData.Pick(MiniGameData.News, Balance.FakeNewsPerRound, gm.Rng);
            index = 0;
            correct = 0;
            finished = false;
            ShowItem();
        }

        void ShowItem()
        {
            answered = false;
            shownFrame = Time.frameCount;
            var n = round[index];
            sender.text = n.sender;
            headline.text = n.headline;
            clue.text = "";
            feedback.text = "";
            progress.text = $"Khabar {index + 1}/{round.Count}   ·   Sahi: {correct}";
            answerRow.SetActive(true);
            checkButton.SetActive(true);
            nextButton.SetActive(false);
            summary.gameObject.SetActive(false);
            finishButton.SetActive(false);
        }

        void CheckSource()
        {
            if (answered) return;
            clue.text = "Source check: " + round[index].clue;
            checkButton.SetActive(false);
        }

        void Answer(bool saysFake)
        {
            if (answered || finished || Time.frameCount == shownFrame) return;
            answered = true;
            var n = round[index];
            bool ok = saysFake == n.isFake;
            if (ok) correct++;
            string truth = n.isFake ? "AFWAAH (fake)" : "SACH (real)";
            feedback.text = (ok ? "<color=#5ce65c><b>Sahi pakda!</b></color> " : $"<color=#ff5555><b>Galat!</b></color> Yeh {truth} tha. ") + n.explanation;
            if (clue.text.Length == 0) clue.text = "Source: " + n.clue;
            progress.text = $"Khabar {index + 1}/{round.Count}   ·   Sahi: {correct}";
            answerRow.SetActive(false);
            nextButton.SetActive(true);
            shownFrame = Time.frameCount;
        }

        void Next()
        {
            if (!answered || Time.frameCount == shownFrame) return;
            index++;
            if (index < round.Count) { ShowItem(); return; }
            finished = true;
            var (civic, honesty) = MiniGameData.FakeNewsReward(correct, round.Count);
            nextButton.SetActive(false);
            feedback.text = "";
            clue.text = "";
            summary.gameObject.SetActive(true);
            summary.text = $"{correct}/{round.Count} sahi!   Civic +{civic}{(honesty > 0 ? $"   Honesty +{honesty}" : "")}\n" +
                           "<size=22><color=#fff8e7>Check karo: source kaun hai? Date kya hai? Kis ko fayda? \"Forward karo\" pressure = red flag!</color></size>";
            finishButton.SetActive(true);
            shownFrame = Time.frameCount;
        }

        void Finish()
        {
            if (!finished || Time.frameCount == shownFrame) return;
            var (civic, honesty) = MiniGameData.FakeNewsReward(correct, round.Count);
            ui.Close(this);
            gm.ApplyMiniGameResult(MiniGame.FakeNews, civic, 0, 0, honesty, "rumours_checked", 1);
            gm.UI.Notify("Chacha ab \"Fact-Check Chacha\" ban gaye hain!");
        }

        void Update()
        {
            if (ui.CurrentModal != this || ui.JustOpened || Time.frameCount == shownFrame) return;
            if (!answered && !finished)
            {
                if (GameInput.Down(GameKey.Choice1)) Answer(false);
                else if (GameInput.Down(GameKey.Choice2)) Answer(true);
                else if (GameInput.Down(GameKey.Choice3)) CheckSource();
            }
            else if (GameInput.Down(GameKey.Interact))
            {
                if (finished) Finish(); else Next();
            }
        }
    }
}
