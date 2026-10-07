using UnityEngine;

namespace MohallaHero
{
    /// <summary>Every tunable number in one place (see Docs/DESIGN_DECISIONS.md for the reasoning).</summary>
    public static class Balance
    {
        // ---- movement
        public const float WalkSpeed = 4.4f;
        public const float RunSpeed = 6.8f;
        public const float NpcSpeed = 2.3f;

        // ---- time: a day runs 07:00 → 23:00 (16 game hours) in about 9.6 real minutes
        public const float DayStartMinute = 7 * 60;
        public const float DayEndMinute = 23 * 60;
        public const float RealSecondsPerGameMinute = 0.6f;
        public const float EveningHour = 18f;

        // ---- karma (XP per stat; levels are cosmetic ranks, the raw XP feeds the Mohalla Rating)
        public const int KarmaStart = 10;
        public const int KarmaXpPerLevel = 40;
        public const int KarmaMaxLevel = 5;
        /// <summary>
        /// XP that counts as a "full" stat for the Mohalla Rating, roughly what a near-perfect run earns in that stat
        /// (the story offers many more Civic choices than Green or Honesty ones, so the caps differ).
        /// </summary>
        public static int RatingCap(KarmaStat s)
        {
            switch (s)
            {
                case KarmaStat.Civic: return 150;
                case KarmaStat.Green: return 75;
                case KarmaStat.Courage: return 90;
                default: return 75;
            }
        }

        // ---- trust (0..100 per person)
        public const int TrustJoinThreshold = 60;
        public const int SupportersNeeded = 4;

        // ---- election
        public const int VotersToInform = 5;
        public const int VoterDoors = 10;

        // ---- Mohalla Rating weights (sum = 100)
        public const float RatingKarmaWeight = 70f;      // the four stats, averaged
        public const float RatingSupportersWeight = 10f; // people who joined the march
        public const float RatingVotersWeight = 20f;     // voters informed during the Mela
        public const int ViolencePenalty = 25;           // per act of violence/vandalism

        // ---- mini-games
        public const int WastePerRound = 8;
        public const int FakeNewsPerRound = 6;
        public const int PlacardsNeeded = 3;

        // ---- money (only for the bazaar story)
        public const int StartingMoney = 200;

        public static int KarmaLevel(int xp) => Mathf.Clamp(1 + Mathf.Max(0, xp) / KarmaXpPerLevel, 1, KarmaMaxLevel);
        public static float KarmaLevelProgress(int xp) =>
            KarmaLevel(xp) >= KarmaMaxLevel ? 1f : (Mathf.Max(0, xp) % KarmaXpPerLevel) / (float)KarmaXpPerLevel;
    }

    public static class Format
    {
        public static string Money(int amount) => "₹" + amount.ToString("N0");

        public static string Clock(float minuteOfDay)
        {
            int m = Mathf.FloorToInt(minuteOfDay);
            int h = (m / 60) % 24;
            int mm = m % 60;
            string ampm = h < 12 ? "AM" : "PM";
            int h12 = h % 12 == 0 ? 12 : h % 12;
            return $"{h12}:{mm:00} {ampm}";
        }

        public static string Area(AreaId a)
        {
            switch (a)
            {
                case AreaId.Bazaar: return "Bazaar Gali";
                case AreaId.Maidan: return "Mela Maidan";
                default: return "Shanti Nagar";
            }
        }

        public static string Stat(KarmaStat s)
        {
            switch (s)
            {
                case KarmaStat.Civic: return "Civic Sense";
                case KarmaStat.Green: return "Green Score";
                case KarmaStat.Courage: return "Courage";
                default: return "Honesty";
            }
        }

        public static string StatHindi(KarmaStat s)
        {
            switch (s)
            {
                case KarmaStat.Civic: return "Nagrik Samajh";
                case KarmaStat.Green: return "Hariyali";
                case KarmaStat.Courage: return "Himmat";
                default: return "Imaandari";
            }
        }

        public static string Signed(int v) => v > 0 ? "+" + v : v.ToString();
    }
}
