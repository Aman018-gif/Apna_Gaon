using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MohallaHero.EditorTools
{
    /// <summary>
    /// First-open setup: creates Assets/Scenes/Main.unity, adds it to Build Settings and opens it.
    /// The scene is empty on purpose: the game builds itself at runtime (see GameManager.Boot).
    /// Also adds the "Mohalla Hero" menu.
    /// </summary>
    [InitializeOnLoad]
    static class MohallaHeroProjectSetup
    {
        const string ScenePath = "Assets/Scenes/Main.unity";

        static MohallaHeroProjectSetup()
        {
            EditorApplication.delayCall += () => EnsureMainScene(false);
        }

        [MenuItem("Mohalla Hero/Setup Main Scene")]
        static void SetupFromMenu() => EnsureMainScene(true);

        static void EnsureMainScene(bool forceOpen)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isBatchMode) return;

            if (!File.Exists(ScenePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
                var active = SceneManager.GetActiveScene();
                bool replace = !active.isDirty;
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, replace ? NewSceneMode.Single : NewSceneMode.Additive);
                EditorSceneManager.SaveScene(scene, ScenePath);
                if (!replace) EditorSceneManager.CloseScene(scene, true);
                AssetDatabase.Refresh();
                Debug.Log("Mohalla Hero: created " + ScenePath + ". Press Play to start the game.");
            }

            if (!EditorBuildSettings.scenes.Any(s => s.path == ScenePath))
            {
                var list = EditorBuildSettings.scenes.ToList();
                list.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
                EditorBuildSettings.scenes = list.ToArray();
            }

            var current = SceneManager.GetActiveScene();
            bool untitled = string.IsNullOrEmpty(current.path);
            if ((forceOpen || untitled) && current.path != ScenePath && !current.isDirty)
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        [MenuItem("Mohalla Hero/Delete Save File")]
        static void DeleteSave()
        {
            SaveSystem.Delete();
            Debug.Log("Mohalla Hero: save file deleted (" + SaveSystem.SavePath + ")");
        }

        [MenuItem("Mohalla Hero/Open Save Folder")]
        static void OpenSaveFolder() => EditorUtility.RevealInFinder(Application.persistentDataPath);

        /// <summary>
        /// Writes the four karma stats as ScriptableObject assets to Assets/MohallaHero/Resources/Karma, so their names,
        /// colours, descriptions and starting XP can be edited in the Inspector. The game loads them automatically.
        /// </summary>
        [MenuItem("Mohalla Hero/Create Karma Stat Assets")]
        static void CreateKarmaAssets()
        {
            const string dir = "Assets/MohallaHero/Resources/Karma";
            Directory.CreateDirectory(dir);
            var defaults = new KarmaSystem();
            foreach (var s in KarmaSystem.All)
            {
                string path = $"{dir}/{s}.asset";
                if (File.Exists(path)) continue;
                var copy = Object.Instantiate(defaults.Get(s));
                copy.name = s.ToString();
                AssetDatabase.CreateAsset(copy, path);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Mohalla Hero: karma stat assets are in " + dir + ". Edit them in the Inspector.");
        }

        /// <summary>Prints the key of every sprite drawn so far, for replacing art via Resources/ArtOverrides/&lt;key&gt;.png.</summary>
        [MenuItem("Mohalla Hero/List Art Keys (use in Play Mode)")]
        static void ListArtKeys()
        {
            var keys = SpriteFactory.Keys.OrderBy(k => k).ToList();
            if (keys.Count == 0) { Debug.Log("Mohalla Hero: press Play and walk around first; sprites are drawn on demand."); return; }
            Debug.Log($"Mohalla Hero: {keys.Count} art keys (put Resources/ArtOverrides/<key>.png to replace one):\n" + string.Join("\n", keys));
        }

        [MenuItem("Mohalla Hero/Validate Story (Ink)")]
        static void ValidateStory()
        {
            StoryLibrary.ClearCache();
            try
            {
                var story = StoryLibrary.Story;
                var problems = story.Validate();
                if (problems.Count == 0) Debug.Log($"Mohalla Hero: story OK. {story.Knots.Count} knots, {story.VarDefaults.Count} variables.");
                else foreach (var p in problems) Debug.LogError("Mohalla Hero story: " + p);
            }
            catch (System.Exception e) { Debug.LogError("Mohalla Hero story: " + e.Message); }
        }
    }
}
