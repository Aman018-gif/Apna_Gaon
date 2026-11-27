using System.Collections.Generic;
using UnityEngine;

namespace MohallaHero
{
    /// <summary>Spawns the residents and resolves where the story wants them to be.</summary>
    public class NPCManager : MonoBehaviour
    {
        readonly Dictionary<NpcId, NPC> npcs = new Dictionary<NpcId, NPC>();
        public IEnumerable<NPC> All => npcs.Values;
        public NPC Get(NpcId id) => npcs.TryGetValue(id, out var n) ? n : null;

        public void Spawn()
        {
            foreach (var def in NpcDatabase.All)
            {
                var npc = WorldObjects.Create<NPC>("NPC_" + def.id, WorldLayout.CellFeet(def.Door), transform);
                npc.Setup(def);
                npcs[def.id] = npc;
            }
        }

        public void PlaceAll()
        {
            foreach (var n in npcs.Values) n.PlaceBySchedule();
        }

        /// <summary>Workplaces that depend on the story (Lala sells on the footpath until the bazaar reopens).</summary>
        public Vector2Int WorkCell(NpcDef d)
        {
            var gm = GameManager.I;
            if (d.id == NpcId.Lala && !gm.Areas.IsUnlocked(AreaId.Bazaar)) return new Vector2Int(30, 33);
            return d.workCell;
        }

        /// <summary>Story positions that override the normal schedule.</summary>
        public bool TryGetSpecialSpot(NpcId id, out Vector2Int cell)
        {
            var gm = GameManager.I;
            var s = gm.Story;
            cell = default;
            int ch = gm.Missions.CurrentChapter;

            // Chapter 2: the bullying scene in the park corner
            if (ch == 2 && s.Is("m2_started") && !s.Is("m2_stopped"))
            {
                if (id == NpcId.Golu) { cell = WorldLayout.BullyCorner; return true; }
                if (id == NpcId.Bunty) { cell = WorldLayout.BullyCorner + new Vector2Int(1, 0); return true; }
            }

            // Chapter 5: Meera waits in the park once the placards are ready
            if (ch == 5 && id == NpcId.Meera && s.Get("placards") >= Balance.PlacardsNeeded && !s.Is("m5_gathered"))
            {
                cell = WorldLayout.ParkGather;
                return true;
            }
            // ... and Officer Fernandes waits for the march at the Ward Office
            if (ch == 5 && id == NpcId.Fernandes && s.Is("m5_gathered")) { cell = new Vector2Int(81, 36); return true; }

            // Chapter 6: the Election Mela
            if (ch >= 6)
            {
                switch (id)
                {
                    case NpcId.Fernandes: cell = s.Is("m6_started") ? new Vector2Int(92, 3) : new Vector2Int(84, 24); return true;
                    case NpcId.Jugaad: cell = new Vector2Int(79, 11); return true;
                    case NpcId.Vaada: cell = new Vector2Int(85, 11); return true;
                    case NpcId.Meera: cell = new Vector2Int(91, 11); return true;
                    case NpcId.Chacha: cell = new Vector2Int(79, 5); return true;
                    case NpcId.Pintu: cell = new Vector2Int(84, 17); return true;
                    case NpcId.Asha: cell = new Vector2Int(80, 17); return true;
                    case NpcId.Golu: cell = new Vector2Int(82, 18); return true;
                    case NpcId.Bunty: cell = new Vector2Int(87, 17); return true;
                    case NpcId.Sharma: cell = new Vector2Int(85, 8); return true;
                    case NpcId.Lala: cell = new Vector2Int(82, 7); return true;
                }
            }
            return false;
        }
    }
}
