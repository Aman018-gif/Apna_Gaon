using System;

namespace MohallaHero
{
    /// <summary>
    /// World-scoped gameplay events. Subscribers are cleared whenever a session is torn down,
    /// so only objects that live inside the world (or re-subscribe on build) should use these.
    /// </summary>
    public static class GameEvents
    {
        public static event Action<int> DayStarted;
        public static event Action<int> HourChanged;
        public static event Action<KarmaStat, int> KarmaChanged;     // stat, delta
        public static event Action<string, int> VarChanged;           // story variable, new value
        public static event Action<NpcId, int> TrustChanged;          // person, new value
        public static event Action<AreaId> AreaUnlocked;
        public static event Action<int> ChapterStarted;
        public static event Action RatingChanged;

        public static void RaiseDayStarted(int day) => DayStarted?.Invoke(day);
        public static void RaiseHourChanged(int hour) => HourChanged?.Invoke(hour);
        public static void RaiseKarmaChanged(KarmaStat s, int delta) => KarmaChanged?.Invoke(s, delta);
        public static void RaiseVarChanged(string name, int value) => VarChanged?.Invoke(name, value);
        public static void RaiseTrustChanged(NpcId n, int value) => TrustChanged?.Invoke(n, value);
        public static void RaiseAreaUnlocked(AreaId a) => AreaUnlocked?.Invoke(a);
        public static void RaiseChapterStarted(int c) => ChapterStarted?.Invoke(c);
        public static void RaiseRatingChanged() => RatingChanged?.Invoke();

        public static void ResetAll()
        {
            DayStarted = null; HourChanged = null; KarmaChanged = null; VarChanged = null;
            TrustChanged = null; AreaUnlocked = null; ChapterStarted = null; RatingChanged = null;
        }
    }
}
