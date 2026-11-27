using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MohallaHero
{
    /// <summary>
    /// The polling booth, step by step as in a real Indian polling station:
    /// 1) the officer checks your slip, 2) indelible ink on your left index finger (animated),
    /// 3) the EVM: press the blue button next to your choice (NOTA included), 4) the VVPAT slip confirms it.
    /// Your choice is secret and never changes your karma.
    /// </summary>
    public class VotingScreen : UIScreen
    {
        GameObject stepId, stepInk, stepEvm, stepDone;
        Image inkFill, fingerImage;
        Text inkText, doneText, vvpatText;
        Image vvpatSymbol, readyLamp;
        GameObject vvpat;
        readonly Image[] lamps = new Image[4];
        Candidate chosen;
        bool pressed;

        public override bool CloseOnEscape => false;

        protected override void Build()
        {
            var body = Window("Polling Booth No. 1 · Ward 7", new Vector2(1240, 900), false, new Color(0.11f, 0.44f, 0.72f));

            // ---- step 1: ID check
            var id = UIFactory.Rect(body, "Id").Stretch();
            id.VLayout(24, 40, TextAnchor.MiddleCenter);
            var idRow = UIFactory.Rect(id, "Row");
            idRow.HLayout(30, 0, TextAnchor.MiddleCenter);
            UIFactory.Icon(idRow, SpriteFactory.Character(NpcDatabase.Get(NpcId.Fernandes), 0, 0), 200);
            UIFactory.Label(idRow, "<b>Officer Fernandes:</b>\n\"Namaste! Voter slip aur photo ID dikhaiye. Naam list mein hai... haan, mil gaya!\"\n\n<size=22><color=#d8c8f0>Step 1 of 3: identity check</color></size>",
                30, TextAnchor.MiddleLeft).Size(700, 260);
            UIFactory.Button(id, "Voter slip dikhao", () => Show(stepInk, InkRoutine()), 32, Theme.Teal).Size(460, 86);
            stepId = id.gameObject;

            // ---- step 2: ink
            var ink = UIFactory.Rect(body, "Ink").Stretch();
            var fingerBox = UIFactory.Panel(ink, "Finger", Theme.Card);
            fingerBox.rectTransform.Place(new Vector2(0.3f, 0.5f), new Vector2(0, 0), new Vector2(360, 600));
            fingerBox.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            fingerImage = UIFactory.Icon(fingerBox.transform, SpriteFactory.Finger, 300);
            fingerImage.rectTransform.anchorMin = fingerImage.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            fingerImage.rectTransform.pivot = new Vector2(0.5f, 0f);
            fingerImage.rectTransform.sizeDelta = new Vector2(300, 400);
            fingerImage.rectTransform.anchoredPosition = new Vector2(0, 40);
            // the ink line drawn across the nail and up the finger
            inkFill = UIFactory.Panel(fingerImage.transform, "InkLine", new Color(0.32f, 0.1f, 0.55f, 0.95f), false);
            inkFill.rectTransform.anchorMin = inkFill.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            inkFill.rectTransform.pivot = new Vector2(0.5f, 0f);
            inkFill.rectTransform.sizeDelta = new Vector2(14, 130);
            inkFill.rectTransform.anchoredPosition = new Vector2(-62, 250);
            inkFill.type = Image.Type.Filled;
            inkFill.fillMethod = Image.FillMethod.Vertical;
            inkFill.fillOrigin = (int)Image.OriginVertical.Top;
            inkText = UIFactory.Label(ink, "", 30, TextAnchor.MiddleLeft);
            inkText.rectTransform.Anchor(new Vector2(0.5f, 0.15f), new Vector2(0.97f, 0.85f), Vector2.zero, Vector2.zero);
            stepInk = ink.gameObject;

            // ---- step 3: EVM
            var evm = UIFactory.Rect(body, "Evm").Stretch();
            var machine = UIFactory.Panel(evm, "Machine", new Color(0.82f, 0.83f, 0.8f, 1f));
            machine.rectTransform.Place(new Vector2(0.38f, 0.5f), Vector2.zero, new Vector2(760, 700));
            machine.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            machine.VLayout(10, 24, TextAnchor.UpperCenter);
            var top = UIFactory.Rect(machine.transform, "Top");
            top.HLayout(12, 0, TextAnchor.MiddleLeft);
            top.Size(-1, 50);
            UIFactory.Label(top, "BALLOT UNIT · Ward 7", 26, TextAnchor.MiddleLeft, new Color(0.15f, 0.15f, 0.2f), FontStyle.Bold).Size(520);
            readyLamp = UIFactory.Panel(top, "Ready", new Color(0.2f, 0.85f, 0.3f), false);
            readyLamp.sprite = SpriteFactory.Circle;
            readyLamp.Size(30, 30);
            UIFactory.Label(top, "READY", 20, TextAnchor.MiddleLeft, new Color(0.15f, 0.15f, 0.2f), FontStyle.Bold).Size(100);
            Candidate[] order = { Candidate.Jugaad, Candidate.Vaada, Candidate.Meera, Candidate.Nota };
            for (int i = 0; i < order.Length; i++)
            {
                var c = order[i];
                var row = UIFactory.Panel(machine.transform, "Row", new Color(1f, 1f, 1f, 1f));
                row.Size(-1, 128);
                row.HLayout(18, 12, TextAnchor.MiddleLeft);
                UIFactory.Label(row.transform, (i + 1).ToString(), 34, TextAnchor.MiddleCenter, new Color(0.15f, 0.15f, 0.2f), FontStyle.Bold).Size(40);
                UIFactory.Label(row.transform, EndingEvaluator.CandidateName(c), 28, TextAnchor.MiddleLeft, new Color(0.15f, 0.15f, 0.2f), FontStyle.Bold).Size(330);
                var symBg = UIFactory.Panel(row.transform, "Symbol", new Color(0.2f, 0.2f, 0.28f), false);
                symBg.Size(96, 96);
                var sym = UIFactory.Icon(symBg.transform, SpriteFactory.CandidateSymbol(c), 80);
                sym.rectTransform.anchorMin = sym.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                sym.rectTransform.sizeDelta = new Vector2(80, 80);
                lamps[i] = UIFactory.Panel(row.transform, "Lamp", new Color(0.35f, 0.1f, 0.1f), false);
                lamps[i].sprite = SpriteFactory.Circle;
                lamps[i].Size(30, 30);
                var btn = UIFactory.Button(row.transform, "", () => Press(c), 20, new Color(0.11f, 0.3f, 0.75f));
                btn.Size(110, 74);
            }
            // VVPAT
            var vv = UIFactory.Panel(evm, "VVPAT", new Color(0.25f, 0.25f, 0.3f, 1f));
            vv.rectTransform.Place(new Vector2(0.86f, 0.5f), Vector2.zero, new Vector2(300, 460));
            vv.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            UIFactory.Label(vv.transform, "VVPAT", 24, TextAnchor.UpperCenter, Theme.Text, FontStyle.Bold).rectTransform.Stretch(14);
            var slip = UIFactory.Panel(vv.transform, "Slip", new Color(1f, 1f, 0.95f, 1f), false);
            slip.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(220, 300));
            slip.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            slip.VLayout(12, 16, TextAnchor.MiddleCenter);
            vvpatSymbol = UIFactory.Icon(slip.transform, null, 110);
            vvpatText = UIFactory.Label(slip.transform, "", 22, TextAnchor.MiddleCenter, new Color(0.15f, 0.15f, 0.2f), FontStyle.Bold);
            vvpat = slip.gameObject;
            stepEvm = evm.gameObject;

            // ---- done
            var done = UIFactory.Rect(body, "Done").Stretch();
            done.VLayout(24, 40, TextAnchor.MiddleCenter);
            doneText = UIFactory.Label(done, "", 36, TextAnchor.MiddleCenter, Theme.Accent, FontStyle.Bold);
            UIFactory.Label(done, "Aapka vote secret hai: kisi ko batane ki zaroorat nahi. Ungli ki syahi dikhaiye aur garv kariye!", 26, TextAnchor.MiddleCenter);
            UIFactory.Button(done, "Booth se bahar jao", Leave, 32, Theme.Magenta).Size(480, 86);
            stepDone = done.gameObject;
        }

        public override void OnOpen()
        {
            pressed = false;
            Show(stepId, null);
        }

        void Show(GameObject step, IEnumerator routine)
        {
            stepId.SetActive(step == stepId);
            stepInk.SetActive(step == stepInk);
            stepEvm.SetActive(step == stepEvm);
            stepDone.SetActive(step == stepDone);
            if (routine != null) StartCoroutine(routine);
        }

        IEnumerator InkRoutine()
        {
            inkFill.fillAmount = 0f;
            inkText.text = "<b>Step 2 of 3:</b> Polling officer aapki baayein haath ki ungli pe <color=#c77dff><b>syahi (indelible ink)</b></color> lagata hai...";
            yield return new WaitForSecondsRealtime(0.8f);
            for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / 1.6f)
            {
                inkFill.fillAmount = Mathf.SmoothStep(0, 1, t);
                yield return null;
            }
            inkFill.fillAmount = 1f;
            inkText.text += "\n\n<color=#5ce65c><b>Syahi lag gayi!</b></color> Yeh kuch hafton tak nahi mitegi, taaki koi do baar vote na kar sake. <b>Ek insaan, ek vote.</b>";
            yield return new WaitForSecondsRealtime(2.6f);
            foreach (var l in lamps) l.color = new Color(0.35f, 0.1f, 0.1f);
            vvpat.SetActive(false);
            Show(stepEvm, null);
        }

        void Press(Candidate c)
        {
            if (pressed) return;
            pressed = true;
            chosen = c;
            StartCoroutine(VoteRoutine(c));
        }

        IEnumerator VoteRoutine(Candidate c)
        {
            int i = c == Candidate.Jugaad ? 0 : c == Candidate.Vaada ? 1 : c == Candidate.Meera ? 2 : 3;
            lamps[i].color = new Color(1f, 0.15f, 0.1f);
            readyLamp.color = new Color(0.3f, 0.3f, 0.3f);
            gm.UI.Subtitle("EVM", "BEEEEP!", 2f);
            yield return new WaitForSecondsRealtime(0.6f);
            vvpat.SetActive(true);
            vvpatSymbol.sprite = SpriteFactory.CandidateSymbol(c);
            vvpatText.text = EndingEvaluator.CandidateName(c) + "\n(" + EndingEvaluator.Symbol(c) + ")";
            yield return new WaitForSecondsRealtime(2.4f);
            vvpat.SetActive(false);
            doneText.text = "Vote ho gaya! Mera vote, meri awaaz!";
            Show(stepDone, null);
        }

        void Leave()
        {
            ui.Close(this);
            gm.Election.CastVote(chosen);
        }
    }

    /// <summary>Election result and ending: winner, ending, Mohalla Rating, karma ranks, choices and lessons.</summary>
    public class EndingScreen : UIScreen
    {
        RectTransform content;

        /// <summary>"Saalon baad": the player at their age today and later in life, shaped by the ending.</summary>
        void Epilogue(RectTransform parent)
        {
            var e = gm.Election.Ending;
            bool good = e == Ending.MohallaHero || e == Ending.JagrukNagrik;
            string n = gm.PlayerName;
            int now = gm.Age.Years;
            UIFactory.Label(parent, $"<b>Saalon baad... (Epilogue)</b>  <size=22><color=#d8c8f0>{AgeSystem.Format(gm.Age.Date)} · aaj {now} saal</color></size>", 28, TextAnchor.UpperLeft, Theme.Accent);
            var row = UIFactory.Rect(parent, "Life");
            row.HLayout(12, 0, TextAnchor.UpperCenter);
            (int age, string text)[] life =
            {
                (now, good ? "Pehla vote diya, soch samajh kar. Mohalla ne tumhe naam se jaana." : "Pehla vote diya. Kuch sawaal reh gaye, par seekhna shuru hua."),
                (30, good ? "Mohalla committee mein: har mahine safai abhiyan aur RTI workshop." : "Naukri aur ghar... par mohalla ki baatein ab bhi yaad aati hain."),
                (45, good ? "Apne bachchon ko Gulmohar Park ke woh ped dikhate ho jo tumne lagaye the." : "Ab bachchon ko sikhate ho: 'Sach check karo, phir share karo.'"),
                (65, good ? $"Buzurg {n}: bachche kehte hain, \"Yahi the asli Mohalla Hero!\"" : $"Buzurg {n}, chai pe: \"Har vote count karta hai, beta.\""),
            };
            foreach (var (age, text) in life)
            {
                var card = UIFactory.Panel(row, "Age", new Color(1, 1, 1, 0.07f));
                card.VLayout(6, 12, TextAnchor.UpperCenter);
                card.Size(285, 300);
                UIFactory.Icon(card.transform, SpriteFactory.PlayerAged(gm.PlayerGender, age, 0, 0), 110);
                UIFactory.Label(card.transform, $"<b>{age} saal · {AgeSystem.StageName(AgeSystem.StageFor(age))}</b>", 22, TextAnchor.MiddleCenter, Theme.Accent);
                UIFactory.Label(card.transform, text, 19, TextAnchor.UpperCenter, Theme.Text);
            }
        }

        protected override void Build()
        {
            var body = Window("Chunav ka Nateeja (Result)", new Vector2(1280, 960), true, new Color(0.55f, 0.2f, 0.75f));
            content = UIFactory.ScrollList(body, "List", 12);
            ((RectTransform)content.parent.parent).Anchor(Vector2.zero, Vector2.one, new Vector2(0, 96), Vector2.zero);
            var buttons = UIFactory.Rect(body, "Buttons");
            buttons.Anchor(Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 84));
            buttons.HLayout(20, 0, TextAnchor.MiddleCenter);
            UIFactory.Button(buttons, "Mohalla ghoomo", () => ui.Close(this), 28, Theme.Teal).Size(330, 76);
            UIFactory.Button(buttons, "Main Menu", () => { ui.Close(this); gm.QuitToMenu(); }, 28, Theme.Button).Size(300, 76);
        }

        public override void OnOpen()
        {
            UIFactory.Clear(content);
            var e = gm.Election;
            var winner = UIFactory.Panel(content, "Winner", new Color(1f, 0.76f, 0.05f, 0.2f));
            winner.HLayout(24, 20, TextAnchor.MiddleLeft);
            var symBg = UIFactory.Panel(winner.transform, "Sym", Theme.PanelLight);
            symBg.Size(130, 130);
            var sym = UIFactory.Icon(symBg.transform, SpriteFactory.CandidateSymbol(e.Winner), 100);
            sym.rectTransform.anchorMin = sym.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            sym.rectTransform.sizeDelta = new Vector2(100, 100);
            UIFactory.Label(winner.transform, $"<size=24><color=#d8c8f0>WARD 7 KA NAYA PARSHAD (COUNCILLOR)</color></size>\n<b>{EndingEvaluator.CandidateName(e.Winner)}</b>", 40, TextAnchor.MiddleLeft, Theme.Accent).Size(1000);

            UIFactory.Label(content, $"<b>Ending: {EndingEvaluator.Title(e.Ending)}</b>", 44, TextAnchor.MiddleCenter, Theme.Accent);
            UIFactory.Label(content, EndingEvaluator.Description(e.Ending), 27, TextAnchor.UpperLeft, Theme.Text);

            var rating = UIFactory.Panel(content, "Rating", new Color(1, 1, 1, 0.07f));
            rating.HLayout(16, 16, TextAnchor.MiddleLeft);
            UIFactory.Label(rating.transform, "<b>MOHALLA RATING</b>", 26, TextAnchor.MiddleLeft, Theme.TextDim).Size(260);
            UIFactory.Stars(rating.transform, EndingEvaluator.Stars(e.FinalRating), 46);
            UIFactory.Label(rating.transform, $"<b>{e.FinalRating}/100</b>", 34, TextAnchor.MiddleLeft, Theme.Accent).Size(200);

            var karma = UIFactory.Rect(content, "Karma");
            karma.HLayout(12, 0, TextAnchor.MiddleCenter);
            foreach (var s in KarmaSystem.All)
            {
                var a = gm.Karma.Get(s);
                var c = UIFactory.Panel(karma, "Stat", a.color * 0.8f + new Color(0, 0, 0, 0.2f));
                c.VLayout(4, 12, TextAnchor.MiddleCenter);
                c.Size(285, 130);
                UIFactory.Icon(c.transform, SpriteFactory.StatIcon(s), 40);
                UIFactory.Label(c.transform, $"<b>{a.displayName}: {a.xp}</b>\n<size=19>{a.Rank}</size>", 22, TextAnchor.MiddleCenter);
            }

            var st = gm.Story;
            string bribe = st.Is("bribe_taken") || st.Is("took_bribe_netaji") ? "<color=#ff5555>You took a bribe.</color>" : st.Is("bribe_reported") ? "<color=#5ce65c>You reported vote-buying.</color>" : "<color=#5ce65c>You refused every bribe.</color>";
            UIFactory.Label(content,
                $"Neighbours who marched with you: <b>{gm.Trust.JoinedCount}</b>   ·   Voters you informed: <b>{st.Get("voters_informed")}</b>   ·   " +
                $"Violence: <b>{st.Get("violence")}</b>\n{bribe}   ·   Lights off: {st.Get("lights_off")}, taps closed: {st.Get("taps_closed")}, trees planted: {st.Get("saplings")}, garbage heaps cleared: {st.Get("piles_cleaned")}\n" +
                "<color=#d8c8f0>Your vote? Secret. That's the point: nobody, not even this game, judges whom you voted for.</color>",
                23, TextAnchor.UpperLeft, Theme.Text);

            Epilogue(content);

            UIFactory.Label(content, "<b>Aapne kya seekha (What this story teaches)</b>", 28, TextAnchor.UpperLeft, Theme.Accent);
            foreach (var m in MissionDatabase.All)
                UIFactory.Label(content, $"<b>{m.title}:</b> {m.lesson}", 22, TextAnchor.UpperLeft, Theme.TextDim);
            UIFactory.Label(content, "Doosra ending dekhna hai? Naya game shuru karo aur alag choices try karo!", 24, TextAnchor.MiddleCenter, Theme.Accent);
            ((ScrollRect)content.parent.parent.GetComponent<ScrollRect>()).verticalNormalizedPosition = 1f;
        }
    }
}
