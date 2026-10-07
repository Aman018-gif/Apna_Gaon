using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace MohallaHero
{
    /// <summary>One karma/trust effect of a choice, for the "Why it matters" card.</summary>
    public struct ChoiceEffect
    {
        public string label;
        public int delta;
        public Color color;
        public Sprite icon;
    }

    /// <summary>
    /// Runs the Ink story (branching dialogue with 2–4 choices) and is its host: story variables, EXTERNAL functions,
    /// choice tags (#civic:+5, #trust:+15, #why:..., #violence), speakers' portraits and voices.
    /// After every choice that changes karma or trust, the "Why it matters" card explains the consequence.
    /// </summary>
    public class DialogueManager : IStoryHost
    {
        GameManager gm => GameManager.I;
        InkRunner runner;
        NPC npc;
        StoryObject obj;
        StoryStep pending;
        readonly Queue<Action> afterEnd = new Queue<Action>();
        string lastSpeaker, lastText;
        Sprite lastFace;
        bool lastIsPlayer;
        bool choseAny;

        public bool Active { get; private set; }
        public NPC CurrentNpc => npc;

        // ------------------------------------------------------------------ entry points

        public void TalkTo(NPC target)
        {
            if (Active) return;
            npc = target;
            obj = null;
            npc.BeginTalk(gm.Player.transform.position);
            Begin("npc_" + target.Def.Key);
        }

        public void RunObject(StoryObject o)
        {
            if (Active) return;
            npc = null;
            obj = o;
            Begin(o.Knot);
        }

        /// <summary>Runs a scripted scene (intro, march incident...).</summary>
        public void RunKnot(string knot)
        {
            if (Active) return;
            npc = null;
            obj = null;
            Begin(knot);
        }

        void Begin(string knot)
        {
            var story = StoryLibrary.Story;
            if (!story.HasKnot(knot)) { Debug.LogWarning("Mohalla Hero: no knot " + knot); EndConversation(); return; }
            Active = true;
            lastSpeaker = lastText = null;
            lastFace = null;
            runner = new InkRunner(story, this);
            runner.Start(knot);
            pending = null;
            choseAny = false;
            Advance();
        }

        // ------------------------------------------------------------------ flow

        StoryStep NextStep()
        {
            if (pending != null) { var p = pending; pending = null; return p; }
            try { return runner.Next(); }
            catch (Exception e)
            {
                Debug.LogError("Mohalla Hero story error: " + e.Message);
                return StoryStep.EndStep;
            }
        }

        void Advance()
        {
            var step = NextStep();
            if (step.Kind == StepKind.End) { Finish(); return; }

            if (step.Kind == StepKind.Line)
            {
                ResolveSpeaker(step.Speaker, out lastSpeaker, out lastFace, out lastIsPlayer, out var voice);
                lastText = step.Text;
                gm.Voice.Say(voice, step.Text);
                // Look ahead: if choices follow, show them with this line; otherwise a Continue button.
                var next = NextStep();
                if (next.Kind == StepKind.Choices) { ShowChoices(next); return; }
                pending = next;
                bool last = next.Kind == StepKind.End;
                gm.UI.Dialogue.Show(lastSpeaker, lastFace, lastIsPlayer, lastText,
                    new List<(string, Action)> { (last ? "Theek hai" : "Aage", Advance) });
                return;
            }
            ShowChoices(step);
        }

        void ShowChoices(StoryStep step)
        {
            var opts = new List<(string, Action)>();
            foreach (var c in step.Choices)
            {
                var choice = c;
                opts.Add((ChoiceLabel(c), () => Choose(choice)));
            }
            gm.UI.Dialogue.Show(lastSpeaker ?? "", lastFace, lastIsPlayer, lastText ?? "", opts);
        }

        /// <summary>Choice text plus small hints of what it affects (not the exact numbers, so choices stay honest).</summary>
        static string ChoiceLabel(ShownChoice c) => c.Text;

        void Choose(ShownChoice c)
        {
            choseAny = true;
            var r = ChoiceTags.Parse(c.Tags, npc != null ? npc.Def.id : (NpcId?)null);
            ChoiceTags.Apply(r, gm.Karma, gm.Trust, gm.Story);

            var effects = new List<ChoiceEffect>();
            foreach (var (stat, delta) in r.Karma)
                effects.Add(new ChoiceEffect { label = Format.Stat(stat), delta = delta, color = Theme.StatColor(stat), icon = SpriteFactory.StatIcon(stat) });
            foreach (var (id, delta) in r.Trust)
                effects.Add(new ChoiceEffect { label = $"{NpcDatabase.FirstName(id)}'s trust", delta = delta, color = Theme.Trust, icon = SpriteFactory.AppIcon("trust") });
            if (r.Violence)
            {
                gm.CameraRig.Shake(0.6f);
                effects.Add(new ChoiceEffect { label = "Violence: Mohalla Rating", delta = -Balance.ViolencePenalty, color = Theme.Bad, icon = SpriteFactory.StarIcon(false) });
            }

            try { runner.Choose(c.Index); }
            catch (Exception e) { Debug.LogError("Mohalla Hero story error: " + e.Message); Finish(); return; }

            GameEvents.RaiseRatingChanged();
            if (effects.Count > 0 || !string.IsNullOrEmpty(r.Why))
            {
                gm.Voice.Stop();
                gm.UI.Why.Show(c.Text, effects, r.Why, r.Violence, Advance);
            }
            else Advance();
        }

        void Finish()
        {
            Active = false;
            gm.Voice.Stop();
            EndConversation();
            gm.UI.Close(gm.UI.Dialogue);
            while (afterEnd.Count > 0)
            {
                var a = afterEnd.Dequeue();
                try { a(); } catch (Exception e) { Debug.LogException(e); }
            }
            gm.CheckProgress();
        }

        void EndConversation()
        {
            if (npc != null) npc.EndTalk();
            npc = null;
            obj = null;
        }

        /// <summary>
        /// Escape on a dialogue: only real conversations (not scripted scenes or objects) can be left early, and only
        /// before the first choice, so a decision is never cut off half-way through its consequences.
        /// </summary>
        public bool CanCancel => Active && npc != null && !choseAny;

        public void Cancel()
        {
            if (!Active) return;
            afterEnd.Clear();
            Finish();
        }

        // ------------------------------------------------------------------ speakers

        void ResolveSpeaker(string speaker, out string name, out Sprite face, out bool isPlayer, out VoiceProfile voice)
        {
            isPlayer = false;
            if (string.IsNullOrEmpty(speaker))
            {
                name = "";
                face = null;
                voice = VoiceSystem.Narrator;
                return;
            }
            if (speaker == "You")
            {
                isPlayer = true;
                name = gm.PlayerName;
                face = SpriteFactory.Player(gm.PlayerGender, 0, 0);
                voice = VoiceSystem.ForPlayer(gm.PlayerGender);
                return;
            }
            if (NpcDatabase.TryFind(speaker, out var def))
            {
                name = $"{def.name}  <size=22><color=#ffe9a8>{def.role}</color></size>";
                face = SpriteFactory.Character(def, 0, 0);
                voice = VoiceSystem.ForNpc(def);
                return;
            }
            name = speaker;
            face = SpriteFactory.Silhouette;
            bool female = speaker.Contains("Aunty") || speaker.Contains("Dadi") || speaker.Contains("Didi");
            voice = female ? VoiceSystem.StrangerFemale : VoiceSystem.Stranger;
        }

        // ------------------------------------------------------------------ IStoryHost

        public int GetVar(string name)
        {
            if (name.StartsWith("trust_") && NpcDatabase.TryFind(name.Substring(6), out var d)) return gm.Trust.Get(d.id);
            return gm.Story.Get(name);
        }

        public void SetVar(string name, int value)
        {
            if (name.StartsWith("trust_") && NpcDatabase.TryFind(name.Substring(6), out var d)) { gm.Trust.Add(d.id, value - gm.Trust.Get(d.id)); return; }
            gm.Story.Set(name, value);
        }

        public string TextOf(string identifier)
        {
            switch (identifier)
            {
                case "player_name": return gm.PlayerName;
                case "hint": return gm.Missions.Hint();
            }
            return null;
        }

        public int Call(string function, string[] args)
        {
            string a0 = args.Length > 0 ? args[0] : "";
            int n0 = args.Length > 0 && int.TryParse(args[0], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var v0) ? v0 : 0;
            int n1 = args.Length > 1 && int.TryParse(args[1], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var v1) ? v1 : 0;
            switch (function)
            {
                case "has": return gm.HasItem(a0) ? 1 : 0;
                case "give": gm.GiveItem(a0); return 1;
                case "take": gm.Story.Set(ItemDatabase.Var(a0), 0); return 1;
                case "trust": return NpcDatabase.TryFind(a0, out var d) ? gm.Trust.Get(d.id) : 0;
                case "add_trust": if (NpcDatabase.TryFind(a0, out var d2)) gm.Trust.Add(d2.id, n1); return 1;
                case "joined": return gm.Trust.JoinedCount;
                case "karma": return KarmaSystem.TryParse(a0, out var s) ? gm.Karma.Xp(s) : 0;
                case "money": return gm.Money;
                case "pay": return gm.TrySpend(n0) ? 1 : 0;
                case "earn": gm.AddMoney(n0); return 1;
                case "day": return gm.Clock.Day;
                case "hour": return Mathf.FloorToInt(gm.Clock.HourFloat);
                case "chapter": return gm.Missions.CurrentChapter;
                case "rating": return gm.Rating;
                case "resolve": if (obj != null) obj.Resolve(); return 1;
                case "minigame":
                    afterEnd.Enqueue(() => gm.UI.OpenMiniGame(a0));
                    return 1;
                case "unlock":
                    if (a0 == "bazaar") gm.Areas.Unlock(AreaId.Bazaar);
                    else if (a0 == "maidan") gm.Areas.Unlock(AreaId.Maidan);
                    return 1;
                case "protest":
                    if (a0 == "gather") gm.Protest.Gather();
                    else if (a0 == "march") afterEnd.Enqueue(gm.Protest.StartMarch);
                    return 1;
                case "vote": afterEnd.Enqueue(gm.Election.OpenBooth); return 1;
                case "sleep": afterEnd.Enqueue(gm.Sleep); return 1;
                case "save": gm.SaveGame(); gm.UI.Notify("Game saved!"); return 1;
            }
            Debug.LogWarning($"Mohalla Hero: unknown story function {function}()");
            return 0;
        }
    }
}
