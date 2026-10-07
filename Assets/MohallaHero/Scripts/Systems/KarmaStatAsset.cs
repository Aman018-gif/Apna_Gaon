using System;
using UnityEngine;

namespace MohallaHero
{
    /// <summary>
    /// One karma stat as a ScriptableObject: its look and text, plus the player's current XP.
    /// Designers can create assets with Create → Mohalla Hero → Karma Stat and put them in
    /// <c>Resources/Karma/&lt;Stat&gt;.asset</c> to override the built-in defaults; the game always works on a runtime copy,
    /// so playing never modifies the asset on disk.
    /// </summary>
    [CreateAssetMenu(menuName = "Mohalla Hero/Karma Stat", fileName = "Civic")]
    public class KarmaStatAsset : ScriptableObject
    {
        public KarmaStat stat;
        public string displayName = "Civic Sense";
        public string hindiName = "Nagrik Samajh";
        [TextArea] public string description = "";
        public Color color = Color.white;
        [Tooltip("Starting XP for a new game")] public int startXp = Balance.KarmaStart;

        [NonSerialized] public int xp;

        public int Level => Balance.KarmaLevel(xp);
        public float LevelProgress => Balance.KarmaLevelProgress(xp);
        public string Rank => KarmaSystem.RankName(stat, Level);
    }
}
