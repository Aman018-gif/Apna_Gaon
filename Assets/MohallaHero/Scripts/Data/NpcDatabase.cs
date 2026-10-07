using System.Collections.Generic;
using UnityEngine;

namespace MohallaHero
{
    public enum Outfit { Plain, Kurta, NetaJacket, Suit, Saree, Uniform, Coat, Cap }

    public class NpcDef
    {
        public NpcId id;
        public string name;           // display name
        public string speaker;        // name used in the .ink files ("Asha Ma'am: ...")
        public string role;
        public string bio;
        public bool female;
        public bool child;
        public Outfit outfit;
        public Color skin, shirt, pants, hair, accent;
        public Vector2Int workCell;
        public int workRadius = 2;
        public int workStartHour = 8;
        public int workEndHour = 19;
        public int sleepHour = 21;
        public int initialTrust = 30;
        public string macVoice = "Rishi";
        public int voiceRate = 175;

        public string Key => id.ToString().ToLowerInvariant();
        public Vector2Int Door => WorldLayout.HomeOf(id).Door;
    }

    /// <summary>The 12 fictional residents of Shanti Nagar (no real people or politicians).</summary>
    public static class NpcDatabase
    {
        public static readonly List<NpcDef> All = new List<NpcDef>();
        static readonly Dictionary<NpcId, NpcDef> byId = new Dictionary<NpcId, NpcDef>();

        static readonly Color SkinA = new Color(0.82f, 0.62f, 0.47f);
        static readonly Color SkinB = new Color(0.70f, 0.50f, 0.36f);
        static readonly Color SkinC = new Color(0.58f, 0.40f, 0.28f);
        static readonly Color Black = new Color(0.1f, 0.08f, 0.07f);
        static readonly Color Grey = new Color(0.78f, 0.78f, 0.78f);

        static NpcDatabase()
        {
            Add(new NpcDef
            {
                id = NpcId.Pintu, name = "Pintu", speaker = "Pintu", role = "Meme Kid", child = true, outfit = Outfit.Cap,
                bio = "Eleven years old, two phones' worth of memes in his head. Knows every shortcut in the mohalla and gives the best hints.",
                skin = SkinB, shirt = Hex("#ffcc00"), pants = Hex("#2d6cdf"), hair = Black, accent = Hex("#e5157a"),
                workCell = new Vector2Int(58, 31), workRadius = 4, workStartHour = 8, workEndHour = 19, sleepHour = 20, macVoice = "Aman", voiceRate = 205
            });
            Add(new NpcDef
            {
                id = NpcId.Chacha, name = "Chacha Gossip", speaker = "Chacha", role = "Tea Stall Owner", outfit = Outfit.Kurta,
                bio = "Makes the best cutting chai in town and forwards every WhatsApp message before reading it.",
                skin = SkinA, shirt = Hex("#f2efe4"), pants = Hex("#7a5a3a"), hair = Grey, accent = Hex("#8a5a2b"),
                workCell = new Vector2Int(9, 19), workRadius = 1, workStartHour = 7, workEndHour = 21, sleepHour = 22, initialTrust = 20, macVoice = "Rishi", voiceRate = 190
            });
            Add(new NpcDef
            {
                id = NpcId.Jugaad, name = "Netaji Jugaad Singh", speaker = "Netaji", role = "Local Neta (fictional)", outfit = Outfit.NetaJacket,
                bio = "Promises free WiFi on the Moon, a helipad on every roof and a flyover over the park pond. Has never filled a pothole.",
                skin = SkinA, shirt = Hex("#fbfbf5"), pants = Hex("#fbfbf5"), hair = Black, accent = Hex("#6a2c91"),
                workCell = new Vector2Int(55, 17), workRadius = 2, workStartHour = 10, workEndHour = 19, initialTrust = 5, macVoice = "Rishi", voiceRate = 168
            });
            Add(new NpcDef
            {
                id = NpcId.Vaada, name = "Dr. Vaada Verma", speaker = "Dr. Vaada", role = "Rival Neta (fictional)", outfit = Outfit.Suit,
                bio = "Agrees with whoever spoke last. His manifesto has two versions: one for mornings, one for evenings.",
                skin = SkinB, shirt = Hex("#2b9bb3"), pants = Hex("#1e3d59"), hair = Hex("#3b2a1a"), accent = Hex("#ffd23f"),
                workCell = new Vector2Int(66, 17), workRadius = 2, workStartHour = 9, workEndHour = 19, initialTrust = 15, macVoice = "Aman", voiceRate = 185
            });
            Add(new NpcDef
            {
                id = NpcId.Asha, name = "Asha Ma'am", speaker = "Asha Ma'am", role = "School Teacher", female = true, outfit = Outfit.Saree,
                bio = "Teaches science at Shanti Nagar Public School. Believes every child can change a mohalla.",
                skin = SkinA, shirt = Hex("#2e8b57"), pants = Hex("#2e8b57"), hair = Black, accent = Hex("#ffc20e"),
                workCell = new Vector2Int(33, 35), workRadius = 2, workStartHour = 8, workEndHour = 18, initialTrust = 50, macVoice = "Tara", voiceRate = 178
            });
            Add(new NpcDef
            {
                id = NpcId.Lala, name = "Lala Kishorilal", speaker = "Lala ji", role = "Kirana Shopkeeper", outfit = Outfit.Kurta,
                bio = "Runs the oldest grocery store in the bazaar. Counts change twice, trusts people once.",
                skin = SkinB, shirt = Hex("#f4a261"), pants = Hex("#f2efe4"), hair = Grey, accent = Hex("#e76f51"),
                workCell = new Vector2Int(28, 18), workRadius = 1, workStartHour = 8, workEndHour = 20, initialTrust = 30, macVoice = "Rishi", voiceRate = 160
            });
            Add(new NpcDef
            {
                id = NpcId.Sharma, name = "Sharma Aunty", speaker = "Sharma Aunty", role = "Your Neighbour", female = true, outfit = Outfit.Saree,
                bio = "Lives one floor below you. Knows everyone's business and runs the building's WhatsApp group.",
                skin = SkinA, shirt = Hex("#e5157a"), pants = Hex("#e5157a"), hair = Hex("#2b2020"), accent = Hex("#ffc20e"),
                workCell = new Vector2Int(13, 34), workRadius = 2, workStartHour = 8, workEndHour = 19, initialTrust = 35, macVoice = "Lekha", voiceRate = 168
            });
            Add(new NpcDef
            {
                id = NpcId.Raju, name = "Raju Rickshawala", speaker = "Raju", role = "Auto-rickshaw Driver", outfit = Outfit.Uniform,
                bio = "Drives the brightest auto in town. Hears every rumour in the city, believes about half.",
                skin = SkinC, shirt = Hex("#c8a24a"), pants = Hex("#3d3d4f"), hair = Black, accent = Hex("#00a6a6"),
                workCell = new Vector2Int(15, 25), workRadius = 2, workStartHour = 7, workEndHour = 20, initialTrust = 40, macVoice = "Aman", voiceRate = 190
            });
            Add(new NpcDef
            {
                id = NpcId.Golu, name = "Golu", speaker = "Golu", role = "Shy Student", child = true, outfit = Outfit.Uniform,
                bio = "Loves drawing comics. Has been eating lunch alone since Bunty started picking on him.",
                skin = SkinA, shirt = Hex("#f2f2f2"), pants = Hex("#264653"), hair = Black, accent = Hex("#e63946"),
                workCell = new Vector2Int(36, 34), workRadius = 2, workStartHour = 9, workEndHour = 18, sleepHour = 20, initialTrust = 40, macVoice = "Aman", voiceRate = 200
            });
            Add(new NpcDef
            {
                id = NpcId.Bunty, name = "Bunty", speaker = "Bunty", role = "Class Bully", child = true, outfit = Outfit.Cap,
                bio = "Loud, bored and, though he won't admit it, lonely.",
                skin = SkinB, shirt = Hex("#d62828"), pants = Hex("#2b2d42"), hair = Black, accent = Hex("#111111"),
                workCell = new Vector2Int(56, 43), workRadius = 3, workStartHour = 9, workEndHour = 18, sleepHour = 20, initialTrust = 10, macVoice = "Rishi", voiceRate = 205
            });
            Add(new NpcDef
            {
                id = NpcId.Fernandes, name = "Officer Fernandes", speaker = "Officer Fernandes", role = "Ward & Election Officer", female = true, outfit = Outfit.Coat,
                bio = "Runs the Ward Office by the book. Will accept any complaint, as long as the form is filled correctly.",
                skin = SkinB, shirt = Hex("#1d3557"), pants = Hex("#1d3557"), hair = Hex("#3b2a1a"), accent = Hex("#f1faee"),
                workCell = new Vector2Int(81, 36), workRadius = 1, workStartHour = 9, workEndHour = 18, initialTrust = 50, macVoice = "Tara", voiceRate = 172
            });
            Add(new NpcDef
            {
                id = NpcId.Meera, name = "Meera Didi", speaker = "Meera Didi", role = "Nurse, Seva Clinic", female = true, outfit = Outfit.Coat,
                bio = "Runs the free clinic. Quietly fixed the mohalla's water problem last year with a petition and a lot of patience.",
                skin = SkinC, shirt = Hex("#f7f7f7"), pants = Hex("#00a6a6"), hair = Black, accent = Hex("#00a6a6"),
                workCell = new Vector2Int(91, 36), workRadius = 1, workStartHour = 8, workEndHour = 19, initialTrust = 60, macVoice = "Tara", voiceRate = 182
            });
        }

        static Color Hex(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }

        static void Add(NpcDef d) { All.Add(d); byId[d.id] = d; }

        public static NpcDef Get(NpcId id) => byId[id];
        public static string Name(NpcId id) => byId[id].name;

        public static string FirstName(NpcId id)
        {
            switch (id)
            {
                case NpcId.Jugaad: return "Netaji";
                case NpcId.Vaada: return "Dr. Vaada";
                case NpcId.Chacha: return "Chacha";
                case NpcId.Fernandes: return "Officer Fernandes";
                case NpcId.Asha: return "Asha Ma'am";
                case NpcId.Lala: return "Lala ji";
                case NpcId.Meera: return "Meera Didi";
                case NpcId.Sharma: return "Sharma Aunty";
                case NpcId.Raju: return "Raju";
                default: return byId[id].name;
            }
        }

        /// <summary>Resolves an id written in a story (<c>"raju"</c>) or a speaker name (<c>"Lala ji"</c>).</summary>
        public static bool TryFind(string key, out NpcDef def)
        {
            def = null;
            if (string.IsNullOrEmpty(key)) return false;
            string k = key.Trim().ToLowerInvariant();
            foreach (var d in All)
                if (d.Key == k || d.speaker.ToLowerInvariant() == k || d.name.ToLowerInvariant() == k) { def = d; return true; }
            return false;
        }
    }
}
