using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static DBDefsLib.Structs;

namespace DBDefsLib
{
    public class DBDWriter
    {
        public void Save(DBDefinition definition, string target)
        {
            if (!Directory.Exists(Path.GetDirectoryName(target)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(target));
            }

            using (StreamWriter writer = new StreamWriter(target))
            {
                writer.NewLine = "\n";

                writer.WriteLine("COLUMNS");
                foreach(var columnDefinition in definition.columnDefinitions)
                {
                    writer.Write(columnDefinition.Value.type);

                    if (!string.IsNullOrEmpty(columnDefinition.Value.foreignTable) && !string.IsNullOrEmpty(columnDefinition.Value.foreignColumn))
                    {
                        writer.Write("<" + columnDefinition.Value.foreignTable + "::" + columnDefinition.Value.foreignColumn + ">");
                    }

                    writer.Write(" " + columnDefinition.Key);

                    if(columnDefinition.Value.verified == false)
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
                    if(versionDefinition.layoutHashes.Length > 0)
                    {
                        writer.WriteLine("LAYOUT " + string.Join(", ", versionDefinition.layoutHashes));
                    }

                    if(versionDefinition.builds.Length > 0)
                    {
                        writer.Write("BUILD ");
                        for(var b =0; b < versionDefinition.builds.Length; b++)
                        {
                            writer.Write(Utils.BuildToString(versionDefinition.builds[b]));
                            if(b + 1 < versionDefinition.builds.Length)
                            {
                                writer.Write(", ");
                            }
                        }
                        writer.Write(writer.NewLine);
                    }

                    if (!string.IsNullOrWhiteSpace(versionDefinition.comment))
                    {
                        writer.WriteLine("COMMENT " + versionDefinition.comment);
                    }

                    if (versionDefinition.buildRanges.Length > 0)
                    {
                        foreach(var buildRange in versionDefinition.buildRanges)
                        {
                            writer.WriteLine("BUILD " + Utils.BuildToString(buildRange.minBuild) + "-" + Utils.BuildToString(buildRange.maxBuild));
                        }
                    }

                    foreach(var column in versionDefinition.definitions)
                    {
                        if (column.isID)
                        {
                            writer.Write("$id$");
                        }

                        if (column.isRelation)
                        {
                            writer.Write("$relation$");
                        }

                        writer.Write(column.name);

                        if(column.size > 0)
                        {
                            writer.Write("<" + column.size + ">");
                        }

                        if(column.arrLength > 0)
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
    }
}
