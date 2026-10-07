using System;

namespace MohallaHero
{
    /// <summary>
    /// Linear story progress. A chapter is complete when its <c>doneVar</c> is set (by the .ink story);
    /// steps are read from story variables, so the tracker is always in sync with what actually happened.
    /// Pure logic: <paramref name="value"/> resolves variable names (and computed ones like "joined").
    /// </summary>
    public class MissionSystem
    {
        readonly Func<string, int> value;

        public MissionSystem(Func<string, int> value) { this.value = value; }

        /// <summary>1..6, or 7 once the election is over.</summary>
        public int CurrentChapter
        {
            get
            {
                foreach (var m in MissionDatabase.All)
                    if (value(m.doneVar) == 0) return m.chapter;
                return MissionDatabase.Count + 1;
            }
        }

        public bool AllDone => CurrentChapter > MissionDatabase.Count;
        public MissionDef Current => AllDone ? null : MissionDatabase.All[CurrentChapter - 1];

        public bool IsStarted(MissionDef m) => value(m.startVar) != 0;
        public bool IsDone(MissionDef m) => value(m.doneVar) != 0;
        public int Progress(MissionStep s) => Math.Min(value(s.var), s.need);
        public bool IsDone(MissionStep s) => value(s.var) >= s.need;

        public MissionStep NextStep(MissionDef m)
        {
            if (m == null) return null;
            foreach (var s in m.steps) if (!IsDone(s)) return s;
            return null;
        }

        public string StepLabel(MissionStep s) => s.need > 1 ? $"{s.text} ({Progress(s)}/{s.need})" : s.text;

        public string TrackerText()
        {
            var m = Current;
            if (m == null) return "Shanti Nagar ka faisla ho gaya! Check your ending in the phone.";
            if (!IsStarted(m)) return $"Talk to {NpcDatabase.FirstName(m.giver)} to begin";
            var s = NextStep(m);
            return s != null ? StepLabel(s) : $"Talk to {NpcDatabase.FirstName(m.giver)}";
        }

        public string Hint()
        {
            var m = Current;
            if (m == null) return "Game khatam, par civic sense kabhi khatam nahi hota! New game se doosra ending try karo.";
            if (!IsStarted(m)) return m.startHint;
            var s = NextStep(m);
            return s != null ? s.hint : "Sab ho gaya? Talk to the person who gave you the mission.";
        }
    }
}
