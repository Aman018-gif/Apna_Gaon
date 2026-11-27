using System.Collections.Generic;
using UnityEngine;

namespace MohallaHero
{
    /// <summary>
    /// Chapter 5, the peaceful protest. Unlocked when enough residents trust the player (TrustSystem); then
    /// placards (slogan mini-game) → gathering in Gulmohar Park at dusk → candle march along a line of diyas with
    /// supporters following the player → an incident on the way (violence or calm, decided in Ink) → petition
    /// submitted to the election officer. Every supporter walks in a procession that follows the player's footsteps.
    /// </summary>
    public class ProtestManager : MonoBehaviour
    {
        GameManager gm => GameManager.I;
        readonly List<NpcId> followers = new List<NpcId>();
        readonly List<Vector2> trail = new List<Vector2>();
        readonly List<SpriteRenderer> diyas = new List<SpriteRenderer>();
        readonly List<GlowLight> diyaGlows = new List<GlowLight>();
        Transform root;

        public bool Marching { get; private set; }
        public int Checkpoints => gm.Story.Get("march_checkpoints");
        public Vector2 NextCheckpoint => WorldLayout.CellFeet(WorldLayout.MarchRoute[Mathf.Clamp(Checkpoints, 0, WorldLayout.MarchRoute.Length - 1)]);

        /// <summary>The procession lasts from the gathering until the petition is handed in.</summary>
        bool ProcessionActive => gm.Story.Is("m5_gathered") && !gm.Story.Is("m5_done");

        public void Init(Transform worldRoot)
        {
            root = new GameObject("Protest").transform;
            root.SetParent(worldRoot, false);
        }

        public static List<NpcId> ChooseFollowers(TrustSystem trust)
        {
            var list = new List<NpcId> { NpcId.Meera, NpcId.Pintu };
            foreach (var id in TrustSystem.Convincible) if (trust.HasJoined(id)) list.Add(id);
            list.Add(NpcId.Golu);
            return list;
        }

        public bool FollowerIndex(NpcId id, out int index)
        {
            index = followers.IndexOf(id);
            if (!ProcessionActive) return false;
            return index >= 0;
        }

        // ------------------------------------------------------------------ gathering

        /// <summary>Called from the story (m5_meera_gather): dusk falls and everyone gathers in the park.</summary>
        public void Gather()
        {
            gm.Clock.SkipTo(Balance.EveningHour + 0.5f);
            followers.Clear();
            followers.AddRange(ChooseFollowers(gm.Trust));
            var center = WorldLayout.CellFeet(WorldLayout.ParkGather);
            gm.Player.Teleport(center + new Vector2(0, -1.2f));
            gm.CameraRig.SnapToTarget();
            ResetTrail();
            for (int i = 0; i < followers.Count; i++)
            {
                var npc = gm.Npcs.Get(followers[i]);
                if (npc == null) continue;
                float a = i / (float)followers.Count * Mathf.PI * 2f;
                npc.TeleportTo(center + new Vector2(Mathf.Cos(a) * 1.8f, Mathf.Sin(a) * 1.3f));
            }
            gm.Player.SetCandle(true);
        }

        public void StartMarch()
        {
            Marching = true;
            BuildDiyas();
            ResetTrail();
            gm.Player.SetCandle(true);
            gm.UI.Notify("Shanti March shuru!", NotifyStyle.Big);
            gm.UI.Notify("Diyon ke raaste chalo. Saath wale tumhare peeche aayenge.");
            foreach (var n in gm.Npcs.All) n.EndTalk();
        }

        /// <summary>After loading a save made during the march.</summary>
        public void RestoreAfterLoad()
        {
            if (!ProcessionActive) return;
            followers.Clear();
            followers.AddRange(ChooseFollowers(gm.Trust));
            ResetTrail();
            Vector2 p = gm.Player.transform.position;
            for (int i = 0; i < followers.Count; i++)
                gm.Npcs.Get(followers[i])?.TeleportTo(p + new Vector2(0, -1f - i * 0.8f));
            gm.Player.SetCandle(true);
            if (Checkpoints < WorldLayout.MarchRoute.Length) { Marching = true; BuildDiyas(); }
        }

        void ClearDiyas()
        {
            foreach (var d in diyas) if (d != null) Destroy(d.gameObject);
            diyas.Clear();
            diyaGlows.Clear();
        }

        void BuildDiyas()
        {
            ClearDiyas();
            // a line of diyas between consecutive checkpoints, starting at the park
            Vector2 prev = WorldLayout.CellFeet(WorldLayout.ParkGather);
            for (int i = 0; i < WorldLayout.MarchRoute.Length; i++)
            {
                Vector2 next = WorldLayout.CellFeet(WorldLayout.MarchRoute[i]);
                int n = Mathf.Max(2, Mathf.RoundToInt(Vector2.Distance(prev, next) / 3f));
                for (int k = 1; k <= n; k++)
                {
                    var pos = Vector2.Lerp(prev, next, k / (float)n) + new Vector2(0.9f, 0);
                    var sr = WorldObjects.CreateSprite("Diya", SpriteFactory.Diya(true), pos, root);
                    var glow = WorldObjects.AddGlow(sr.transform, new Vector2(0, 0.3f), new Color(1f, 0.75f, 0.3f, 0.5f), k == n ? 2.4f : 1.3f);
                    diyas.Add(sr);
                    glow.Flicker = true;
                    diyaGlows.Add(glow);
                }
                prev = next;
            }
        }

        // ------------------------------------------------------------------ the march

        void ResetTrail()
        {
            trail.Clear();
            Vector2 p = gm.Player.transform.position;
            for (int i = 0; i < 40; i++) trail.Add(p + new Vector2(0, -0.1f * i));
            trail.Reverse();
        }

        /// <summary>Where follower <paramref name="index"/> should be: further back along the player's recent path.</summary>
        public Vector2 TrailPoint(int index)
        {
            float want = 1.1f + index * 0.95f;
            Vector2 prev = gm.Player.transform.position;
            float acc = 0f;
            for (int i = trail.Count - 1; i >= 0; i--)
            {
                float seg = Vector2.Distance(prev, trail[i]);
                if (acc + seg >= want) return Vector2.Lerp(prev, trail[i], (want - acc) / Mathf.Max(0.0001f, seg));
                acc += seg;
                prev = trail[i];
            }
            return prev;
        }

        void Update()
        {
            if (gm == null || !gm.InWorld || gm.Player == null) return;
            bool procession = ProcessionActive;
            gm.Player.SetCandle(procession);
            if (!procession)
            {
                if (Marching) Marching = false;
                if (diyas.Count > 0 && gm.Story.Is("m5_done")) ClearDiyas();   // the march is over
                return;
            }

            Vector2 p = gm.Player.transform.position;
            if (trail.Count == 0 || Vector2.Distance(trail[trail.Count - 1], p) > 0.2f)
            {
                trail.Add(p);
                if (trail.Count > 400) trail.RemoveAt(0);
            }

            if (!Marching || gm.IsModalOpen) return;
            int cp = Checkpoints;
            if (cp >= WorldLayout.MarchRoute.Length) { Marching = false; return; }
            if (Vector2.Distance(p, NextCheckpoint) > 1.8f) return;

            gm.Story.Set("march_checkpoints", cp + 1);
            int reached = cp + 1;
            if (reached == 2) gm.Dialogue.RunKnot("march_incident");
            else if (reached >= WorldLayout.MarchRoute.Length)
            {
                Marching = false;
                gm.UI.Notify("Ward Office pahunch gaye! Officer Fernandes ko petition do.", NotifyStyle.Big);
            }
            else
            {
                string[] chants = { "Hisaab do, hisaab do!", "Park humara, paisa humara!", "Sach ki awaaz dabegi nahi!" };
                gm.UI.Subtitle("Crowd", chants[cp % chants.Length]);
            }
        }
    }
}
