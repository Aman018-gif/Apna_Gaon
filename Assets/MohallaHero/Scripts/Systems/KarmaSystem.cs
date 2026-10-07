using System;
using System.Collections.Generic;
using UnityEngine;

namespace MohallaHero
{
    /// <summary>
    /// The four karma stats (Civic Sense, Green Score, Courage, Honesty). Choices add or remove XP; the XP never drops
    /// below zero. Pure gameplay logic apart from the ScriptableObject containers, so it runs in EditMode tests.
    /// </summary>
    public class KarmaSystem
    {
        readonly Dictionary<KarmaStat, KarmaStatAsset> stats = new Dictionary<KarmaStat, KarmaStatAsset>();
        public static readonly KarmaStat[] All = { KarmaStat.Civic, KarmaStat.Green, KarmaStat.Courage, KarmaStat.Honesty };

        /// <summary>Raised with the stat and the delta actually applied.</summary>
        public event Action<KarmaStat, int> Changed;

        /// <summary>Total XP lost through bad choices, for the summary screen.</summary>
        public int TotalLost { get; private set; }
        public int TotalGained { get; private set; }

        public KarmaSystem()
        {
            foreach (var s in All)
            {
                KarmaStatAsset asset = null;
                var authored = Resources.Load<KarmaStatAsset>("Karma/" + s);
                asset = authored != null ? UnityEngine.Object.Instantiate(authored) : CreateDefault(s);
                asset.stat = s;
                asset.xp = asset.startXp;
                stats[s] = asset;
            }
        }

        static KarmaStatAsset CreateDefault(KarmaStat s)
        {
            var a = ScriptableObject.CreateInstance<KarmaStatAsset>();
            a.name = s.ToString();
            a.stat = s;
            a.displayName = Format.Stat(s);
            a.hindiName = Format.StatHindi(s);
            a.color = Theme.StatColor(s);
            a.startXp = Balance.KarmaStart;
            switch (s)
            {
                case KarmaStat.Civic: a.description = "Caring for shared spaces and rules that help everyone: queues, public property, reporting problems."; break;
                case KarmaStat.Green: a.description = "Saving electricity and water, sorting waste and planting trees."; break;
                case KarmaStat.Courage: a.description = "Standing up safely for others and for the truth, without violence."; break;
                default: a.description = "Telling the truth, returning what isn't yours and refusing bribes and shortcuts."; break;
            }
            return a;
        }

        public KarmaStatAsset Get(KarmaStat s) => stats[s];
        public int Xp(KarmaStat s) => stats[s].xp;
        public int Level(KarmaStat s) => stats[s].Level;

        /// <summary>0..1 share of the stat's rating cap.</summary>
        public float Normalized(KarmaStat s) => Mathf.Clamp01(stats[s].xp / (float)Balance.RatingCap(s));

        public int Add(KarmaStat s, int delta)
        {
            if (delta == 0) return 0;
            var a = stats[s];
            int before = a.xp;
            a.xp = Mathf.Max(0, a.xp + delta);
            int applied = a.xp - before;
            if (delta > 0) TotalGained += delta; else TotalLost += -delta;
            Changed?.Invoke(s, delta);
            return applied;
        }

        public static bool TryParse(string name, out KarmaStat stat)
        {
            switch ((name ?? "").Trim().ToLowerInvariant())
            {
                case "civic": stat = KarmaStat.Civic; return true;
                case "green": stat = KarmaStat.Green; return true;
                case "courage": stat = KarmaStat.Courage; return true;
                case "honesty": stat = KarmaStat.Honesty; return true;
            }
            stat = KarmaStat.Civic;
            return false;
        }

        public static string RankName(KarmaStat s, int level)
        {
            string[] names;
            switch (s)
            {
                case KarmaStat.Civic: names = new[] { "Naya Padosi", "Achha Padosi", "Zimmedar Nagrik", "Mohalla Mentor", "Civic Superstar" }; break;
                case KarmaStat.Green: names = new[] { "Plastic Prem", "Switch-Off Shishya", "Paudha Pyaara", "Green Guardian", "Prithvi ka Dost" }; break;
                case KarmaStat.Courage: names = new[] { "Darpok Dost", "Thoda Bahadur", "Himmatwala", "Sach ka Sipahi", "Mohalla Sher" }; break;
                default: names = new[] { "Jugaadu", "Seedha-Saadha", "Imaandar", "Sachcha Insaan", "Raja Harishchandra 2.0" }; break;
            }
            return names[Mathf.Clamp(level, 1, names.Length) - 1];
        }

        public int[] Snapshot()
        {
            var arr = new int[All.Length];
            for (int i = 0; i < All.Length; i++) arr[i] = stats[All[i]].xp;
            return arr;
        }

        public void Load(int[] xp, int gained, int lost)
        {
            if (xp != null)
                for (int i = 0; i < All.Length && i < xp.Length; i++) stats[All[i]].xp = Mathf.Max(0, xp[i]);
            TotalGained = gained;
            TotalLost = lost;
        }
    }
}
