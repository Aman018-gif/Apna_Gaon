using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace MohallaHero
{
    /// <summary>Locked areas (Bazaar Gali, Mela Maidan) behind barricades that the story removes.</summary>
    public class AreaManager : MonoBehaviour
    {
        readonly HashSet<AreaId> unlocked = new HashSet<AreaId> { AreaId.Mohalla };
        readonly Dictionary<AreaId, List<GameObject>> barriers = new Dictionary<AreaId, List<GameObject>>();
        Tilemap blockers;
        WorldMap map;

        public void Init(WorldMap map, Tilemap blockers)
        {
            this.map = map;
            this.blockers = blockers;
        }

        public void RegisterBarrier(AreaId area, GameObject go)
        {
            if (!barriers.TryGetValue(area, out var list)) barriers[area] = list = new List<GameObject>();
            list.Add(go);
        }

        public bool IsUnlocked(AreaId a) => unlocked.Contains(a);

        /// <summary>Opens a locked area: removes the barricade, the physics blockers and updates navigation.</summary>
        public void Unlock(AreaId area, bool announce = true)
        {
            if (!unlocked.Add(area)) return;
            map.SetGateLocked(area, false);
            if (WorldLayout.Gates.TryGetValue(area, out var cells))
                foreach (var c in cells) blockers.SetTile(new Vector3Int(c.x, c.y, 0), null);
            if (barriers.TryGetValue(area, out var list))
                foreach (var go in list) if (go != null) Destroy(go);
            barriers.Remove(area);

            var gm = GameManager.I;
            gm.RefreshNavigation();
            if (announce) gm.UI.Notify($"Naya ilaaka khula: {Format.Area(area)}!", NotifyStyle.Big);
            GameEvents.RaiseAreaUnlocked(area);
        }

        public List<int> Snapshot()
        {
            var list = new List<int>();
            foreach (var a in unlocked) if (a != AreaId.Mohalla) list.Add((int)a);
            return list;
        }
    }
}
