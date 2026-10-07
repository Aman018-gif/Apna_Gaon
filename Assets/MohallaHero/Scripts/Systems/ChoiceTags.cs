using System.Collections.Generic;
using System.Globalization;

namespace MohallaHero
{
    /// <summary>
    /// Reads and applies the tags on a story choice: <c>#civic:+5</c>, <c>#green:-3</c>, <c>#courage:+2</c>,
    /// <c>#honesty:+10</c>, <c>#trust:+15</c> (the person you're talking to), <c>#trust_raju:+10</c>,
    /// <c>#why:text</c> and <c>#violence</c>. Pure logic shared by the game and the tests.
    /// </summary>
    public static class ChoiceTags
    {
        public class Result
        {
            public readonly List<(KarmaStat stat, int delta)> Karma = new List<(KarmaStat, int)>();
            public readonly List<(NpcId id, int delta)> Trust = new List<(NpcId, int)>();
            public string Why;
            public bool Violence;

            /// <summary>A rough "how good is this choice" score (used by tests to play the best and worst paths).</summary>
            public float Score
            {
                get
                {
                    float s = 0;
                    foreach (var k in Karma) s += k.delta;
                    foreach (var t in Trust) s += t.delta * 0.5f;
                    if (Violence) s -= 100;
                    return s;
                }
            }
        }

        public static Result Parse(IEnumerable<string> tags, NpcId? speaker)
        {
            var r = new Result();
            foreach (var tag in tags)
            {
                string key = tag, value = null;
                int colon = tag.IndexOf(':');
                if (colon >= 0) { key = tag.Substring(0, colon).Trim(); value = tag.Substring(colon + 1).Trim(); }
                key = key.ToLowerInvariant();
                if (key == "why") { r.Why = value; continue; }
                if (key == "violence") { r.Violence = true; continue; }
                if (!int.TryParse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int delta)) continue;
                if (KarmaSystem.TryParse(key, out var stat)) r.Karma.Add((stat, delta));
                else if (key == "trust" && speaker.HasValue) r.Trust.Add((speaker.Value, delta));
                else if (key.StartsWith("trust_") && NpcDatabase.TryFind(key.Substring(6), out var other)) r.Trust.Add((other.id, delta));
            }
            return r;
        }

        public static void Apply(Result r, KarmaSystem karma, TrustSystem trust, StoryState story)
        {
            foreach (var (stat, delta) in r.Karma) karma.Add(stat, delta);
            foreach (var (id, delta) in r.Trust) trust.Add(id, delta);
            if (r.Violence) story.Add("violence", 1);
        }
    }
}
