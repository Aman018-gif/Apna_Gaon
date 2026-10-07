using UnityEngine;

namespace MohallaHero
{
    /// <summary>
    /// Who has something for the player right now ("!" markers, orange map dots) and where the next objective is
    /// (the golden marker in the world). Mirrors the routing in Resources/Stories/mohalla.txt.
    /// </summary>
    public static class Guidance
    {
        public static bool WantsToTalk(GameManager gm, NpcId id)
        {
            var s = gm.Story;
            int ch = gm.Missions.CurrentChapter;
            switch (ch)
            {
                case 1:
                    return id == NpcId.Asha && (!s.Is("m1_started") || gm.Missions.NextStep(gm.Missions.Current)?.var == "m1_done");
                case 2:
                    if (!s.Is("m2_started")) return id == NpcId.Pintu;
                    if (!s.Is("m2_stopped")) return id == NpcId.Golu || id == NpcId.Bunty;
                    if (!s.Is("m2_reported") && id == NpcId.Asha) return true;
                    if (!s.Is("m2_supported") && id == NpcId.Golu) return true;
                    return id == NpcId.Bunty && s.Is("m2_reported") && s.Is("m2_supported");
                case 3:
                    if (!s.Is("m3_started")) return id == NpcId.Sharma;
                    if (!s.Is("m3_bought")) return id == NpcId.Lala;
                    return id == NpcId.Sharma && s.Is("m3_ration");
                case 4:
                    if (!s.Is("m4_started")) return id == NpcId.Meera;
                    if (id == NpcId.Meera) return gm.HasItem("photo") && gm.HasItem("receipt") && gm.HasItem("rti") && gm.Trust.JoinedCount >= Balance.SupportersNeeded;
                    if (id == NpcId.Fernandes) return !s.Is("rti_filed") || (!gm.HasItem("rti") && gm.Clock.Day > s.Get("rti_day"));
                    if (id == NpcId.Lala && !gm.HasItem("receipt") && s.Is("m3_returned")) return true;
                    if (System.Array.IndexOf(TrustSystem.Convincible, id) >= 0) return !gm.Trust.HasJoined(id) && (gm.HasItem("photo") || gm.HasItem("receipt") || gm.HasItem("rti"));
                    return false;
                case 5:
                    if (id == NpcId.Meera) return s.Get("placards") >= Balance.PlacardsNeeded && !s.Is("m5_gathered");
                    return id == NpcId.Fernandes && s.Get("march_checkpoints") >= WorldLayout.MarchRoute.Length;
                case 6:
                    if (!s.Is("m6_started")) return id == NpcId.Fernandes;
                    if (id == NpcId.Chacha) return !s.Is("rumours_checked");
                    if (id == NpcId.Jugaad) return !s.Is("seen_jugaad");
                    if (id == NpcId.Vaada) return !s.Is("seen_vaada");
                    if (id == NpcId.Meera) return !s.Is("seen_meera");
                    return false;
            }
            return false;
        }

        /// <summary>World position of the most useful next target, or null.</summary>
        public static Vector2? ObjectiveTarget(GameManager gm)
        {
            if (gm.Player == null) return null;
            Vector2 p = gm.Player.transform.position;
            var s = gm.Story;
            var m = gm.Missions.Current;
            if (m == null) return null;

            // March checkpoints come first while marching
            if (gm.Protest != null && gm.Protest.Marching) return gm.Protest.NextCheckpoint;

            // A specific person?
            Vector2? best = null;
            float bestD = float.MaxValue;
            foreach (var npc in gm.Npcs.All)
                if (!npc.Hidden && WantsToTalk(gm, npc.Def.id)) Consider(npc.transform.position);
            if (best != null) return best;

            var step = gm.Missions.IsStarted(m) ? gm.Missions.NextStep(m) : null;
            if (step == null) return null;
            string prefix = null, single = null;
            switch (step.var)
            {
                case "lights_off": prefix = "light_"; break;
                case "taps_closed": prefix = "tap_"; break;
                case "saplings": prefix = "sapling_"; break;
                case "piles_cleaned": prefix = "garbage_"; break;
                case "voters_informed": prefix = "door_"; break;
                case "m3_lucky": single = "lucky"; break;
                case "m3_ration": single = "ration"; break;
                case "item_photo": single = "park_board"; break;
                case "item_receipt": single = s.Is("m3_returned") ? null : "raddi"; break;
                case "placards": single = "placards"; break;
                case "mela_play": single = "stage"; break;
                case "m6_done": single = "booth"; break;
            }
            foreach (var o in gm.Builder.Objects)
            {
                if (!o.CanInteract) continue;
                if ((prefix != null && o.Id.StartsWith(prefix)) || (single != null && o.Id == single)) Consider(o.transform.position);
            }
            return best;

            void Consider(Vector2 q)
            {
                float d = (q - p).sqrMagnitude;
                if (d < bestD) { bestD = d; best = q; }
            }
        }
    }
}
