using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DBDefsMerge
{
    public static class MergeJSONManifests
    {
        public static void Merge(string baseFile, string inFile, string outFile)
        {
            var baseEntries = JsonSerializer.Deserialize<ManifestEntry[]>(File.ReadAllText(baseFile));

            var baseDict = new Dictionary<string, ManifestEntry>();
            foreach (var entry in baseEntries)
            {
                baseDict.Add(entry.tableHash, entry);
            }

            var inDict = new Dictionary<string, ManifestEntry>();
            var inEntries = JsonSerializer.Deserialize<ManifestEntry[]>(File.ReadAllText(inFile));
            foreach (var entry in inEntries)
            {
                inDict.Add(entry.tableHash, entry);
            }

            var outEntries = new List<ManifestEntry>();
            foreach (var entry in inDict)
            {
                if (!baseDict.TryGetValue(entry.Key, out var baseEntry))
                {
                    outEntries.Add(entry.Value);
                }
                else
                {
                    var newEntry = baseEntry;

                    if (entry.Value.dbcFileDataID != 0 && baseEntry.dbcFileDataID == 0)
                    {
                        newEntry.dbcFileDataID = entry.Value.dbcFileDataID;
                    }
                  

                    if (entry.Value.db2FileDataID != 0 && baseEntry.db2FileDataID == 0)
                    {
                        newEntry.db2FileDataID = entry.Value.db2FileDataID;
                    }

                    outEntries.Add(newEntry);
                }
            }

            foreach (var entry in baseDict)
            {
                if (!inDict.ContainsKey(entry.Key))
                {
                    outEntries.Add(entry.Value);
                }
            }

            File.WriteAllText(outFile, JsonSerializer.Serialize(outEntries.OrderBy(x => x.tableName).ToArray(), new JsonSerializerOptions() { WriteIndented = true }));
        }

        public struct ManifestEntry
        {
            public string tableName { get; set; }
            public string tableHash { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public int dbcFileDataID { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public int db2FileDataID { get; set; }
        }
    }
}
