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
    /// Boots the real game and drives it through its UI like a player: new game, intro, a mission conversation with
    /// the "Why it matters" card, world objects, all three mini-games, the phone, the map, the candle march, the
    /// Election Mela, the polling booth with the ink animation, the ending, then save and continue.
    /// Any exception or error log fails the test. Set the environment variable MH_SHOTS to a folder to also save
    /// screenshots of every step.
    /// </summary>
    public class GameSmokeTests
    {
        GameManager gm;
        string shots;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveSystem.PathOverride = Path.Combine(Application.temporaryCachePath, "mohallahero_test_save.json");
            if (File.Exists(SaveSystem.PathOverride)) File.Delete(SaveSystem.PathOverride);
            VoiceSystem.Muted = true;
            shots = Environment.GetEnvironmentVariable("MH_SHOTS");
            yield return null;
            gm = GameManager.I;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (File.Exists(SaveSystem.PathOverride)) File.Delete(SaveSystem.PathOverride);
            SaveSystem.PathOverride = null;
            Settings.TouchOverride = null;
            VoiceSystem.Muted = false;
            yield return null;
        }

        // ------------------------------------------------------------------ helpers

        static IEnumerator WaitUntil(Func<bool> cond, float seconds, string what)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (!cond())
            {
                if (Time.realtimeSinceStartup > end) Assert.Fail("Timed out waiting for " + what);
                yield return null;
            }
        }

        static IEnumerator Frames(int n)
        {
            for (int i = 0; i < n; i++) yield return null;
        }

        static void Click(Button b)
        {
            Assert.IsNotNull(b, "button not found");
            Assert.IsTrue(b.isActiveAndEnabled && b.interactable, "button not clickable: " + b.name);
            b.onClick.Invoke();
        }

        static Button FindButton(Component root, string textContains) =>
            root.GetComponentsInChildren<Button>(false).FirstOrDefault(b =>
            {
                var t = b.GetComponentInChildren<Text>();
                return t != null && t.text.Contains(textContains);
            });

        /// <summary>Clicks through a conversation (picking option <paramref name="pick"/>, default the first).</summary>
        IEnumerator ClickThrough(Func<int, int> pick = null, string shotName = null, string whyShot = null, int max = 80)
        {
            int page = 0;
            bool whyTaken = false;
            for (int i = 0; i < max; i++)
            {
                yield return Frames(2);
                var modal = gm.UI.CurrentModal;
                if (modal == gm.UI.Why)
                {
                    if (whyShot != null && !whyTaken) { whyTaken = true; yield return Shot(whyShot); }
                    Click(gm.UI.Why.GetComponentsInChildren<Button>().Last());
                    continue;
                }
                if (modal == gm.UI.Dialogue)
                {
                    if (shotName != null && page == 1) yield return Shot(shotName);
                    var opts = gm.UI.Dialogue.transform.Find("Box/Options").GetComponentsInChildren<Button>();
                    Assert.Greater(opts.Length, 0);
                    Assert.LessOrEqual(opts.Length, 4, "the spec allows 2-4 choices");
                    int k = pick != null ? Mathf.Clamp(pick(opts.Length), 0, opts.Length - 1) : 0;
                    Click(opts[k]);
                    page++;
                    continue;
                }
                if (!gm.Dialogue.Active) yield break;
            }
            Assert.Fail("conversation did not finish");
        }

        /// <summary>Renders the world and the UI into a PNG (when MH_SHOTS is set).</summary>
        IEnumerator Shot(string name)
        {
            yield return null;
            if (string.IsNullOrEmpty(shots)) yield break;
            Directory.CreateDirectory(shots);
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
            File.WriteAllBytes(Path.Combine(shots, name + ".png"), tex.EncodeToPNG());
            cam.targetTexture = null;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            UnityEngine.Object.Destroy(tex);
            rt.Release();
        }

        void TeleportNear(Vector2 p)
        {
            gm.Player.Teleport(p);
            gm.CameraRig.SnapToTarget();
        }

        void Finish(params string[] vars)
        {
            foreach (var v in vars) gm.Story.Set(v, 1);
        }

        // ------------------------------------------------------------------ the run

        [UnityTest]
        public IEnumerator FullGame_SmokeRun()
        {
            Assert.IsNotNull(gm, "GameManager did not boot");
            yield return Shot("01_main_menu");

            gm.NewGame("Tester", Gender.Female);
            yield return WaitUntil(() => gm.InWorld && !gm.UI.IsFading, 15, "new game");
            yield return WaitUntil(() => gm.Dialogue.Active, 5, "intro");
            yield return ClickThrough(shotName: "02_intro_dialogue");
            Assert.IsTrue(gm.Story.Is("intro_done"));
            yield return Frames(30);
            yield return Shot("03_city_start_low_rating");

            // ---- Chapter 1 conversation with the Why card
            gm.Clock.SkipTo(9f);
            gm.Npcs.PlaceAll();
            var asha = gm.Npcs.Get(NpcId.Asha);
            yield return WaitUntil(() => !asha.Hidden, 5, "Asha visible");
            TeleportNear((Vector2)asha.transform.position + new Vector2(0, -0.9f));
            yield return Frames(3);
            gm.Dialogue.TalkTo(asha);
            yield return ClickThrough(whyShot: "04_why_it_matters");
            Assert.IsTrue(gm.Story.Is("m1_started"), "Asha should start chapter 1");
            Assert.Greater(gm.Karma.Xp(KarmaStat.Civic), Balance.KarmaStart);

            // ---- a wasted light, through the real Interact path
            var light = gm.Builder.Objects.First(o => o.Id == "light_0");
            TeleportNear((Vector2)light.transform.position + new Vector2(0, -0.8f));
            yield return Frames(5);
            yield return Shot("05_light_prompt");
            light.Interact(gm.Player);
            yield return ClickThrough();
            Assert.IsTrue(light.Resolved);
            Assert.AreEqual(1, gm.Story.Get("lights_off"));

            // ---- garbage → waste sorting mini-game
            var garbage = gm.Builder.Objects.First(o => o.Id == "garbage_0");
            TeleportNear((Vector2)garbage.transform.position + new Vector2(0, -1f));
            garbage.Interact(gm.Player);
            yield return ClickThrough();
            yield return WaitUntil(() => gm.UI.CurrentModal == gm.UI.Waste, 3, "waste mini-game");
            for (int i = 0; i < Balance.WastePerRound; i++)
            {
                yield return Frames(2);
                Click(gm.UI.Waste.transform.Find("Window/Body/Bins").GetComponentsInChildren<Button>()[i % 3]);
                yield return Frames(2);
                if (i == 0) yield return Shot("06_waste_sorting");
                Click(FindButton(gm.UI.Waste, "Agla"));
            }
            yield return Frames(2);
            Click(FindButton(gm.UI.Waste, "Ho gaya"));
            yield return Frames(2);
            Assert.IsNull(gm.UI.CurrentModal);
            Assert.AreEqual(1, gm.Story.Get("piles_cleaned"));

            // ---- the phone
            gm.UI.Open(gm.UI.Phone);
            yield return Shot("07_phone_home");
            foreach (var (app, shot) in new[] { ("App_Missions", "08_phone_missions"), ("App_Karma", "09_phone_karma"), ("App_Bharosa", "10_phone_trust"), ("App_Pintu", "11_phone_hint"), ("App_Settings", null), ("App_Saboot", null) })
            {
                Click(gm.UI.Phone.transform.Find("PhoneBody/Screen/Home/Grid/" + app).GetComponentInChildren<Button>());
                yield return Frames(2);
                if (shot != null) yield return Shot(shot);
                Click(FindButton(gm.UI.Phone.transform.Find("PhoneBody/Screen/Page"), "<"));
                yield return Frames(1);
            }
            gm.UI.Close(gm.UI.Phone);

            gm.UI.Open(gm.UI.Map);
            yield return Shot("12_map");
            gm.UI.Close(gm.UI.Map);
            gm.UI.Open(gm.UI.Pause);
            yield return Frames(2);
            gm.UI.Close(gm.UI.Pause);

            // ---- the mobile layout
            Settings.TouchOverride = true;
            yield return Frames(3);
            yield return Shot("13_touch_controls");
            Settings.TouchOverride = false;

            // ---- fake news and slogans
            gm.UI.OpenMiniGame("fakenews");
            for (int i = 0; i < Balance.FakeNewsPerRound; i++)
            {
                yield return Frames(2);
                if (i == 0) { Click(FindButton(gm.UI.FakeNews, "Source")); yield return Frames(1); yield return Shot("14_fake_news"); }
                Click(FindButton(gm.UI.FakeNews, i % 2 == 0 ? "AFWAAH" : "SACH"));
                yield return Frames(2);
                Click(FindButton(gm.UI.FakeNews, "Agli"));
            }
            yield return Frames(2);
            Click(FindButton(gm.UI.FakeNews, "Ho gaya"));
            Assert.IsTrue(gm.Story.Is("rumours_checked"));

            gm.Story.Set("m5_started", 1);
            gm.UI.OpenMiniGame("slogan");
            yield return Frames(2);
            for (int i = 0; i < Balance.PlacardsNeeded; i++)
            {
                Click(FindButton(gm.UI.Slogan, MiniGameData.SloganStarts[i].text));
                Click(FindButton(gm.UI.Slogan, MiniGameData.SloganEnds[i].text));
                Click(FindButton(gm.UI.Slogan, "Placard banao"));
                yield return Frames(2);
            }
            yield return Shot("15_slogans");
            Assert.AreEqual(Balance.PlacardsNeeded, gm.Story.Get("placards"));
            gm.UI.Close(gm.UI.Slogan);

            // ---- jump to chapter 5: the candle march at dusk
            Finish("m1_done", "m2_started", "m2_done", "m3_started", "m3_done", "m4_started", "m4_done");
            foreach (var id in TrustSystem.Convincible) gm.Trust.Add(id, 100);
            foreach (var s in KarmaSystem.All) gm.Karma.Add(s, 60);
            gm.CheckProgress();
            Assert.AreEqual(5, gm.Missions.CurrentChapter);
            gm.Protest.Gather();
            gm.Story.Set("m5_gathered", 1);
            gm.Protest.StartMarch();
            gm.Clock.SkipTo(19.6f);
            for (int i = 0; i < 90; i++)
            {
                TeleportNear(Vector2.Lerp(WorldLayout.CellFeet(WorldLayout.ParkGather), WorldLayout.CellFeet(WorldLayout.MarchRoute[0]), i / 90f));
                yield return null;
            }
            yield return new WaitForSecondsRealtime(4f);   // let the procession catch up
            yield return Shot("16_candle_march");
            Assert.AreEqual(1, gm.Story.Get("march_checkpoints"));
            Assert.IsTrue(gm.Protest.FollowerIndex(NpcId.Meera, out _));

            // ---- chapter 6: the Election Mela
            gm.Story.Set("march_checkpoints", 4);
            Finish("m5_done");
            gm.Areas.Unlock(AreaId.Maidan);
            gm.Clock.SkipTo(19.6f);
            gm.CheckProgress();
            Assert.AreEqual(6, gm.Missions.CurrentChapter);
            yield return Frames(5);
            var fern = gm.Npcs.Get(NpcId.Fernandes);
            fern.PlaceBySchedule();
            TeleportNear((Vector2)fern.transform.position + new Vector2(0, -0.9f));
            yield return Frames(3);
            gm.Dialogue.TalkTo(fern);
            yield return ClickThrough();
            Assert.IsTrue(gm.Story.Is("m6_started"));
            gm.Npcs.PlaceAll();
            TeleportNear(WorldLayout.CellFeet(new Vector2Int(84, 13)));
            yield return Frames(30);
            yield return Shot("17_election_mela");

            var door = gm.Builder.Objects.First(o => o.Id == "door_0");
            TeleportNear((Vector2)door.transform.position + new Vector2(0, -0.8f));
            door.Interact(gm.Player);
            yield return ClickThrough();
            Assert.AreEqual(1, gm.Story.Get("voters_informed"));

            // ---- the polling booth
            var booth = gm.Builder.Objects.First(o => o.Id == "booth");
            TeleportNear((Vector2)booth.transform.position + new Vector2(0, -0.8f));
            booth.Interact(gm.Player);
            yield return ClickThrough();
            yield return WaitUntil(() => gm.UI.CurrentModal == gm.UI.Voting, 3, "voting screen");
            yield return Frames(2);
            Click(FindButton(gm.UI.Voting, "Voter slip"));
            yield return new WaitForSecondsRealtime(2.6f);
            yield return Shot("18_ink_on_finger");
            yield return WaitUntil(() => gm.UI.Voting.transform.Find("Window/Body/Evm").gameObject.activeSelf, 8, "EVM");
            yield return Shot("19_evm");
            var evmButtons = gm.UI.Voting.transform.Find("Window/Body/Evm/Machine").GetComponentsInChildren<Button>();
            Assert.AreEqual(4, evmButtons.Length, "3 candidates + NOTA");
            Click(evmButtons[2]);
            yield return new WaitForSecondsRealtime(1f);
            yield return Shot("20_vvpat");
            yield return WaitUntil(() => gm.UI.Voting.transform.Find("Window/Body/Done").gameObject.activeSelf, 6, "vote done");
            Click(FindButton(gm.UI.Voting, "bahar"));
            yield return WaitUntil(() => gm.UI.CurrentModal == gm.UI.Ending, 10, "ending screen");
            yield return Frames(3);
            yield return Shot("21_ending");
            Assert.IsTrue(gm.GameOver);
            Assert.AreEqual(Candidate.Meera, gm.Election.MyVote);
            Assert.IsTrue(gm.Story.Is("m6_done"));
            gm.UI.Close(gm.UI.Ending);

            // ---- a bright, festive mohalla in daylight
            gm.Clock.Set(gm.Clock.Day, 11 * 60);
            foreach (var s in KarmaSystem.All) gm.Karma.Add(s, 200);
            gm.Story.Set("voters_informed", 10);
            gm.CheckProgress();
            TeleportNear(WorldLayout.CellFeet(new Vector2Int(60, 36)));
            yield return Frames(80);
            yield return Shot("22_city_high_rating");

            // ---- save, quit, continue
            var ending = gm.Election.Ending;
            Assert.AreNotEqual(Ending.None, ending);
            gm.SaveGame();
            Assert.IsTrue(SaveSystem.HasSave());
            gm.QuitToMenu();
            yield return WaitUntil(() => !gm.InWorld && !gm.UI.IsFading, 10, "main menu");
            gm.ContinueGame();
            yield return WaitUntil(() => gm.InWorld && !gm.UI.IsFading, 10, "continue");
            Assert.IsTrue(gm.GameOver, "the result must survive save/load");
            Assert.AreEqual(ending, gm.Election.Ending);
            Assert.IsTrue(gm.Story.Is("obj_light_0"), "world objects stay fixed after loading");
            gm.QuitToMenu();
            yield return WaitUntil(() => !gm.InWorld && !gm.UI.IsFading, 10, "back to menu");
        }
    }
}
