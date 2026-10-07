using System;
using System.Collections.Generic;

namespace MohallaHero
{
    [Serializable]
    public class VarEntry
    {
        public string name;
        public int value;
        public VarEntry() { }
        public VarEntry(string name, int value) { this.name = name; this.value = value; }
    }

    /// <summary>
    /// Every story variable in one place: Ink VARs, mission progress counters, evidence (<c>item_*</c>),
    /// trust (<c>trust_*</c>), world objects that were dealt with (<c>obj_*</c>) and remembered once-only choices.
    /// Saves store only the values that differ from the defaults.
    /// </summary>
    public class StoryState
    {
        readonly Dictionary<string, int> vars = new Dictionary<string, int>();
        readonly Dictionary<string, int> defaults = new Dictionary<string, int>();

        public event Action<string, int> Changed;

        public int Get(string name)
        {
            if (vars.TryGetValue(name, out var v)) return v;
            return defaults.TryGetValue(name, out var d) ? d : 0;
        }

        public bool Is(string name) => Get(name) != 0;

        public void Set(string name, int value)
        {
            if (Get(name) == value) return;
            vars[name] = value;
            Changed?.Invoke(name, value);
        }

        public void Add(string name, int delta) => Set(name, Get(name) + delta);

        public void SetDefault(string name, int value) => defaults[name] = value;

        public void SetDefaults(IDictionary<string, int> values)
        {
            foreach (var kv in values) defaults[kv.Key] = kv.Value;
        }

        public List<VarEntry> Snapshot()
        {
            var list = new List<VarEntry>();
            foreach (var kv in vars)
                if (!defaults.TryGetValue(kv.Key, out var d) || d != kv.Value) list.Add(new VarEntry(kv.Key, kv.Value));
            list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return list;
        }

        public void Load(List<VarEntry> entries)
        {
            vars.Clear();
            if (entries == null) return;
            foreach (var e in entries) if (!string.IsNullOrEmpty(e.name)) vars[e.name] = e.value;
        }
    }
}
