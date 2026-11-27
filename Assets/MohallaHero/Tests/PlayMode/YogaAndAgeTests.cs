using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MohallaHero.Tests
{
    /// <summary>
    /// The Yoga Kendra through its real UI (questions, a fast-forwarded 5-minute session, completion, return) and the
    /// player growing up across chapter time-skips, including save/load and migration of an old (version 1) save.
    /// </summary>
    public class YogaAndAgeTests
    {
        GameManager gm;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveSystem.PathOverride = Path.Combine(Application.temporaryCachePath, "mohallahero_yoga_test.json");
            if (File.Exists(SaveSystem.PathOverride)) File.Delete(SaveSystem.PathOverride);
            VoiceSystem.Muted = true;
            Settings.TouchOverride = false;
            yield return null;
            gm = GameManager.I;
            Assert.IsNotNull(gm, "GameManager did not boot");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            YogaScreen.Speed = 1f;
            if (gm != null && gm.InWorld) { gm.UI.CloseModal(); gm.QuitToMenu(); yield return WaitUntil(() => !gm.UI.IsFading && !gm.InWorld, 10, "menu"); }
            if (File.Exists(SaveSystem.PathOverride)) File.Delete(SaveSystem.PathOverride);
            SaveSystem.PathOverride = null;
            Settings.TouchOverride = null;
            VoiceSystem.Muted = false;
        }

        /// <summary>Renders world + UI to MH_SHOTS/name.png (same method as GameSmokeTests).</summary>
        IEnumerator Shot(string name)
        {
            yield return null;
            var dir = Environment.GetEnvironmentVariable("MH_SHOTS");
            if (string.IsNullOrEmpty(dir)) yield break;
            Directory.CreateDirectory(dir);
            const int W = 1600, H = 900;
            var cam = gm.CameraRig.Cam;
            var canvas = gm.UI.GetComponent<Canvas>();
            var rt = new RenderTexture(W, H, 24);
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 1f;
            int order = canvas.sortingOrder;
            canvas.sortingOrder = 1000;
            cam.targetTexture = rt;
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();
            cam.Render();
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG());
            cam.targetTexture = null;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            UnityEngine.Object.Destroy(tex);
            rt.Release();
        }

        static IEnumerator WaitUntil(Func<bool> cond, float seconds, string what)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (!cond())
            {
                if (Time.realtimeSinceStartup > end) Assert.Fail("Timed out waiting for " + what);
                yield return null;
            }
        }

        static Button Find(Component root, string text) =>
            root.GetComponentsInChildren<Button>(false).FirstOrDefault(b => b.interactable && b.GetComponentInChildren<Text>().text.Contains(text));

        static void Click(Component root, string text)
        {
            var b = Find(root, text);
            Assert.IsNotNull(b, "no clickable button with " + text);
            b.onClick.Invoke();
        }

        IEnumerator StartWorld()
        {
            gm.NewGame("Tester", Gender.Male);
            yield return WaitUntil(() => gm.InWorld && !gm.UI.IsFading, 15, "world");
            if (gm.Dialogue.Active) gm.Dialogue.Cancel();
            yield return null;
            gm.UI.CloseModal();
            yield return null;
        }

        [UnityTest]
        public IEnumerator YogaKendra_FullSession_PausesTheCityAndChangesNothing()
        {
            yield return StartWorld();
            var door = UnityEngine.Object.FindAnyObjectByType<YogaDoor>();
            Assert.IsNotNull(door, "the Yoga Kendra entrance exists");

            gm.Player.Teleport(WorldLayout.CellFeet(WorldLayout.YogaCenter.Door) + new Vector2(0, -1.2f));
            gm.CameraRig.SnapToTarget();
            yield return Shot("yoga_01_building");
            int day = gm.Clock.Day, money = gm.Money;
            float minute = gm.Clock.Minute;
            var karma = gm.Karma.Snapshot();
            int vars = gm.Story.Snapshot().Count;
            door.Interact(gm.Player);
            yield return null;
            var yoga = gm.UI.Yoga;
            Assert.AreEqual(yoga, gm.UI.CurrentModal);
            yield return Shot("yoga_02_welcome");
            Click(yoga, "Full Body"); yield return null;
            Click(yoga, "5 minute"); yield return null;
            Click(yoga, "Pehli baar"); yield return null;
            yield return Shot("yoga_03_summary");
            Click(yoga, "Haan, shuru"); yield return null;
            Assert.IsTrue(yoga.InSession);

            YogaScreen.Speed = 40f;   // 5 minutes in ~8 seconds
            float end = Time.realtimeSinceStartup + 40f;
            int trick = 0, shots = 0;
            while (!yoga.IsComplete)
            {
                if (shots < 4 && yoga.Elapsed > 14f + shots * 31f) { YogaScreen.Speed = 1f; yield return Shot("yoga_04_session_" + shots++); YogaScreen.Speed = 40f; }
                Assert.Less(Time.realtimeSinceStartup, end, "the session did not finish");
                var karo = Find(yoga, "KARO");
                // follow every instruction except one in three, to exercise both the joined and the missed paths
                if (karo != null && (trick++ % 3) != 2) karo.onClick.Invoke();
                yield return null;
            }
            Assert.Greater(yoga.PosesCompleted, 3);
            Assert.GreaterOrEqual(yoga.Elapsed, 299f, "a 5-minute session lasts 5 minutes");
            Assert.Less(yoga.Elapsed, 302f);
            var summary = yoga.GetComponentsInChildren<Text>().Select(t => t.text).FirstOrDefault(t => t.Contains("poses completed"));
            Assert.IsNotNull(summary, "the completion card shows the poses completed");
            yield return new WaitForSecondsRealtime(1.6f);
            yield return Shot("yoga_05_complete");

            Assert.AreEqual(day, gm.Clock.Day, "yoga does not use the city's days");
            Assert.AreEqual(minute, gm.Clock.Minute, "the city clock stood still during yoga");
            Assert.AreEqual(money, gm.Money);
            CollectionAssert.AreEqual(karma, gm.Karma.Snapshot(), "yoga does not change karma");
            Assert.AreEqual(vars, gm.Story.Snapshot().Count, "yoga does not touch the story");

            YogaScreen.Speed = 1f;
            yield return null;
            Click(yoga, "Dobara");
            yield return null;
            Assert.IsTrue(yoga.InSession, "repeat starts the same session again");
            Click(yoga, "Khatam");
            yield return null;
            Assert.IsTrue(yoga.IsComplete);
            yield return null;
            Click(yoga, "Shehar wapas");
            yield return null;
            Assert.IsNull(gm.UI.CurrentModal, "back in the city");
        }

        [UnityTest]
        public IEnumerator Player_GrowsUpWithTheStory_AndAgeSurvivesSaving()
        {
            yield return StartWorld();
            Assert.AreEqual(11, gm.Age.Years);
            Assert.AreEqual(11, gm.Player.Years);
            Assert.AreEqual(DayOfWeek.Monday, gm.Age.Date.DayOfWeek, "Din 1 is a Monday");

            foreach (var v in new[] { "m1_done", "m2_done", "m3_done" }) gm.Story.Set(v, 1);
            gm.CheckProgress();
            yield return WaitUntil(() => !gm.UI.IsFading, 10, "time-skip fade");
            Assert.AreEqual(4, gm.Missions.CurrentChapter);
            Assert.AreEqual(AgeSystem.CumulativeSkip(4), gm.Age.SkipDays);
            Assert.AreEqual(14, gm.Age.Years);
            Assert.AreEqual(14, gm.Player.Years, "the player sprite follows the calendar");
            gm.Player.Teleport(WorldLayout.PlayerStart);
            gm.CameraRig.SnapToTarget();
            yield return Shot("age_01_fourteen");

            gm.SaveGame();
            var saved = SaveSystem.Load();
            Assert.AreEqual(3, saved.version);
            Assert.AreEqual(14, saved.ageYears);
            Assert.AreEqual((int)GrowthStage.Teen, saved.growthStage);

            gm.QuitToMenu();
            yield return WaitUntil(() => !gm.UI.IsFading && !gm.InWorld, 10, "menu");
            gm.ContinueGame();
            yield return WaitUntil(() => gm.InWorld && !gm.UI.IsFading, 15, "continue");
            Assert.AreEqual(14, gm.Age.Years, "age persists after reloading");
            Assert.AreEqual(AgeSystem.CumulativeSkip(4), gm.Age.SkipDays);

            // an old save (from before ages existed) at Chapter 5 must not load as a 12-year-old
            gm.QuitToMenu();
            yield return WaitUntil(() => !gm.UI.IsFading && !gm.InWorld, 10, "menu");
            var legacy = SaveSystem.Load();
            legacy.version = 1;
            legacy.ageSkipDays = 0;
            legacy.ageYears = 0;
            legacy.lastAgeAnnounced = 0;
            legacy.lastChapterAnnounced = 5;
            foreach (var v in new[] { "m4_done" }) legacy.vars.Add(new VarEntry { name = v, value = 1 });
            SaveSystem.Save(legacy);
            gm.ContinueGame();
            yield return WaitUntil(() => gm.InWorld && !gm.UI.IsFading, 15, "continue legacy");
            Assert.AreEqual(AgeSystem.CumulativeSkip(5), gm.Age.SkipDays);
            Assert.AreEqual(16, gm.Age.Years, "a migrated Chapter 5 save is 16");
        }

        [UnityTest]
        public IEnumerator LockedAreas_ShowGrey_UntilTheStoryOpensThem()
        {
            yield return StartWorld();
            Assert.IsNotNull(GameObject.Find("LockedShade_Bazaar"), "the closed Bazaar Gali is shaded grey");
            Assert.IsNotNull(GameObject.Find("LockedShade_Maidan"), "the closed Mela Maidan is shaded grey");
            gm.Player.Teleport(WorldLayout.CellFeet(new Vector2Int(34, 31)));
            gm.CameraRig.SnapToTarget();
            yield return Shot("locked_01_grey");
            gm.Areas.Unlock(AreaId.Bazaar, false);
            yield return null;
            yield return null;
            Assert.IsNull(GameObject.Find("LockedShade_Bazaar"), "the grey veil goes away when the area opens");
            Assert.IsNotNull(GameObject.Find("LockedShade_Maidan"), "the other area stays grey");
            yield return Shot("locked_02_open");
        }

        [UnityTest]
        public IEnumerator DailyYoga_BlocksSleepUntilDone_ExceptOnSunday()
        {
            yield return StartWorld();
            Assert.AreEqual(1, gm.Clock.Day);
            Assert.IsTrue(gm.YogaPending, "Monday: yoga is owed");

            gm.Sleep();                                    // blocked
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.IsFalse(gm.UI.IsFading, "no fade: sleeping is refused");
            Assert.AreEqual(1, gm.Clock.Day, "the day did not advance");

            Assert.IsFalse(gm.CompleteDailyYoga(200f), "a short session does not count");
            Assert.IsTrue(gm.YogaPending);
            Assert.IsTrue(gm.CompleteDailyYoga(300f));
            Assert.IsFalse(gm.YogaPending);
            Assert.AreEqual(1, gm.Yoga.Streak);

            gm.Sleep();
            yield return WaitUntil(() => gm.Clock.Day == 2 && !gm.UI.IsFading, 15, "next day");
            Assert.AreEqual(DayOfWeek.Tuesday, gm.Age.Date.DayOfWeek);
            Assert.IsTrue(gm.YogaPending, "a new day owes yoga again");

            // Din 2 (Tuesday) ends at 11 PM with no yoga: the streak breaks and the miss is counted
            gm.EndOfDay();
            yield return WaitUntil(() => gm.Clock.Day == 3 && !gm.UI.IsFading, 15, "Din 3");
            Assert.AreEqual(1, gm.Yoga.Missed, "Din 2 ended without yoga");
            Assert.AreEqual(0, gm.Yoga.Streak, "the missed day broke the streak");

            // skip ahead to Sunday (Din 7): rest day, sleeping is always allowed
            gm.Clock.Set(7, gm.Clock.Minute);
            Assert.AreEqual(DayOfWeek.Sunday, gm.Age.Date.DayOfWeek);
            Assert.IsFalse(gm.YogaPending, "Sunday is a rest day");
            gm.Sleep();
            yield return WaitUntil(() => gm.Clock.Day == 8 && !gm.UI.IsFading, 15, "Monday");
            Assert.AreEqual(DayOfWeek.Monday, gm.Age.Date.DayOfWeek);
            Assert.AreEqual(1, gm.Yoga.Missed, "a Sunday is never counted as missed");
        }
    }
}
