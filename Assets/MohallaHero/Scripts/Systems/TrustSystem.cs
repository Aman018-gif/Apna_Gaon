using System;
using UnityEngine;

namespace MohallaHero
{
    /// <summary>
    /// How much each resident trusts the player (0–100). Stored as story variables <c>trust_&lt;id&gt;</c>, so the .ink files
    /// can read them directly and saves pick them up for free. Honest arguments raise trust, lies and pressure lower it.
    /// A resident with trust ≥ <see cref="Balance.TrustJoinThreshold"/> joins the peaceful protest.
    /// </summary>
    public class TrustSystem
    {
        /// <summary>The residents who can be convinced to join the march (Chapter 4).</summary>
        public static readonly NpcId[] Convincible = { NpcId.Sharma, NpcId.Raju, NpcId.Lala, NpcId.Chacha, NpcId.Asha, NpcId.Bunty };

        readonly StoryState state;

        public event Action<NpcId, int> Changed;

        public TrustSystem(StoryState state)
        {
            this.state = state;
            foreach (var d in NpcDatabase.All) state.SetDefault(Var(d.id), d.initialTrust);
        }

        public static string Var(NpcId id) => "trust_" + id.ToString().ToLowerInvariant();

        public int Get(NpcId id) => state.Get(Var(id));

        public int Add(NpcId id, int delta)
        {
            int v = Mathf.Clamp(Get(id) + delta, 0, 100);
            state.Set(Var(id), v);
            Changed?.Invoke(id, v);
            return v;
        }

        public bool HasJoined(NpcId id) => Get(id) >= Balance.TrustJoinThreshold;

        public int JoinedCount
        {
            get
            {
                int n = 0;
                foreach (var id in Convincible) if (HasJoined(id)) n++;
                return n;
            }
        }

        public static string Label(int trust)
        {
            if (trust >= 80) return "Pakka saath";
            if (trust >= Balance.TrustJoinThreshold) return "Saath hai";
            if (trust >= 40) return "Soch raha hai";
            if (trust >= 20) return "Shak hai";
            return "Bharosa nahi";
        }
    }
}
