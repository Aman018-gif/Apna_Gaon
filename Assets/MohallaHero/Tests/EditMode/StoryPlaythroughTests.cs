using System;
using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;

namespace MohallaHero.Tests
{
    /// <summary>
    /// Plays the whole story (all .ink files in Resources/Stories) with the real karma, trust, mission and ending logic,
    /// simulating the world actions the game performs (objects, mini-games, the march, the next day, the vote).
    /// </summary>
    public class StoryPlaythroughTests
    {
        /// <summary>A headless copy of the game's story host (mirrors DialogueManager.Call).</summary>
        class Sim : IStoryHost
        {
            public readonly InkStory Story;
            public readonly StoryState S = new StoryState();
            public readonly KarmaSystem K;
            public readonly TrustSystem T;
            public readonly MissionSystem M;
            public int Day = 1, Money = Balance.StartingMoney;
            public string CurrentObj;
            public readonly List<string> MiniGames = new List<string>();
            public readonly List<string> Protest = new List<string>();
            public bool Voted;
            public Func<List<ShownChoice>, int> Chooser;
            public int Lines;

            public Sim(InkStory story)
            {
                Story = story;
                S.SetDefaults(story.VarDefaults);
                T = new TrustSystem(S);
                K = new KarmaSystem();
                M = new MissionSystem(n => n == "joined" ? T.JoinedCount : S.Get(n));
            }

            public int GetVar(string name)
            {
                if (name.StartsWith("trust_") && NpcDatabase.TryFind(name.Substring(6), out var d)) return T.Get(d.id);
                return S.Get(name);
            }

            public void SetVar(string name, int value) => S.Set(name, value);

            public string TextOf(string id) => id == "player_name" ? "Tester" : id == "hint" ? M.Hint() : null;

            public int Call(string f, string[] args)
            {
                string a0 = args.Length > 0 ? args[0] : "";
                int n0 = args.Length > 0 && int.TryParse(a0, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var v0) ? v0 : 0;
                int n1 = args.Length > 1 && int.TryParse(args[1], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var v1) ? v1 : 0;
                switch (f)
                {
                    case "has": return S.Get(ItemDatabase.Var(a0)) > 0 ? 1 : 0;
                    case "give": Assert.IsNotNull(ItemDatabase.Get(a0), "unknown item " + a0); S.Set(ItemDatabase.Var(a0), 1); return 1;
                    case "take": S.Set(ItemDatabase.Var(a0), 0); return 1;
                    case "trust": Assert.IsTrue(NpcDatabase.TryFind(a0, out var d), "unknown person " + a0); return T.Get(d.id);
                    case "add_trust": Assert.IsTrue(NpcDatabase.TryFind(a0, out var d2), "unknown person " + a0); T.Add(d2.id, n1); return 1;
                    case "joined": return T.JoinedCount;
                    case "karma": return KarmaSystem.TryParse(a0, out var s) ? K.Xp(s) : 0;
                    case "money": return Money;
                    case "pay": if (Money < n0) return 0; Money -= n0; return 1;
                    case "earn": Money += n0; return 1;
                    case "day": return Day;
                    case "hour": return 12;
                    case "chapter": return M.CurrentChapter;
                    case "rating": return Rating;
                    case "resolve": Assert.IsNotNull(CurrentObj, "resolve() outside an object"); S.Set("obj_" + CurrentObj, 1); return 1;
                    case "minigame": Assert.Contains(a0, new[] { "waste", "fakenews", "slogan" }); MiniGames.Add(a0); return 1;
                    case "unlock": Assert.Contains(a0, new[] { "bazaar", "maidan" }); S.Set("unlocked_" + a0, 1); return 1;
                    case "protest": Protest.Add(a0); return 1;
                    case "vote": Voted = true; return 1;
                    case "sleep": case "save": return 1;
                }
                Assert.Fail("Story calls an unknown function: " + f);
                return 0;
            }

            public int Rating => EndingEvaluator.Rating(RatingInput.From(K, T.JoinedCount, S.Get("voters_informed"), S.Get("violence")));

            public void Run(string knot, NpcId? speaker = null, string obj = null)
            {
                Assert.IsTrue(Story.HasKnot(knot), "missing knot " + knot);
                CurrentObj = obj;
                var r = new InkRunner(Story, this, 7);
                r.Start(knot);
                for (int guard = 0; guard < 500; guard++)
                {
                    var step = r.Next();
                    if (step.Kind == StepKind.End) { CurrentObj = null; return; }
                    if (step.Kind == StepKind.Line) { Lines++; Assert.IsFalse(step.Text.Contains("{"), "uninterpolated text: " + step.Text); continue; }
                    Assert.LessOrEqual(step.Choices.Count, 4, "the spec allows 2-4 choices; too many in " + knot);
                    int i = Chooser(step.Choices);
                    var tags = ChoiceTags.Parse(step.Choices[i].Tags, speaker);
                    ChoiceTags.Apply(tags, K, T, S);
                    r.Choose(i);
                }
                Assert.Fail("conversation did not end: " + knot);
            }

            public void Talk(NpcId id) => Run("npc_" + id.ToString().ToLowerInvariant(), id);
            public void Use(string objId, string knot) { if (!S.Is("obj_" + objId)) Run(knot, null, objId); }

            /// <summary>Repeats an action until a condition holds (a player retrying), failing on a soft-lock.</summary>
            public void Until(Func<bool> done, Action act, string what, int max = 25)
            {
                for (int i = 0; i < max && !done(); i++) act();
                Assert.IsTrue(done(), "stuck: " + what);
            }
        }

        static int Best(List<ShownChoice> cs)
        {
            int best = 0;
            float bestScore = float.MinValue;
            for (int i = 0; i < cs.Count; i++)
            {
                float s = ChoiceTags.Parse(cs[i].Tags, NpcId.Raju).Score;
                if (s > bestScore) { bestScore = s; best = i; }
            }
            return best;
        }

        static int Worst(List<ShownChoice> cs)
        {
            int worst = 0;
            float worstScore = float.MaxValue;
            for (int i = 0; i < cs.Count; i++)
            {
                float s = ChoiceTags.Parse(cs[i].Tags, NpcId.Raju).Score;
                if (s < worstScore) { worstScore = s; worst = i; }
            }
            return worst;
        }

        static Sim NewSim() => new Sim(LoadStory());

        static InkStory LoadStory()
        {
            StoryLibrary.ClearCache();
            return StoryLibrary.Story;
        }

        /// <summary>Plays the full story. <paramref name="learn"/>: the chooser to use on retries.</summary>
        static void PlayThrough(Sim sim, Func<List<ShownChoice>, int> first, Func<List<ShownChoice>, int> retry, bool perfectMiniGames)
        {
            int attempt = 0;
            sim.Chooser = cs => attempt++ == 0 ? first(cs) : retry(cs);
            void Fresh() => attempt = 0;
            var s = sim.S;

            Fresh(); sim.Run("intro");
            Assert.IsTrue(s.Is("intro_done"));

            // ---- Chapter 1: Bijli Bachao
            Assert.AreEqual(1, sim.M.CurrentChapter);
            Fresh(); sim.Until(() => s.Is("m1_started"), () => sim.Talk(NpcId.Asha), "start m1");
            Fresh();
            for (int i = 0; i < WorldLayout.Lights.Length && s.Get("lights_off") < 4; i++) sim.Until(() => s.Is("obj_light_" + i), () => sim.Use("light_" + i, "obj_light"), "light " + i);
            for (int i = 0; i < WorldLayout.Taps.Length && s.Get("taps_closed") < 3; i++) sim.Until(() => s.Is("obj_tap_" + i), () => sim.Use("tap_" + i, "obj_tap"), "tap " + i);
            for (int i = 0; i < WorldLayout.Saplings.Length; i++) sim.Until(() => s.Is("obj_sapling_" + i), () => sim.Use("sapling_" + i, "obj_sapling"), "sapling " + i);
            for (int i = 0; i < WorldLayout.Garbage.Length && s.Get("piles_cleaned") < 2; i++)
            {
                sim.Until(() => s.Is("obj_garbage_" + i), () => sim.Use("garbage_" + i, "obj_garbage"), "garbage " + i);
                var (green, civic) = MiniGameData.WasteReward(perfectMiniGames ? 8 : 3, 8);
                sim.K.Add(KarmaStat.Green, green); sim.K.Add(KarmaStat.Civic, civic);
            }
            Assert.Contains("waste", sim.MiniGames);
            Fresh(); sim.Until(() => s.Is("m1_done"), () => sim.Talk(NpcId.Asha), "finish m1");
            Assert.AreEqual(2, sim.M.CurrentChapter);

            // ---- Chapter 2: Bully Buster
            Fresh(); sim.Until(() => s.Is("m2_started"), () => sim.Talk(NpcId.Pintu), "start m2");
            Fresh(); sim.Until(() => s.Is("m2_stopped"), () => sim.Talk(NpcId.Golu), "stop the bullying");
            Fresh(); sim.Until(() => s.Is("m2_reported"), () => sim.Talk(NpcId.Asha), "report");
            Fresh(); sim.Until(() => s.Is("m2_supported"), () => sim.Talk(NpcId.Golu), "support Golu");
            Fresh(); sim.Until(() => s.Is("m2_done"), () => sim.Talk(NpcId.Bunty), "talk to Bunty");
            Assert.AreEqual(3, sim.M.CurrentChapter);

            // ---- Chapter 3: Imandar Bazaar
            Fresh(); sim.Until(() => s.Is("m3_started"), () => sim.Talk(NpcId.Sharma), "start m3");
            Assert.IsTrue(s.Is("unlocked_bazaar"));
            Fresh(); sim.Until(() => s.Is("m3_bought"), () => sim.Talk(NpcId.Lala), "buy groceries");
            Assert.IsTrue(s.Is("m3_returned") ^ s.Is("m3_kept"));
            Fresh(); sim.Until(() => s.Is("m3_lucky"), () => sim.Use("lucky", "lucky_stall"), "lucky stall");
            Fresh(); sim.Until(() => s.Is("m3_ration"), () => sim.Use("ration", "ration_queue"), "ration queue");
            Fresh(); sim.Until(() => s.Is("m3_done"), () => sim.Talk(NpcId.Sharma), "deliver");
            Assert.AreEqual(0, s.Get("item_groceries"));
            Assert.AreEqual(4, sim.M.CurrentChapter);

            // ---- Chapter 4: Sach ki Awaaz
            Fresh(); sim.Until(() => s.Is("m4_started"), () => sim.Talk(NpcId.Meera), "start m4");
            Fresh(); sim.Until(() => s.Is("item_photo"), () => sim.Use("park_board", "park_board"), "photo");
            Fresh(); sim.Talk(NpcId.Lala);
            if (!s.Is("item_receipt")) { Fresh(); sim.Until(() => s.Is("item_receipt"), () => sim.Use("raddi", "raddi_pile"), "receipt"); }
            Fresh(); sim.Until(() => s.Is("rti_filed"), () => sim.Talk(NpcId.Fernandes), "file RTI");
            Fresh(); sim.Talk(NpcId.Fernandes);
            Assert.IsFalse(s.Is("item_rti"), "the RTI reply must take a day");
            sim.Day++;
            Fresh(); sim.Until(() => s.Is("item_rti"), () => sim.Talk(NpcId.Fernandes), "RTI reply");
            Fresh(); sim.Talk(NpcId.Jugaad);
            Assert.IsTrue(s.Is("jugaad_warned"));
            sim.Until(() => sim.T.JoinedCount >= Balance.SupportersNeeded, () =>
            {
                foreach (var id in TrustSystem.Convincible) if (!sim.T.HasJoined(id)) { Fresh(); sim.Talk(id); }
            }, "convince neighbours", 40);
            if (retry == Best)   // a thorough player keeps going until everyone is on board
                sim.Until(() => sim.T.JoinedCount == TrustSystem.Convincible.Length, () =>
                {
                    foreach (var id in TrustSystem.Convincible) if (!sim.T.HasJoined(id)) { Fresh(); sim.Talk(id); }
                }, "convince everyone", 40);
            Fresh(); sim.Until(() => s.Is("m4_done"), () => sim.Talk(NpcId.Meera), "finish m4");
            Assert.IsTrue(s.Is("m5_started"));
            Assert.AreEqual(5, sim.M.CurrentChapter);

            // ---- Chapter 5: Shanti March
            Fresh(); sim.Use("placards", "placard_table");
            Assert.Contains("slogan", sim.MiniGames);
            for (int i = 0; i < Balance.PlacardsNeeded; i++)
            {
                var r = MiniGameData.EvaluateSlogan(MiniGameData.SloganStarts[i], MiniGameData.SloganEnds[i]);
                Assert.IsTrue(r.accepted);
                sim.K.Add(KarmaStat.Civic, r.civic); sim.K.Add(KarmaStat.Courage, r.courage);
                s.Add("placards", 1);
            }
            Fresh(); sim.Until(() => s.Is("m5_gathered"), () => sim.Talk(NpcId.Meera), "gather");
            CollectionAssert.AreEqual(new[] { "gather", "march" }, sim.Protest);
            Assert.IsTrue(s.Is("item_petition"));
            s.Set("march_checkpoints", 2);
            Fresh(); sim.Run("march_incident");
            s.Set("march_checkpoints", WorldLayout.MarchRoute.Length);
            Fresh(); sim.Until(() => s.Is("m5_done"), () => sim.Talk(NpcId.Fernandes), "petition");
            Assert.IsTrue(s.Is("unlocked_maidan"));
            Assert.AreEqual(6, sim.M.CurrentChapter);

            // ---- Chapter 6: Election Mela
            Fresh(); sim.Until(() => s.Is("m6_started"), () => sim.Talk(NpcId.Fernandes), "start m6");
            Assert.IsTrue(s.Is("item_voter_slip"));
            foreach (var id in new[] { NpcId.Jugaad, NpcId.Vaada, NpcId.Meera }) { Fresh(); sim.Talk(id); }
            Assert.AreEqual(3, s.Get("stalls_visited"));
            Fresh(); sim.Talk(NpcId.Jugaad);   // second visit: the bribe or more questions
            Fresh(); sim.Until(() => s.Is("mela_play"), () => sim.Use("stage", "nukkad_stage"), "street play");
            Fresh(); sim.Talk(NpcId.Chacha);
            Assert.Contains("fakenews", sim.MiniGames);
            var (fc, fh) = MiniGameData.FakeNewsReward(perfectMiniGames ? 6 : 2, 6);
            sim.K.Add(KarmaStat.Civic, fc); sim.K.Add(KarmaStat.Honesty, fh);
            s.Set("rumours_checked", 1);
            for (int i = 0; i < Balance.VoterDoors; i++) { Fresh(); sim.Use("door_" + i, "voter_door"); }
            Fresh(); sim.Until(() => sim.Voted, () => sim.Use("booth", "booth"), "vote");
            s.Set("m6_done", 1);   // set by ElectionManager.CastVote
            Assert.AreEqual(7, sim.M.CurrentChapter);
            Assert.IsTrue(sim.M.AllDone);
        }

        [Test]
        public void StoryParses_AllDivertsResolve_AllEntryKnotsExist()
        {
            var story = LoadStory();
            CollectionAssert.IsEmpty(story.Validate());
            foreach (var d in NpcDatabase.All) Assert.IsTrue(story.HasKnot("npc_" + d.Key), "no router for " + d.id);
            foreach (var k in new[] { "intro", "obj_light", "obj_tap", "obj_sapling", "obj_garbage", "obj_home", "obj_notice", "park_board", "raddi_pile",
                                      "lucky_stall", "ration_queue", "placard_table", "voter_door", "nukkad_stage", "booth", "march_incident" })
                Assert.IsTrue(story.HasKnot(k), "missing knot " + k);
            foreach (var m in MissionDatabase.All)
            {
                Assert.IsTrue(story.VarDefaults.ContainsKey(m.startVar), "undeclared " + m.startVar);
                Assert.IsTrue(story.VarDefaults.ContainsKey(m.doneVar), "undeclared " + m.doneVar);
                foreach (var st in m.steps)
                    if (st.var != "joined" && !st.var.StartsWith("item_")) Assert.IsTrue(story.VarDefaults.ContainsKey(st.var), "undeclared " + st.var);
            }
        }

        [Test]
        public void EveryKarmaChoiceExplainsWhy()
        {
            var story = LoadStory();
            int karmaChoices = 0;
            foreach (var kv in story.Knots)
                foreach (var node in kv.Value)
                    if (node is ChoiceGroupNode g)
                        foreach (var c in g.Choices)
                        {
                            var r = ChoiceTags.Parse(c.Tags, NpcId.Raju);
                            Assert.LessOrEqual(c.Text.Length, 110, $"choice too long for mobile in {kv.Key}: {c.Text}");
                            int total = 0;
                            foreach (var k in r.Karma) total += Math.Abs(k.delta);
                            if (total >= 5)
                            {
                                karmaChoices++;
                                Assert.IsFalse(string.IsNullOrEmpty(r.Why), $"choice '{c.Text}' in {kv.Key} changes karma by {total} but has no #why");
                            }
                        }
            Assert.Greater(karmaChoices, 50);
        }

        [Test]
        public void BestPath_ReachesTheMohallaHeroEnding()
        {
            var sim = NewSim();
            PlayThrough(sim, Best, Best, true);
            Assert.AreEqual(0, sim.S.Get("violence"));
            Assert.AreEqual(TrustSystem.Convincible.Length, sim.T.JoinedCount);
            Assert.GreaterOrEqual(sim.S.Get("voters_informed"), Balance.VotersToInform);
            Assert.IsTrue(sim.S.Is("bribe_reported") || sim.S.Is("bribe_refused"));
            Assert.IsFalse(sim.S.Is("took_bribe_netaji"));
            int rating = sim.Rating;
            Assert.GreaterOrEqual(rating, 75, "a near-perfect run should rate 75+, got " + rating);
            Assert.AreEqual(Ending.MohallaHero, EndingEvaluator.Evaluate(rating, sim.S.Get("voters_informed")));
            foreach (var st in KarmaSystem.All)
                Assert.LessOrEqual(sim.K.Xp(st), Balance.RatingCap(st) * 1.6f, $"{st} far exceeds its rating cap; rebalance Balance.RatingCap");
            foreach (var st in KarmaSystem.All)
                Assert.GreaterOrEqual(sim.K.Xp(st), Balance.RatingCap(st) * 0.75f, $"{st}: a near-perfect run should come close to the cap ({sim.K.Xp(st)})");
        }

        [Test]
        public void BadChoicesFirst_StoryStillFinishes_WithAWorseEnding()
        {
            var sim = NewSim();
            PlayThrough(sim, Worst, Best, false);
            var best = NewSim();
            PlayThrough(best, Best, Best, true);
            Assert.Greater(best.Rating, sim.Rating + 20, "bad choices must visibly hurt the Mohalla Rating");
            Assert.AreNotEqual(Ending.MohallaHero, EndingEvaluator.Evaluate(sim.Rating, sim.S.Get("voters_informed")));
            Assert.IsTrue(sim.S.Is("m2_fought") || sim.S.Is("photo_fake") || sim.S.Is("took_bribe_netaji") || sim.S.Is("bribe_taken"), "the worst path should take some bad branches");
        }

        [Test]
        public void ViolenceInTheMarch_CostsTwentyFiveRatingPoints()
        {
            var sim = NewSim();
            int before = sim.Rating;
            foreach (var st in KarmaSystem.All) sim.K.Add(st, 500);
            int high = sim.Rating;
            Assert.GreaterOrEqual(high, 70);
            sim.Chooser = Worst;
            sim.Run("march_incident");
            Assert.AreEqual(1, sim.S.Get("violence"));
            Assert.LessOrEqual(sim.Rating, high - Balance.ViolencePenalty);
            Assert.GreaterOrEqual(before, 0);
        }

        [Test]
        public void ChaptersDescribeEveryStepWithAHint()
        {
            foreach (var m in MissionDatabase.All)
            {
                Assert.IsFalse(string.IsNullOrEmpty(m.startHint), m.title);
                Assert.IsFalse(string.IsNullOrEmpty(m.lesson), m.title);
                foreach (var st in m.steps) Assert.IsFalse(string.IsNullOrEmpty(st.hint), m.title + ": " + st.text);
            }
        }
    }
}
