using System.Collections.Generic;
using System.IO;
using System.Linq;
using static DBDefsLib.Structs;

namespace DBDefsLib
{
    public class DBDWriter
    {
        public void Save(DBDefinition definition, string target, bool sort = false)
        {
            if (!Directory.Exists(Path.GetDirectoryName(target)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(target));
            }

            if (sort)
            {
                var sortedDBDefinitions = definition.versionDefinitions.ToList();
                sortedDBDefinitions.Sort(new DBDVersionsComparer(false));
                definition.versionDefinitions = sortedDBDefinitions.ToArray();
            }

            using (StreamWriter writer = new StreamWriter(target))
            {
                writer.NewLine = "\n";

                writer.WriteLine("COLUMNS");
                foreach (var columnDefinition in definition.columnDefinitions)
                {
                    if (columnDefinition.Value.type == "uint")
                    {
                        writer.Write("int");
                    }
                    else
                    {
                        writer.Write(columnDefinition.Value.type);
                    }

                    if (!string.IsNullOrEmpty(columnDefinition.Value.foreignTable) && !string.IsNullOrEmpty(columnDefinition.Value.foreignColumn))
                    {
                        writer.Write("<" + columnDefinition.Value.foreignTable + "::" + Utils.NormalizeColumn(columnDefinition.Value.foreignColumn) + ">");
                    }

                    var normalizedColumnName = Utils.NormalizeColumn(columnDefinition.Key);
                    writer.Write(" " + normalizedColumnName);
                    if (definition.columnDefinitions[columnDefinition.Key].type == "locstring" && !columnDefinition.Key.EndsWith("_lang"))
                    {
                        writer.Write("_lang");
                    }

                    if (columnDefinition.Value.verified == false)
                    {
                        writer.Write("?");
                    }

                    if (!string.IsNullOrWhiteSpace(columnDefinition.Value.comment))
                    {
                        writer.Write(" // " + columnDefinition.Value.comment);
                    }

                    writer.Write(writer.NewLine);
                }

                // New line between COLUMNS and BUILD definitions
                writer.Write(writer.NewLine);

                for (var i = 0; i < definition.versionDefinitions.Length; i++)
                {
                    var versionDefinition = definition.versionDefinitions[i];

                    if (versionDefinition.layoutHashes.Length > 0)
                    {
                        writer.WriteLine("LAYOUT " + string.Join(", ", versionDefinition.layoutHashes));
                    }

                    if (versionDefinition.builds.Length > 0)
                    {
                        var sortedVersionlist = new List<Build>();
                        sortedVersionlist.AddRange(versionDefinition.builds);
                        sortedVersionlist.Sort();
                        sortedVersionlist.Reverse();
                        versionDefinition.builds = sortedVersionlist.ToArray();

                        var buildsByMajor = new Dictionary<string, List<Build>>();
                        for (var b = 0; b < versionDefinition.builds.Length; b++)
                        {
                            var major = versionDefinition.builds[b].expansion + "." + versionDefinition.builds[b].major + "." + versionDefinition.builds[b].minor;
                            if (!buildsByMajor.ContainsKey(major))
                            {
                                buildsByMajor.Add(major, new List<Build>());
                            }

                            buildsByMajor[major].Add(versionDefinition.builds[b]);
                        }

                        foreach (var buildList in buildsByMajor)
                        {
                            buildList.Value.Reverse();
                            writer.Write("BUILD ");
                            for (var b = 0; b < buildList.Value.Count; b++)
                            {
                                writer.Write(buildList.Value[b].ToString());
                                if (b + 1 < buildList.Value.Count)
                                {
                                    writer.Write(", ");
                                }
                            }
                            writer.Write(writer.NewLine);
                        }
                    }

                    if (versionDefinition.buildRanges.Length > 0)
                    {
                        var sortedBuildRangeList = new List<BuildRange>();
                        sortedBuildRangeList.AddRange(versionDefinition.buildRanges);
                        if (sort)
                        {
                            sortedBuildRangeList.Sort((x, y) => x.CompareTo(y) * -1); // invert build ranges to follow def sort
                        }
                        else
                        {
                            sortedBuildRangeList.Sort();
                        }
                        versionDefinition.buildRanges = sortedBuildRangeList.ToArray();
                        foreach (var buildRange in versionDefinition.buildRanges)
                        {
                            writer.WriteLine("BUILD " + buildRange.ToString());
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(versionDefinition.comment))
                    {
                        writer.WriteLine("COMMENT " + versionDefinition.comment);
                    }

                    foreach (var column in versionDefinition.definitions)
                    {
                        var attribs = new List<string>();
                        if (column.isNonInline)
                        {
                            attribs.Add("noninline");
                        }
                        if (column.isID)
                        {
                            attribs.Add("id");
                        }
                        if (column.isRelation)
                        {
                            attribs.Add("relation");
                        }
                        if (attribs.Count > 0)
                        {
                            writer.Write("$" + System.String.Join(",", attribs) + "$");
                        }

                        var normalizedColumnName = Utils.NormalizeColumn(column.name);

                        writer.Write(normalizedColumnName);

                        // locstrings should always have _lang
                        if (definition.columnDefinitions[column.name].type == "locstring" && !column.name.EndsWith("_lang"))
                        {
                            writer.Write("_lang");
                        }

                        if (column.size > 0)
                        {
                            writer.Write("<");
                            if (!column.isSigned)
                            {
                                writer.Write("u");
                            }
                            writer.Write(column.size + ">");
                        }

                        if (column.arrLength > 0)
                        {
                            writer.Write("[" + column.arrLength + "]");
                        }

                        if (!string.IsNullOrWhiteSpace(column.comment))
                        {
                            writer.Write(" // " + column.comment);
                        }

                        writer.Write(writer.NewLine);
                    }

                    if (i + 1 < definition.versionDefinitions.Length)
                    {
                        writer.Write(writer.NewLine);
                    }
                }

                writer.Flush();
                writer.Close();
            }
        }

        internal class DBDVersionsComparer : IComparer<VersionDefinitions>
        {
            private readonly bool _asc;

            public DBDVersionsComparer(bool ascending = true)
            {
                _asc = ascending;
            }

            public int Compare(VersionDefinitions x, VersionDefinitions y)
            {
                Build xmax, ymax;

                if (_asc)
                {
                    xmax = x.buildRanges.Select(b => b.minBuild).Concat(x.builds).OrderBy(b => b).FirstOrDefault();
                    ymax = y.buildRanges.Select(b => b.minBuild).Concat(y.builds).OrderBy(b => b).FirstOrDefault();
                }
                else
                {
                    xmax = x.buildRanges.Select(b => b.maxBuild).Concat(x.builds).OrderByDescending(b => b).FirstOrDefault();
                    ymax = y.buildRanges.Select(b => b.maxBuild).Concat(y.builds).OrderByDescending(b => b).FirstOrDefault();
                }

                int result = 0;
                if (xmax != null && ymax != null)
                    result = xmax.CompareTo(ymax);

                return result;
            }
        }
    }
}
