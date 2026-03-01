#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Text;

public static class MovementSystemCsvImporter
{
    [MenuItem("VRAnatomy/Import/MovementSystem CSV -> Database")]
    public static void Import()
    {
        string csvPath = "Assets/Resources/CsvFiles/Project_MovementSystem_EDatas.csv";
        if (!File.Exists(csvPath))
        {
            Debug.LogError("CSV bulunamadı: " + csvPath);
            return;
        }

        var lines = File.ReadAllLines(csvPath);
        if (lines.Length < 2)
        {
            Debug.LogError("CSV boş ya da sadece header var.");
            return;
        }

        var header = SplitCsvLine(lines[0]);
        var col = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < header.Count; i++) col[header[i].Trim()] = i;

        string Get(List<string> cols, string name)
        {
            if (!col.TryGetValue(name, out int idx)) return "";
            if (idx < 0 || idx >= cols.Count) return "";
            return cols[idx];
        }

        var db = ScriptableObject.CreateInstance<MovementSystemDatabase>();

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            var cols = SplitCsvLine(lines[i]);

            var e = new MovementSystemDatabase.Entry
            {
                id = Get(cols, "id"),
                valence = Get(cols, "valence"),
                doc_type = Get(cols, "doc_type"),
                content_type = Get(cols, "content_type"),
                title = Get(cols, "title"),
                topic = Get(cols, "topic"),
                region = Get(cols, "region"),
                tags = Get(cols, "tags"),
                body = Get(cols, "body"),
                steps = Get(cols, "steps"),
                source = Get(cols, "source"),
            };

            if (string.IsNullOrWhiteSpace(e.id)) continue;
            db.entries.Add(e);
        }

        const string assetPath = "Assets/MovementSystemDatabase.asset";
        AssetDatabase.DeleteAsset(assetPath);
        AssetDatabase.CreateAsset(db, assetPath);
        AssetDatabase.SaveAssets();

        Debug.Log($"DB oluşturuldu: {assetPath} | entry={db.entries.Count}");
    }

    private static List<string> SplitCsvLine(string line)
    {
        var res = new List<string>();
        bool inQuotes = false;
        var cur = new StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    cur.Append('"'); i++;
                }
                else inQuotes = !inQuotes;
            }
            else if ((c == ',' || c == ';') && !inQuotes)
            {
                res.Add(cur.ToString());
                cur.Clear();
            }
            else cur.Append(c);
        }

        res.Add(cur.ToString());
        return res;
    }
}
#endif