using System.Collections.Generic;

namespace MohallaHero
{
    public class MissionStep
    {
        public string text;
        public string var;      // story variable (or computed value such as "joined") that tracks this step
        public int need = 1;
        public string hint;     // Pintu's hint when this is the next thing to do
    }

    public class MissionDef
    {
        public int chapter;
        public string id;
        public string title;
        public string tagline;   // Hinglish one-liner shown on the chapter banner
        public string lesson;    // what the chapter teaches (journal + summary)
        public NpcId giver;
        public string startVar;
        public string doneVar;
        public MissionStep[] steps;
        public string startHint;
    }

    /// <summary>
    /// The story's six chapters. Progress lives in story variables written by the .ink files (Resources/Stories)
    /// and by world objects; this table only describes what to show. Mirrored in Docs/DESIGN_DECISIONS.md.
    /// </summary>
    public static class MissionDatabase
    {
        public static readonly List<MissionDef> All = new List<MissionDef>
        {
            new MissionDef
            {
                chapter = 1, id = "m1", title = "Bijli Bachao", tagline = "Switch off karo, paisa bachao!", giver = NpcId.Asha,
                lesson = "Saving electricity and water, sorting waste and planting trees are small habits with a big effect.",
                startVar = "m1_started", doneVar = "m1_done",
                startHint = "Asha Ma'am is outside the school, east of your building. Walk over and press E!",
                steps = new[]
                {
                    new MissionStep { text = "Switch off wasted lights & fans", var = "lights_off", need = 4, hint = "Glowing bulbs in daylight = wasted bijli. Look in front of the school, the apartments and the Ward Office." },
                    new MissionStep { text = "Close leaking taps", var = "taps_closed", need = 3, hint = "Drip drip drip... taps near the school, the park and your apartment are leaking." },
                    new MissionStep { text = "Plant saplings in Gulmohar Park", var = "saplings", need = 3, hint = "Look for the brown soil patches inside Gulmohar Park." },
                    new MissionStep { text = "Clean up garbage heaps (sort the waste)", var = "piles_cleaned", need = 2, hint = "Garbage heaps: one in the park, one at the playground. Gila = green bin, Sukha = blue bin, Khatarnak = red bin!" },
                    new MissionStep { text = "Report back to Asha Ma'am", var = "m1_done", hint = "All done? Asha Ma'am is outside the school." },
                }
            },
            new MissionDef
            {
                chapter = 2, id = "m2", title = "Bully Buster", tagline = "Dosti mein dum hai, dadagiri mein nahi!", giver = NpcId.Pintu,
                lesson = "Stop bullying safely: stay calm, get a trusted adult involved, report it and stand by the person being hurt.",
                startVar = "m2_started", doneVar = "m2_done",
                startHint = "Pintu is hanging around near the park entrance on the Main Road. He has news.",
                steps = new[]
                {
                    new MissionStep { text = "Stop the bullying in Gulmohar Park, safely", var = "m2_stopped", hint = "Golu and Bunty are in the top-left corner of the park. Keep it calm, no fighting!" },
                    new MissionStep { text = "Report it to Asha Ma'am", var = "m2_reported", hint = "Bullying is never \"just a joke\". Tell a trusted adult. Asha Ma'am is at the school." },
                    new MissionStep { text = "Support Golu", var = "m2_supported", hint = "Golu needs a friend. Find him near the school." },
                    new MissionStep { text = "Talk to Bunty", var = "m2_done", hint = "Even bullies can change. Find Bunty in the park." },
                }
            },
            new MissionDef
            {
                chapter = 3, id = "m3", title = "Imandar Bazaar", tagline = "Sachchai ka bhav kabhi nahi girta.", giver = NpcId.Sharma,
                lesson = "Honesty in small things: return extra change, ignore too-good-to-be-true offers and wait your turn in queues.",
                startVar = "m3_started", doneVar = "m3_done",
                startHint = "Sharma Aunty is outside Gulmohar Apartments, next to your door.",
                steps = new[]
                {
                    new MissionStep { text = "Buy Sharma Aunty's groceries at Lala Kirana", var = "m3_bought", hint = "The Bazaar Gali is open now. Lala ji's shop is on the left inside." },
                    new MissionStep { text = "Check out the \"Lucky Offer\" stall", var = "m3_lucky", hint = "There's a very loud stall in the middle of the bazaar. If it sounds too good to be true..." },
                    new MissionStep { text = "Collect the ration card at the Ration Office", var = "m3_ration", hint = "The Ration Office is on the right side of the bazaar. There's a queue!" },
                    new MissionStep { text = "Deliver everything to Sharma Aunty", var = "m3_done", hint = "Go back to Sharma Aunty at Gulmohar Apartments." },
                }
            },
            new MissionDef
            {
                chapter = 4, id = "m4", title = "Sach ki Awaaz", tagline = "Saboot bolte hain, afwaah nahi.", giver = NpcId.Meera,
                lesson = "Fight corruption with facts: collect evidence, use the Right to Information (RTI) and persuade people honestly.",
                startVar = "m4_started", doneVar = "m4_done",
                startHint = "Meera Didi at the Seva Clinic (east, next to the Ward Office) wants to see you.",
                steps = new[]
                {
                    new MissionStep { text = "Photograph the park's \"renovation\" board", var = "item_photo", hint = "The big board near the park's south gate says \"Renovation complete\". Does the park look renovated?" },
                    new MissionStep { text = "Find the suspicious bill (receipt)", var = "item_receipt", hint = "Lala ji supplied the park... or so the bill says. Ask him. If he won't help, check the raddi pile behind Netaji's office." },
                    new MissionStep { text = "File an RTI at the Ward Office and collect the reply", var = "item_rti", hint = "Officer Fernandes takes RTI applications. The reply takes a day: sleep at home (your door), then come back." },
                    new MissionStep { text = "Convince neighbours to join you", var = "joined", need = Balance.SupportersNeeded, hint = "Talk to Sharma Aunty, Raju, Lala ji, Chacha, Asha Ma'am and Bunty. Show evidence, don't exaggerate!" },
                    new MissionStep { text = "Tell Meera Didi you're ready", var = "m4_done", hint = "Got enough people on your side? Meera Didi is at the clinic." },
                }
            },
            new MissionDef
            {
                chapter = 5, id = "m5", title = "Shanti March", tagline = "Awaaz uthao, haath nahi.", giver = NpcId.Meera,
                lesson = "Peaceful protest works: placards, slogans, a candle march and a petition through the proper office. Violence ruins everything.",
                startVar = "m5_started", doneVar = "m5_done",
                startHint = "Meera Didi is waiting to plan the march.",
                steps = new[]
                {
                    new MissionStep { text = "Make placards at the Community Hall", var = "placards", need = Balance.PlacardsNeeded, hint = "The Community Hall is at the bottom of the map, below the party offices. Use the table outside." },
                    new MissionStep { text = "Gather everyone at Gulmohar Park", var = "m5_gathered", hint = "Talk to Meera Didi in the park to light the candles." },
                    new MissionStep { text = "Lead the candle march", var = "march_checkpoints", need = 4, hint = "Follow the glowing diyas: Main Road → Gali No. 2 → Ward Office. Keep it peaceful!" },
                    new MissionStep { text = "Submit the petition to Officer Fernandes", var = "m5_done", hint = "Officer Fernandes is waiting at the Ward Office." },
                }
            },
            new MissionDef
            {
                chapter = 6, id = "m6", title = "Election Mela", tagline = "Mera vote, meri awaaz!", giver = NpcId.Fernandes,
                lesson = "Voting is a duty: learn about candidates, check facts, refuse bribes, inform others, then vote in secret.",
                startVar = "m6_started", doneVar = "m6_done",
                startHint = "Officer Fernandes is at the Mela Maidan gate (south-east). The Election Mela has begun!",
                steps = new[]
                {
                    new MissionStep { text = "Visit all three candidates' stalls", var = "stalls_visited", need = 3, hint = "Three stalls in a row inside the Maidan: Netaji, Dr. Vaada and Meera Didi." },
                    new MissionStep { text = "Watch the nukkad natak (street play)", var = "mela_play", hint = "The stage is at the top-left of the Maidan." },
                    new MissionStep { text = "Fact-check rumours at Chacha's stall", var = "rumours_checked", hint = "Chacha has a stall near the Maidan's bottom-left. Bring your fact-checking skills!" },
                    new MissionStep { text = "Inform voters door-to-door", var = "voters_informed", need = Balance.VotersToInform, hint = "Houses with a blue \"Vote\" flag. Knock and share honest information, not who to vote for." },
                    new MissionStep { text = "Cast your vote at the polling booth", var = "m6_done", hint = "The polling booth tent is at the Maidan's bottom-right." },
                }
            },
        };

        public static int Count => All.Count;

        public static MissionDef Get(string id)
        {
            foreach (var m in All) if (m.id == id) return m;
            return null;
        }
    }
}
