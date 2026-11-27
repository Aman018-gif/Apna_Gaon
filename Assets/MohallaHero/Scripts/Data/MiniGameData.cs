using System;
using System.Collections.Generic;

namespace MohallaHero
{
    // ------------------------------------------------------------------------------------------ waste sorting

    public enum WasteBin { Wet, Dry, Hazardous }

    public class WasteItem
    {
        public string name;
        public WasteBin bin;
        public string why;
        public string icon;   // SpriteFactory.WasteIcon key
        public WasteItem(string name, WasteBin bin, string icon, string why) { this.name = name; this.bin = bin; this.icon = icon; this.why = why; }
    }

    // ------------------------------------------------------------------------------------------ fake news

    public class NewsItem
    {
        public string sender;      // who forwarded it
        public string headline;
        public bool isFake;
        public string clue;        // revealed by "Check source"
        public string explanation; // shown after answering
        public NewsItem(string sender, string headline, bool isFake, string clue, string explanation)
        {
            this.sender = sender; this.headline = headline; this.isFake = isFake; this.clue = clue; this.explanation = explanation;
        }
    }

    // ------------------------------------------------------------------------------------------ slogans

    public enum SloganTone { Peaceful, Funny, Angry, Violent }

    public class SloganPart
    {
        public string text;
        public SloganTone tone;
        public SloganPart(string text, SloganTone tone) { this.text = text; this.tone = tone; }
    }

    public struct SloganResult
    {
        public bool accepted;
        public int civic, courage;
        public string feedback;
    }

    /// <summary>Content and scoring for the three mini-games (all pure, unit-tested).</summary>
    public static class MiniGameData
    {
        public static readonly WasteItem[] Waste =
        {
            new WasteItem("Banana peel", WasteBin.Wet, "peel", "Kitchen and food waste is wet waste. It becomes compost for the park!"),
            new WasteItem("Leftover dal-chawal", WasteBin.Wet, "food", "Leftover food is wet waste. Better still: cook only what you can eat."),
            new WasteItem("Used chai patti", WasteBin.Wet, "tea", "Tea leaves are wet waste and excellent compost for plants."),
            new WasteItem("Wilted marigolds", WasteBin.Wet, "flower", "Flowers rot naturally, so they are wet waste. Never throw them in the pond."),
            new WasteItem("Plastic bottle", WasteBin.Dry, "bottle", "Clean plastic is dry waste and can be recycled. Crush it to save space."),
            new WasteItem("Old newspaper", WasteBin.Dry, "paper", "Paper is dry waste. The raddiwala even pays for it!"),
            new WasteItem("Chips packet", WasteBin.Dry, "chips", "Multi-layer packets are dry waste. Keep them away from drains."),
            new WasteItem("Cardboard box", WasteBin.Dry, "box", "Flattened cardboard is dry, recyclable waste."),
            new WasteItem("Glass jar", WasteBin.Dry, "jar", "Clean glass is dry waste and can be recycled again and again."),
            new WasteItem("Used AA battery", WasteBin.Hazardous, "battery", "Batteries leak toxic chemicals. They go to the hazardous bin or an e-waste centre."),
            new WasteItem("Broken CFL bulb", WasteBin.Hazardous, "bulb", "CFL bulbs contain mercury: hazardous! Wrap them in paper before disposal."),
            new WasteItem("Expired medicines", WasteBin.Hazardous, "medicine", "Old medicines can poison water and animals, so they are hazardous. Clinics can collect them."),
            new WasteItem("Old phone charger", WasteBin.Hazardous, "charger", "Electronics are e-waste: hazardous, but valuable when recycled properly."),
            new WasteItem("Paint can", WasteBin.Hazardous, "paint", "Leftover paint is chemical waste: hazardous bin only."),
        };

        public static readonly NewsItem[] News =
        {
            new NewsItem("Chacha's group: \"Sacchi Khabar 24x7\"",
                "BREAKING!! Election cancelled. This year vote by SMS: send VOTE <name> to 5555. Forward to 10 groups!!",
                true, "No official source, no date, 3 spelling mistakes, and it asks you to forward it.",
                "Voting happens only at the polling booth with your ID. No SMS voting exists. Real notices come from the Election Officer."),
            new NewsItem("Ward Office notice board",
                "Polling for Ward 7 on Sunday, 7 AM to 6 PM, at the Mela Maidan booth. Bring your voter ID or another approved photo ID.",
                false, "Signed by Officer Fernandes, with an office stamp and a reference number.",
                "Official notice, official signature: real. When in doubt, check the notice board or the official website."),
            new NewsItem("Uncle No. 3",
                "NASA approves Netaji Jugaad Singh's WiFi tower on the Moon!!! 100% true, my friend's cousin works there.",
                true, "\"My friend's cousin\" is not a source. NASA has never mentioned Shanti Nagar.",
                "Big claims need big proof. \"Someone's cousin said\" is the classic sign of a rumour."),
            new NewsItem("Health Tips Forever",
                "Drinking hot lemon water every hour cures ALL diseases. Doctors don't want you to know!",
                true, "No doctor's name, no study, and a promise to cure \"everything\".",
                "Health rumours can be dangerous. Ask a real doctor (Meera Didi runs the clinic!) before believing or forwarding."),
            new NewsItem("RTI reply (your evidence)",
                "Ward Office records: ₹10 lakh released in March for Gulmohar Park renovation. Work status: \"Complete\".",
                false, "Official RTI reply with file number WO/RTI/112, signed by the Public Information Officer.",
                "RTI replies are official documents, which is why they make such strong evidence."),
            new NewsItem("\"Meera Exposed\" page (created yesterday)",
                "SHOCKING: Meera Didi is secretly a robot sent by aliens to steal our pressure cookers. Share before it's deleted!",
                true, "The page is one day old, has no real name and the photo is edited.",
                "\"Share before it's deleted\" is a trick to make you forward without thinking. Brand-new anonymous pages are red flags."),
            new NewsItem("Family group",
                "The ink on your finger means your vote gets counted twice. So go vote early!",
                true, "Nobody can explain how that would work, and the Election Officer's FAQ says otherwise.",
                "The ink only shows that you have already voted, so nobody votes twice. One person, one vote."),
            new NewsItem("Election Officer's FAQ",
                "Name missing from the voter list? Check the electoral roll at the Ward Office or on the official voter portal before election day.",
                false, "From the Election Officer's official FAQ leaflet.",
                "Real advice from a real authority. Checking your name early is smart citizenship."),
            new NewsItem("Viral video clip",
                "VIDEO: Dr. Vaada Verma promises a metro station INSIDE the park pond! (32-second clip)",
                true, "The full speech is 40 minutes long. The clip is cut mid-sentence and sped up.",
                "Edited clips can make anyone say anything. Look for the full, original video before judging."),
        };

        public static readonly SloganPart[] SloganStarts =
        {
            new SloganPart("Hisaab do, hisaab do,", SloganTone.Peaceful),
            new SloganPart("Park humara, paisa humara,", SloganTone.Peaceful),
            new SloganPart("Golden dustbin kahan gaya?", SloganTone.Funny),
            new SloganPart("Jugaad Singh ko sabak sikhao,", SloganTone.Angry),
        };

        public static readonly SloganPart[] SloganEnds =
        {
            new SloganPart("sach ki awaaz dabegi nahi!", SloganTone.Peaceful),
            new SloganPart("RTI ne sab bata diya!", SloganTone.Peaceful),
            new SloganPart("Chaand pe WiFi baad mein, pehle bench do!", SloganTone.Funny),
            new SloganPart("warna tod-phod karenge!", SloganTone.Violent),
        };

        public static SloganResult EvaluateSlogan(SloganPart start, SloganPart end)
        {
            if (start.tone == SloganTone.Violent || end.tone == SloganTone.Violent)
                return new SloganResult
                {
                    accepted = false, civic = -5, courage = 0,
                    feedback = "Meera Didi shakes her head: \"Dhamki wala naara nahi. A peaceful march never threatens violence.\""
                };
            bool angry = start.tone == SloganTone.Angry || end.tone == SloganTone.Angry;
            bool funny = start.tone == SloganTone.Funny || end.tone == SloganTone.Funny;
            if (angry)
                return new SloganResult
                {
                    accepted = true, civic = 0, courage = 2,
                    feedback = "Strong words! It works, but attack the problem, not the person. Facts persuade better than anger."
                };
            return new SloganResult
            {
                accepted = true, civic = 3, courage = funny ? 4 : 3,
                feedback = funny ? "Ha! Funny AND peaceful. People will remember this one." : "Clear, peaceful and backed by facts. Perfect placard!"
            };
        }

        public static string Slogan(SloganPart start, SloganPart end) => start.text + " " + end.text;

        /// <summary>Waste sorting: Green +1 per correct item, and Civic +2 for a near-perfect round. Mistakes teach, not punish.</summary>
        public static (int green, int civic) WasteReward(int correct, int total) => (correct, correct >= total - 1 ? 2 : 0);

        /// <summary>Fact-checking: Civic +1 per correct call, and Honesty +3 for missing at most one.</summary>
        public static (int civic, int honesty) FakeNewsReward(int correct, int total) => (correct, correct >= total - 1 ? 3 : 0);

        /// <summary>A random round of distinct items.</summary>
        public static List<T> Pick<T>(IList<T> source, int count, Random rng)
        {
            var list = new List<T>(source);
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                var t = list[i]; list[i] = list[j]; list[j] = t;
            }
            if (list.Count > count) list.RemoveRange(count, list.Count - count);
            return list;
        }

        public static string BinName(WasteBin b) => b == WasteBin.Wet ? "Gila (Wet)" : b == WasteBin.Dry ? "Sukha (Dry)" : "Khatarnak (Hazardous)";
    }

    /// <summary>The player's evidence and errand items (story variables <c>item_&lt;id&gt;</c>).</summary>
    public static class ItemDatabase
    {
        public class ItemDef
        {
            public string id, name, description;
            public bool evidence;
            public ItemDef(string id, string name, bool evidence, string description) { this.id = id; this.name = name; this.evidence = evidence; this.description = description; }
        }

        public static readonly ItemDef[] All =
        {
            new ItemDef("photo", "Photo: \"Renovated\" park", true, "The board says \"Renovation complete, ₹10 lakh\". Behind it: broken swings, a garbage heap and a dry pond. Timestamped today."),
            new ItemDef("receipt", "Bill for 500 golden dustbins", true, "A bill from \"Jugaad Supply Co.\" to the Ward: 500 \"gold-plated\" dustbins, ₹9,50,000. The park has two dented dustbins."),
            new ItemDef("rti", "RTI reply WO/RTI/112", true, "Official reply from the Ward Office: ₹10 lakh released for the park in March, status \"Complete\", contractor: Jugaad Supply Co."),
            new ItemDef("groceries", "Sharma Aunty's groceries", false, "Atta, dal, haldi and a packet of Parle-G (her favourite)."),
            new ItemDef("ration_card", "Sharma Aunty's ration card", false, "Collected from the Ration Office, fair and square."),
            new ItemDef("petition", "Petition (signed)", false, "\"Repair Gulmohar Park and investigate the missing ₹10 lakh.\" Signed by your supporters."),
            new ItemDef("voter_slip", "Voter slip", false, "Your polling slip: Ward 7, Booth 1, Mela Maidan."),
        };

        public static ItemDef Get(string id)
        {
            foreach (var d in All) if (d.id == id) return d;
            return null;
        }

        public static string Var(string id) => "item_" + id;
    }
}
