using System;
using UnityEngine;

namespace MohallaHero
{
    /// <summary>
    /// Central game state and the glue between systems. Boots automatically in any scene, shows the main menu,
    /// and builds/tears down world sessions. Owns the persistent UI, camera, voices, settings and dialogue runner.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager I { get; private set; }

        // ---- persistent
        public UIManager UI { get; private set; }
        public CameraRig CameraRig { get; private set; }
        public VoiceSystem Voice { get; private set; }
        public DialogueManager Dialogue { get; private set; }
        public Settings Settings { get; } = new Settings();

        // ---- session state
        public bool InWorld { get; private set; }
        public bool IsModalOpen => UI != null && UI.IsModalOpen;
        public string PlayerName { get; private set; } = "Hero";
        public Gender PlayerGender { get; private set; }
        public int Money { get; private set; }
        public System.Random Rng { get; private set; } = new System.Random();
        public bool GameOver { get; private set; }
        int lastChapterAnnounced;

        // ---- systems (pure logic)
        public StoryState Story { get; private set; }
        public KarmaSystem Karma { get; private set; }
        public TrustSystem Trust { get; private set; }
        public MissionSystem Missions { get; private set; }

        // ---- world
        public Transform WorldRoot { get; private set; }
        public WorldMap Map { get; private set; }
        public NavGrid Nav { get; private set; }
        public WorldBuilder Builder { get; private set; }
        public AreaManager Areas { get; private set; }
        public TimeManager Clock { get; private set; }
        public NPCManager Npcs { get; private set; }
        public CityVisuals Visuals { get; private set; }
        public ProtestManager Protest { get; private set; }
        public ElectionManager Election { get; private set; }
        public PlayerController Player { get; private set; }

        /// <summary>Mohalla Rating, 0–100 (see EndingEvaluator).</summary>
        public int Rating => Karma == null ? 0 : EndingEvaluator.Rating(RatingInput.From(Karma, Trust.JoinedCount, Story.Get("voters_informed"), Story.Get("violence")));

        // ------------------------------------------------------------------ boot

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            I = null;
            GameEvents.ResetAll();
            Interactable.All.Clear();
            StoryLibrary.ClearCache();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (I != null) return;
            var go = new GameObject("MohallaHero");
            DontDestroyOnLoad(go);
            go.AddComponent<GameManager>();
        }

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            Physics2D.gravity = Vector2.zero;
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            CameraRig = CameraRig.Create();
            UI = UIManager.Create(transform);
            Voice = gameObject.AddComponent<VoiceSystem>();
            Dialogue = new DialogueManager();
            UI.ShowMainMenu();
        }

        // ------------------------------------------------------------------ sessions

        public void NewGame(string playerName, Gender gender)
        {
            var d = new SaveData { playerName = playerName, gender = (int)gender };
            var start = WorldLayout.PlayerStart;
            d.posX = start.x;
            d.posY = start.y;
            UI.Fade("Din 1 · Shanti Nagar", () => StartSession(d), () => Dialogue.RunKnot("intro"));
        }

        public void ContinueGame()
        {
            var d = SaveSystem.Load();
            if (d == null) { UI.Notify("Koi saved game nahi mila."); return; }
            UI.Fade($"Din {d.day}", () => StartSession(d), () =>
            {
                UI.Notify($"Welcome back, {PlayerName}!");
                if (GameOver) UI.Notify("Election ho chuka hai. Phone mein apna ending dekho, ya mohalla ghoom lo!");
            });
        }

        void StartSession(SaveData d)
        {
            if (InWorld) EndSession();
            GameEvents.ResetAll();
            Interactable.All.Clear();
            Rng = new System.Random(Environment.TickCount);

            PlayerName = string.IsNullOrEmpty(d.playerName) ? "Hero" : d.playerName;
            PlayerGender = (Gender)d.gender;
            Money = d.money;
            GameOver = d.gameOver;
            lastChapterAnnounced = d.lastChapterAnnounced;

            // --- pure systems
            Story = new StoryState();
            Story.SetDefaults(StoryLibrary.Story.VarDefaults);
            Trust = new TrustSystem(Story);
            Story.Load(d.vars);
            Karma = new KarmaSystem();
            Karma.Load(d.karma, d.karmaGained, d.karmaLost);
            Missions = new MissionSystem(Value);

            // --- world
            WorldRoot = new GameObject("World").transform;
            Map = new WorldMap();
            Clock = WorldRoot.gameObject.AddComponent<TimeManager>();
            Clock.Set(d.day, d.minute);
            Areas = WorldRoot.gameObject.AddComponent<AreaManager>();
            Builder = WorldRoot.gameObject.AddComponent<WorldBuilder>();
            Builder.Build(Map, Areas);
            Areas.Init(Map, Builder.Blockers);
            Nav = NavGrid.FromWorld(Map, 0.6f);
            AStar.InvalidateCostCache();
            if (d.unlockedAreas != null)
                foreach (var a in d.unlockedAreas) Areas.Unlock((AreaId)a, false);

            // --- actors
            Player = PlayerController.Spawn(WorldRoot, new Vector2(d.posX, d.posY), PlayerGender);
            CameraRig.Target = Player.transform;
            CameraRig.SnapToTarget();
            CameraRig.EnableOverlay(true);

            Protest = WorldRoot.gameObject.AddComponent<ProtestManager>();
            Protest.Init(WorldRoot);
            Election = WorldRoot.gameObject.AddComponent<ElectionManager>();
            Election.Init(WorldRoot);
            Election.Restore(d);
            Npcs = new GameObject("NPCs").AddComponent<NPCManager>();
            Npcs.transform.SetParent(WorldRoot, false);
            Npcs.Spawn();

            // --- wiring
            Story.Changed += (name, value) => GameEvents.RaiseVarChanged(name, value);
            Karma.Changed += (s, delta) =>
            {
                GameEvents.RaiseKarmaChanged(s, delta);
                UI.Hud.FloatKarma(s, delta);
            };
            Trust.Changed += (id, v) => GameEvents.RaiseTrustChanged(id, v);

            Visuals = WorldRoot.gameObject.AddComponent<CityVisuals>();
            Visuals.Init(Builder);
            WorldRoot.gameObject.AddComponent<CityLighting>().Init(Builder, Player.transform, CameraRig.Cam);
            var ambience = new GameObject("Ambience").AddComponent<Ambience>();
            ambience.transform.SetParent(WorldRoot, false);
            ambience.Init();

            InWorld = true;
            Npcs.PlaceAll();
            Protest.RestoreAfterLoad();
            UI.ShowGameUI();
            CheckProgress();
        }

        /// <summary>Story values for missions: variables plus computed ones.</summary>
        int Value(string name)
        {
            if (name == "joined") return Trust.JoinedCount;
            return Story.Get(name);
        }

        void EndSession()
        {
            if (Dialogue.Active) Dialogue.Cancel();
            Voice.Stop();
            InWorld = false;
            UI.CloseModal();
            if (WorldRoot != null) Destroy(WorldRoot.gameObject);
            WorldRoot = null;
            Player = null;
            GameEvents.ResetAll();
            Interactable.All.Clear();
            CameraRig.Target = null;
            CameraRig.EnableOverlay(false);
        }

        public void QuitToMenu()
        {
            UI.Fade("", () =>
            {
                EndSession();
                UI.ShowMainMenu();
            });
        }

        // ------------------------------------------------------------------ progress

        /// <summary>Called after every conversation and mini-game: announces new chapters and refreshes the city.</summary>
        public void CheckProgress()
        {
            if (!InWorld) return;
            int ch = Missions.CurrentChapter;
            if (ch != lastChapterAnnounced)
            {
                lastChapterAnnounced = ch;
                var m = Missions.Current;
                if (m != null)
                {
                    UI.Notify($"Chapter {ch}: {m.title}\n<size=34>{m.tagline}</size>", NotifyStyle.Big);
                    UI.Hud.PlayChapterJingle();
                }
                GameEvents.RaiseChapterStarted(ch);
                Npcs.PlaceAll();
                SaveGame();
            }
            GameEvents.RaiseRatingChanged();
        }

        public bool HasItem(string id) => Story.Get(ItemDatabase.Var(id)) > 0;

        public void GiveItem(string id)
        {
            Story.Set(ItemDatabase.Var(id), 1);
            var def = ItemDatabase.Get(id);
            UI.Notify(def != null && def.evidence ? $"Saboot mila: {def.name}! (Phone → Saboot)" : $"Mila: {(def != null ? def.name : id)}");
        }

        public void AddMoney(int amount) => Money += amount;

        public bool TrySpend(int amount)
        {
            if (Money < amount) return false;
            Money -= amount;
            return true;
        }

        public void RefreshNavigation()
        {
            if (Nav == null) return;
            Nav.Refresh(Map, 0.6f);
            AStar.InvalidateCostCache();
        }

        /// <summary>A mini-game finished: apply its karma and story results.</summary>
        public void ApplyMiniGameResult(MiniGame game, int civic, int green, int courage, int honesty, string storyVar, int storyValue)
        {
            if (civic != 0) Karma.Add(KarmaStat.Civic, civic);
            if (green != 0) Karma.Add(KarmaStat.Green, green);
            if (courage != 0) Karma.Add(KarmaStat.Courage, courage);
            if (honesty != 0) Karma.Add(KarmaStat.Honesty, honesty);
            if (!string.IsNullOrEmpty(storyVar)) Story.Set(storyVar, storyValue);
            CheckProgress();
        }

        // ------------------------------------------------------------------ days

        public void Sleep()
        {
            UI.Fade($"Din {Clock.Day + 1}", StartNewDay, () => UI.Notify("Subah ho gayi! Game saved."));
        }

        /// <summary>The clock reached 11 PM: everyone goes home.</summary>
        public void EndOfDay()
        {
            if (!InWorld || UI.IsFading) return;
            Clock.Running = false;
            UI.Fade("Raat ke 11 baj gaye... ghar chalo!", () => { StartNewDay(); Clock.Running = true; },
                () => UI.Notify("Naya din, naye mauke. Game saved."));
        }

        void StartNewDay()
        {
            Clock.BeginNextDay();
            Player.Teleport(WorldLayout.PlayerStart);
            CameraRig.SnapToTarget();
            Npcs.PlaceAll();
            Protest.RestoreAfterLoad();
            GameEvents.RaiseDayStarted(Clock.Day);
            SaveGame();
        }

        public void FinishGame()
        {
            GameOver = true;
            SaveGame();
        }

        // ------------------------------------------------------------------ save

        public void SaveGame()
        {
            if (!InWorld) return;
            var d = new SaveData
            {
                playerName = PlayerName,
                gender = (int)PlayerGender,
                money = Money,
                posX = Player.transform.position.x,
                posY = Player.transform.position.y,
                karma = Karma.Snapshot(),
                karmaGained = Karma.TotalGained,
                karmaLost = Karma.TotalLost,
                day = Clock.Day,
                minute = Clock.Minute,
                unlockedAreas = Areas.Snapshot(),
                vars = Story.Snapshot(),
                lastChapterAnnounced = lastChapterAnnounced,
                gameOver = GameOver,
                ending = (int)Election.Ending,
                winner = (int)Election.Winner,
                myVote = (int)Election.MyVote,
                finalRating = Election.FinalRating,
            };
            try { SaveSystem.Save(d); }
            catch (Exception e) { Debug.LogError("Mohalla Hero: save failed: " + e); UI.Notify("Game save nahi ho paaya!"); }
        }

        void OnApplicationPause(bool paused)
        {
            if (paused && InWorld) SaveGame();   // mobile: the OS may kill the app in the background
        }

        void OnApplicationQuit()
        {
            if (InWorld) SaveGame();
        }
    }
}
