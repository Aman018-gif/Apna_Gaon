using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MohallaHero
{
    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public string savedAt;

        // Player
        public string playerName = "Hero";
        public int gender;
        public int money = Balance.StartingMoney;
        public float posX, posY;
        public int[] karma;
        public int karmaGained, karmaLost;

        // World & story
        public int day = 1;
        public float minute = Balance.DayStartMinute;
        public List<int> unlockedAreas = new List<int>();
        public List<VarEntry> vars = new List<VarEntry>();
        public int lastChapterAnnounced;

        // Finale
        public bool gameOver;
        public int ending;
        public int winner;
        public int myVote = -1;
        public int finalRating;
    }

    /// <summary>
    /// One JSON save file in Application.persistentDataPath (works on desktop and mobile).
    /// Story state is just the list of story variables that differ from their defaults.
    /// </summary>
    public static class SaveSystem
    {
        const string FileName = "mohallahero_save.json";

        /// <summary>Tests point this at a temporary file so they never touch the player's real save.</summary>
        public static string PathOverride;
        public static string SavePath => PathOverride ?? Path.Combine(Application.persistentDataPath, FileName);

        public static bool HasSave() => File.Exists(SavePath);

        public static void Save(SaveData data)
        {
            data.savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            var json = JsonUtility.ToJson(data, true);
            var tmp = SavePath + ".tmp";
            File.WriteAllText(tmp, json);
            if (File.Exists(SavePath)) File.Delete(SavePath);
            File.Move(tmp, SavePath);
        }

        public static SaveData Load()
        {
            try
            {
                if (!HasSave()) return null;
                return JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
            }
            catch (Exception e)
            {
                Debug.LogWarning("Mohalla Hero: could not read save file: " + e.Message);
                return null;
            }
        }

        public static void Delete()
        {
            if (HasSave()) File.Delete(SavePath);
        }
    }

    /// <summary>Player preferences (PlayerPrefs): voices, touch controls, subtitles, text size, auto-run.</summary>
    public class Settings
    {
        const string TouchKey = "mohallahero_touch", SubsKey = "mohallahero_subtitles", RunKey = "mohallahero_autorun", TextKey = "mohallahero_bigtext";

        /// <summary>0 = automatic (on for touch screens), 1 = always on, 2 = off.</summary>
        public int TouchMode { get => PlayerPrefs.GetInt(TouchKey, 0); set { PlayerPrefs.SetInt(TouchKey, value); PlayerPrefs.Save(); } }
        public bool Subtitles { get => PlayerPrefs.GetInt(SubsKey, 1) == 1; set { PlayerPrefs.SetInt(SubsKey, value ? 1 : 0); PlayerPrefs.Save(); } }
        public bool AlwaysRun { get => PlayerPrefs.GetInt(RunKey, 0) == 1; set { PlayerPrefs.SetInt(RunKey, value ? 1 : 0); PlayerPrefs.Save(); } }
        public bool BigText { get => PlayerPrefs.GetInt(TextKey, 0) == 1; set { PlayerPrefs.SetInt(TextKey, value ? 1 : 0); PlayerPrefs.Save(); } }

        /// <summary>Runtime override that is not saved (used by automated tests and screenshots).</summary>
        public static bool? TouchOverride;

        public bool TouchControlsVisible => TouchOverride ?? (TouchMode == 1 || (TouchMode == 0 && (Application.isMobilePlatform || GameInput.TouchAvailable)));

        public static string TouchLabel(int mode) => mode == 0 ? "Auto" : mode == 1 ? "On" : "Off";
    }
}
