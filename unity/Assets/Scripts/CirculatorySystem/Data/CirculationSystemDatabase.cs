using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "VRAnatomy/Circulation System Database")]
public class CirculationSystemDatabase : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string id;
        public string valence;
        public string doc_type;
        public string content_type;
        public string title;
        public string topic;
        public string region;
        public string tags;
        public string body;
        public string steps;
        public string source;
    }

    public List<Entry> entries = new();

    private Dictionary<string, Entry> _map;

    private void OnEnable() => BuildMap();

    private void BuildMap()
    {
        _map = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in entries)
        {
            if (string.IsNullOrWhiteSpace(e.id)) continue;
            _map[e.id.Trim()] = e;
        }
    }

    public bool TryGetTitle(string id, out string title)
    {
        if (_map == null) BuildMap();
        title = null;

        if (string.IsNullOrWhiteSpace(id)) return false;
        if (_map.TryGetValue(id.Trim(), out var e) && !string.IsNullOrWhiteSpace(e.title))
        {
            title = e.title.Trim();
            return true;
        }
        return false;
    }
}
