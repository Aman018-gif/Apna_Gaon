using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace MohallaHero.Tests
{
    public class WorldTests
    {
        static readonly Vector2Int[] Dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        static HashSet<Vector2Int> Flood(WorldMap map, Vector2Int start)
        {
            var seen = new HashSet<Vector2Int> { start };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                foreach (var d in Dirs)
                {
                    var n = c + d;
                    if (map.IsWalkable(n) && seen.Add(n)) queue.Enqueue(n);
                }
            }
            return seen;
        }

        static Vector2Int Start => WorldLayout.ToCell(WorldLayout.PlayerStart);

        /// <summary>Every place the story sends the player or a resident.</summary>
        static IEnumerable<(string what, Vector2Int cell)> PointsOfInterest()
        {
            foreach (var d in NpcDatabase.All)
            {
                yield return (d.name + " work", d.workCell);
                yield return (d.name + " door", d.Door);
            }
            foreach (var b in WorldLayout.Buildings) yield return (b.label + " door", b.Door);
            for (int i = 0; i < WorldLayout.Lights.Length; i++) yield return ("light " + i, WorldLayout.Lights[i]);
            for (int i = 0; i < WorldLayout.Taps.Length; i++) yield return ("tap " + i, WorldLayout.Taps[i]);
            for (int i = 0; i < WorldLayout.Saplings.Length; i++) yield return ("sapling " + i, WorldLayout.Saplings[i]);
            for (int i = 0; i < WorldLayout.Garbage.Length; i++) yield return ("garbage " + i, WorldLayout.Garbage[i]);
            for (int i = 0; i < WorldLayout.MarchRoute.Length; i++) yield return ("march " + i, WorldLayout.MarchRoute[i]);
            yield return ("park board", WorldLayout.ParkBoard);
            yield return ("raddi", WorldLayout.RaddiPile);
            yield return ("notice board", WorldLayout.NoticeBoard);
            yield return ("lucky stall", WorldLayout.LuckyStall);
            yield return ("ration queue", WorldLayout.RationQueue);
            yield return ("placard table", WorldLayout.PlacardTable);
            yield return ("bully corner", WorldLayout.BullyCorner);
            yield return ("bully corner 2", WorldLayout.BullyCorner + Vector2Int.right);
            yield return ("park gather", WorldLayout.ParkGather);
            yield return ("lala footpath", new Vector2Int(30, 33));
            yield return ("fernandes march", new Vector2Int(81, 36));
            foreach (var c in new[] { new Vector2Int(92, 3), new Vector2Int(84, 24), new Vector2Int(79, 11), new Vector2Int(85, 11), new Vector2Int(91, 11), new Vector2Int(79, 5),
                                      new Vector2Int(84, 17), new Vector2Int(80, 17), new Vector2Int(82, 18), new Vector2Int(87, 17), new Vector2Int(85, 8), new Vector2Int(82, 7) })
                yield return ("mela spot " + c, c);
            foreach (var c in WorldLayout.RickshawLoop) yield return ("rickshaw " + c, c);
        }

        [Test]
        public void Generation_IsDeterministic()
        {
            var a = new WorldMap();
            var b = new WorldMap();
            for (int x = 0; x < a.W; x++)
                for (int y = 0; y < a.H; y++)
                {
                    Assert.AreEqual(a.Tiles[x, y], b.Tiles[x, y]);
                    Assert.AreEqual(a.IsWalkable(x, y), b.IsWalkable(x, y));
                }
        }

        [Test]
        public void LockedGates_KeepBazaarAndMaidanClosed()
        {
            var map = new WorldMap();
            Assert.IsTrue(map.IsWalkable(Start), "player start must be walkable");
            foreach (var c in Flood(map, Start))
                Assert.AreEqual(AreaId.Mohalla, WorldLayout.AreaAt(c), $"cell {c} inside a locked area is reachable");
        }

        [Test]
        public void EachGate_OpensOnlyItsOwnArea()
        {
            var map = new WorldMap();
            map.SetGateLocked(AreaId.Bazaar, false);
            var reach = Flood(map, Start);
            Assert.IsTrue(reach.Contains(WorldLayout.LuckyStall));
            foreach (var c in reach) Assert.AreNotEqual(AreaId.Maidan, WorldLayout.AreaAt(c));
        }

        [Test]
        public void EverythingIsReachable_OnceAreasAreOpen()
        {
            var map = new WorldMap();
            foreach (var a in WorldLayout.Gates.Keys) map.SetGateLocked(a, false);
            var reach = Flood(map, Start);
            foreach (var (what, cell) in PointsOfInterest())
            {
                Assert.IsTrue(map.IsWalkable(cell), $"{what} at {cell} is blocked");
                Assert.IsTrue(reach.Contains(cell), $"{what} at {cell} is unreachable");
            }
            // Most of the city should be one connected walkable area (no sealed-off pockets).
            int walkable = 0;
            for (int x = 0; x < map.W; x++) for (int y = 0; y < map.H; y++) if (map.IsWalkable(x, y)) walkable++;
            Assert.Greater(reach.Count, walkable * 0.97f);
        }

        [Test]
        public void ObjectsDoNotOverlap()
        {
            var used = new Dictionary<Vector2Int, string>();
            foreach (var (what, cell) in PointsOfInterest())
            {
                if (what.Contains("door") || what.Contains("work") || what.StartsWith("rickshaw") || what.StartsWith("march") || what.StartsWith("mela") || what.Contains("fernandes")) continue;
                Assert.IsFalse(used.ContainsKey(cell), $"{what} and {(used.ContainsKey(cell) ? used[cell] : "")} share {cell}");
                used[cell] = what;
            }
            foreach (var t in WorldLayout.Trees) Assert.IsFalse(used.ContainsKey(t), "tree on " + used.GetValueOrDefault(t));
        }

        [Test]
        public void AStar_FollowsStreetsAndReachesTheWardOffice()
        {
            var map = new WorldMap();
            var nav = NavGrid.FromWorld(map, 0.6f);
            var path = new List<Vector2Int>();
            Assert.IsTrue(AStar.FindPath(nav, Start, NpcDatabase.Get(NpcId.Fernandes).workCell, path));
            Assert.Greater(path.Count, 10);
            for (int i = 1; i < path.Count; i++)
            {
                Assert.IsTrue(nav.IsWalkable(path[i]));
                var d = path[i] - path[i - 1];
                Assert.LessOrEqual(Mathf.Abs(d.x), 1);
                Assert.LessOrEqual(Mathf.Abs(d.y), 1);
            }
            int paved = 0;
            foreach (var c in path) if (WorldMap.IsPaved(map.Tiles[c.x, c.y])) paved++;
            Assert.Greater(paved, path.Count / 2, "NPCs should prefer streets and footpaths");
        }

        [Test]
        public void VoterHouses_MatchTheDoorCount()
        {
            Assert.AreEqual(Balance.VoterDoors, WorldLayout.VoterHouses.Count);
            foreach (var d in NpcDatabase.All) Assert.DoesNotThrow(() => WorldLayout.HomeOf(d.id), d.name + " has no home");
        }
    }

    public class LogicTests
    {
        [Test]
        public void KarmaLevels_AndNeverBelowZero()
        {
            Assert.AreEqual(1, Balance.KarmaLevel(0));
            Assert.AreEqual(2, Balance.KarmaLevel(Balance.KarmaXpPerLevel));
            Assert.AreEqual(Balance.KarmaMaxLevel, Balance.KarmaLevel(10000));
            var k = new KarmaSystem();
            Assert.AreEqual(Balance.KarmaStart, k.Xp(KarmaStat.Green));
            k.Add(KarmaStat.Green, -500);
            Assert.AreEqual(0, k.Xp(KarmaStat.Green));
            k.Add(KarmaStat.Courage, 25);
            Assert.AreEqual(25, k.TotalGained);
            var snap = k.Snapshot();
            var k2 = new KarmaSystem();
            k2.Load(snap, k.TotalGained, k.TotalLost);
            foreach (var s in KarmaSystem.All) Assert.AreEqual(k.Xp(s), k2.Xp(s));
        }

        [Test]
        public void KarmaStats_AreScriptableObjects_WithDistinctLooks()
        {
            var k = new KarmaSystem();
            var colors = new HashSet<Color>();
            foreach (var s in KarmaSystem.All)
            {
                var a = k.Get(s);
                Assert.IsInstanceOf<ScriptableObject>(a);
                Assert.AreEqual(s, a.stat);
                Assert.IsFalse(string.IsNullOrEmpty(a.description));
                Assert.IsTrue(colors.Add(a.color));
                Assert.IsFalse(string.IsNullOrEmpty(a.Rank));
            }
        }

        [Test]
        public void Trust_ClampsAndCountsSupporters()
        {
            var state = new StoryState();
            var t = new TrustSystem(state);
            Assert.AreEqual(NpcDatabase.Get(NpcId.Raju).initialTrust, t.Get(NpcId.Raju));
            Assert.AreEqual(0, t.JoinedCount);
            t.Add(NpcId.Raju, 500);
            Assert.AreEqual(100, t.Get(NpcId.Raju));
            Assert.AreEqual(1, t.JoinedCount);
            t.Add(NpcId.Meera, 100);   // not one of the residents to convince
            Assert.AreEqual(1, t.JoinedCount);
            t.Add(NpcId.Raju, -1000);
            Assert.AreEqual(0, t.Get(NpcId.Raju));
            Assert.AreEqual(2, state.Snapshot().Count, "only values that differ from the defaults are saved (Raju and Meera)");
        }

        [Test]
        public void Rating_RangeAndViolencePenalty()
        {
            var max = new RatingInput { civic = 999, green = 999, courage = 999, honesty = 999, supporters = 6, votersInformed = 10 };
            Assert.AreEqual(100, EndingEvaluator.Rating(max));
            Assert.AreEqual(0, EndingEvaluator.Rating(new RatingInput()));
            max.violence = 1;
            Assert.AreEqual(100 - Balance.ViolencePenalty, EndingEvaluator.Rating(max));
            Assert.AreEqual(1, EndingEvaluator.Stars(0));
            Assert.AreEqual(3, EndingEvaluator.Stars(50));
            Assert.AreEqual(5, EndingEvaluator.Stars(100));
        }

        [Test]
        public void Endings_DependOnRatingAndInformedVoters()
        {
            Assert.AreEqual(Ending.MohallaHero, EndingEvaluator.Evaluate(80, 6));
            Assert.AreEqual(Ending.JagrukNagrik, EndingEvaluator.Evaluate(60, 5));
            Assert.AreEqual(Ending.AadhaSach, EndingEvaluator.Evaluate(80, 2), "a great rating but too few informed voters");
            Assert.AreEqual(Ending.AadhaSach, EndingEvaluator.Evaluate(30, 4));
            Assert.AreEqual(Ending.JugaadRaj, EndingEvaluator.Evaluate(30, 1));
            foreach (Ending e in System.Enum.GetValues(typeof(Ending)))
                if (e != Ending.None) Assert.IsFalse(string.IsNullOrEmpty(EndingEvaluator.Description(e)));
        }

        [Test]
        public void Slogans_ViolenceIsRefused_PeaceIsRewarded()
        {
            var violent = MiniGameData.EvaluateSlogan(MiniGameData.SloganStarts[0], MiniGameData.SloganEnds[3]);
            Assert.IsFalse(violent.accepted);
            Assert.Less(violent.civic, 0);
            var peaceful = MiniGameData.EvaluateSlogan(MiniGameData.SloganStarts[0], MiniGameData.SloganEnds[0]);
            Assert.IsTrue(peaceful.accepted);
            Assert.Greater(peaceful.civic, 0);
            var angry = MiniGameData.EvaluateSlogan(MiniGameData.SloganStarts[3], MiniGameData.SloganEnds[0]);
            Assert.IsTrue(angry.accepted);
            Assert.Less(angry.civic, peaceful.civic);
            int accepted = 0;
            for (int i = 0; i < MiniGameData.SloganStarts.Length; i++)
                for (int j = 0; j < MiniGameData.SloganEnds.Length; j++)
                    if (MiniGameData.EvaluateSlogan(MiniGameData.SloganStarts[i], MiniGameData.SloganEnds[j]).accepted) accepted++;
            Assert.GreaterOrEqual(accepted, Balance.PlacardsNeeded);
        }

        [Test]
        public void MiniGameContent_IsComplete()
        {
            Assert.GreaterOrEqual(MiniGameData.Waste.Length, Balance.WastePerRound);
            Assert.GreaterOrEqual(MiniGameData.News.Length, Balance.FakeNewsPerRound);
            foreach (WasteBin b in System.Enum.GetValues(typeof(WasteBin)))
                Assert.GreaterOrEqual(System.Array.FindAll(MiniGameData.Waste, w => w.bin == b).Length, 3, "too few items for bin " + b);
            int fakes = System.Array.FindAll(MiniGameData.News, n => n.isFake).Length;
            Assert.Greater(fakes, 0);
            Assert.Less(fakes, MiniGameData.News.Length, "some news must be real, or the game teaches cynicism");
            var round = MiniGameData.Pick(MiniGameData.Waste, 8, new System.Random(3));
            Assert.AreEqual(8, new HashSet<WasteItem>(round).Count);
            Assert.AreEqual((8, 2), MiniGameData.WasteReward(8, 8));
            Assert.AreEqual((3, 0), MiniGameData.WasteReward(3, 8));
            Assert.AreEqual((6, 3), MiniGameData.FakeNewsReward(6, 6));
        }

        [Test]
        public void MissionTracker_FollowsStoryVariables()
        {
            var vars = new Dictionary<string, int>();
            var m = new MissionSystem(n => vars.TryGetValue(n, out var v) ? v : 0);
            Assert.AreEqual(1, m.CurrentChapter);
            StringAssert.Contains("Asha", m.TrackerText());
            vars["m1_started"] = 1;
            vars["lights_off"] = 2;
            Assert.AreEqual("Switch off wasted lights & fans (2/4)", m.TrackerText());
            vars["m1_done"] = 1;
            Assert.AreEqual(2, m.CurrentChapter);
            foreach (var def in MissionDatabase.All) vars[def.doneVar] = 1;
            Assert.IsTrue(m.AllDone);
            Assert.IsNull(m.Current);
        }

        [Test]
        public void ChoiceTags_ParseKarmaTrustWhyAndViolence()
        {
            var r = ChoiceTags.Parse(new[] { "civic:+5", "green:-3", "trust:+15", "trust_lala:-10", "why:Because", "violence", "mood:happy" }, NpcId.Raju);
            Assert.AreEqual(2, r.Karma.Count);
            Assert.AreEqual(2, r.Trust.Count);
            Assert.AreEqual((NpcId.Raju, 15), r.Trust[0]);
            Assert.AreEqual((NpcId.Lala, -10), r.Trust[1]);
            Assert.AreEqual("Because", r.Why);
            Assert.IsTrue(r.Violence);
            var state = new StoryState();
            var k = new KarmaSystem();
            ChoiceTags.Apply(r, k, new TrustSystem(state), state);
            Assert.AreEqual(Balance.KarmaStart + 5, k.Xp(KarmaStat.Civic));
            Assert.AreEqual(1, state.Get("violence"));
        }

        [Test]
        public void Everyone_IsFictional_AndHasAVoiceAndSchedule()
        {
            Assert.AreEqual(12, NpcDatabase.All.Count);
            foreach (var d in NpcDatabase.All)
            {
                Assert.IsFalse(string.IsNullOrEmpty(d.bio));
                Assert.IsFalse(string.IsNullOrEmpty(d.macVoice));
                Assert.Less(d.workStartHour, d.workEndHour);
                Assert.LessOrEqual(d.workEndHour, d.sleepHour);
                Assert.IsTrue(NpcDatabase.TryFind(d.speaker, out var found) && found == d, "speaker lookup for " + d.name);
            }
        }
    }
}
