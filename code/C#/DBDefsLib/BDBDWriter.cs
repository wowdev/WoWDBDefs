using DBDefsLib.Structs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DBDefsLib
{
    public static class BDBDWriter
    {
        public static void Save(string tableName, DBDefinition definition, string target, uint dbcFDID = 0, uint db2fdid = 0)
        {
            Save(new Dictionary<string, DBDefinition> { { tableName, definition } }, target, new Dictionary<string, uint> { { tableName, dbcFDID } }, new Dictionary<string, uint> { { tableName, db2fdid } });
        }

        public static void Save(Dictionary<string, DBDefinition> dbds, string target, Dictionary<string, uint> dbcFDIDs = null, Dictionary<string, uint> db2FDIDs = null, List<MappingDefinition> enumMap = null, Dictionary<string, EnumDefinition> enumDefinitions = null)
        {
            var stringBlock = new Dictionary<string, int>();
            foreach (var dbd in dbds)
            {
                stringBlock.TryAdd(dbd.Key, 0);

                foreach (var col in dbd.Value.columnDefinitions)
                {
                    stringBlock.TryAdd(col.Key, 0);

                    if (!string.IsNullOrEmpty(col.Value.foreignTable))
                        stringBlock.TryAdd(col.Value.foreignTable, 0);

                    if (!string.IsNullOrEmpty(col.Value.foreignColumn))
                        stringBlock.TryAdd(col.Value.foreignColumn, 0);

                    if (!string.IsNullOrEmpty(col.Value.comment))
                        stringBlock.TryAdd(col.Value.comment, 0);
                }

                foreach (var version in dbd.Value.versionDefinitions)
                {
                    foreach (var defn in version.definitions)
                    {
                        if (!string.IsNullOrEmpty(defn.comment))
                            stringBlock.TryAdd(defn.comment, 0);
                    }

                    if (!string.IsNullOrEmpty(version.comment))
                        stringBlock.TryAdd(version.comment, 0);
                }
            }

            if (enumMap != null && enumMap.Count > 0)
            {
                foreach (var map in enumMap)
                {
                    stringBlock.TryAdd(map.tableName, 0);
                    stringBlock.TryAdd(map.columnName, 0);

                    if (!string.IsNullOrEmpty(map.metaValue))
                        stringBlock.TryAdd(map.metaValue, 0);

                    if (!string.IsNullOrEmpty(map.conditionalTable))
                        stringBlock.TryAdd(map.conditionalTable, 0);

                    if (!string.IsNullOrEmpty(map.conditionalColumn))
                        stringBlock.TryAdd(map.conditionalColumn, 0);

                    if (!string.IsNullOrEmpty(map.conditionalValue))
                        stringBlock.TryAdd(map.conditionalValue, 0);

                    if (!string.IsNullOrEmpty(map.comment))
                        stringBlock.TryAdd(map.comment, 0);
                }
            }

            if (enumDefinitions != null && enumDefinitions.Count > 0)
            {
                foreach (var enumDef in enumDefinitions)
                {
                    stringBlock.TryAdd(enumDef.Key, 0);
                    foreach (var entry in enumDef.Value.entries)
                    {
                        if(!string.IsNullOrEmpty(entry.name))
                            stringBlock.TryAdd(entry.name, 0);

                        if (!string.IsNullOrEmpty(entry.comment))
                            stringBlock.TryAdd(entry.comment, 0);
                    }
                }
            }

            using (var fs = new FileStream(target, FileMode.Create))
            using (var bw = new BinaryWriter(fs))
            {
                bw.Write(['B', 'D', 'B', 'D']);
                bw.Write(1);

                // String block
                bw.Write(['S', 'T', 'R', 'B']);
                var strbLengthPos = bw.BaseStream.Position;
                bw.Write(0);
                foreach (var str in stringBlock.Keys)
                {
                    var offs = bw.BaseStream.Position - strbLengthPos - 4;
                    var asBytes = Encoding.UTF8.GetBytes(str);
                    bw.Write((ushort)asBytes.Length);
                    bw.Write(asBytes);
                    stringBlock[str] = (int)offs;
                }

                var strbPos = bw.BaseStream.Position;
                bw.BaseStream.Position = strbLengthPos;
                bw.Write((uint)(strbPos - strbLengthPos - 4));
                bw.BaseStream.Position = strbPos;

                long offsetsPos = 0;

                // Only write TBLS if we're exporting more than 1 DBD
                if (dbds.Count > 1)
                {
                    bw.Write(['T', 'B', 'L', 'S']);
                    bw.Write(dbds.Count);

                    // Table names
                    var tableNames = dbds.Keys;
                    foreach (var tableName in tableNames)
                        bw.WriteStringBlockString(stringBlock, tableName);

                    // Table hashes
                    foreach (var tableName in tableNames)
                        bw.Write(MakeHash(tableName.ToUpperInvariant()));

                    // Table offsets
                    offsetsPos = bw.BaseStream.Position;
                    var tableOffsets = new uint[dbds.Count];
                    foreach (var offset in tableOffsets)
                        bw.Write(offset);
                }

                // Write each DBD
                foreach (var dbd in dbds)
                {
                    var tableName = dbd.Key;
                    var def = dbd.Value;

                    bw.Write(['T', 'A', 'B', 'L']);

                    var lengthPos = bw.BaseStream.Position;
                    bw.Write(0);
                    bw.Write(MakeHash(tableName.ToUpperInvariant()));
                    bw.Write(dbcFDIDs.TryGetValue(tableName, out var dbcFDID) ? dbcFDID : 0); // DBC FDID
                    bw.Write(db2FDIDs.TryGetValue(tableName, out var db2FDID) ? db2FDID : 0); // DB2 FDID

                    // Name
                    bw.WriteStringBlockString(stringBlock, tableName);

                    // Col count
                    bw.Write((ushort)def.columnDefinitions.Count);

                    // Version count
                    bw.Write((ushort)def.versionDefinitions.Length);

                    // Columns
                    var colList = new List<string>();
                    foreach (var col in def.columnDefinitions)
                    {
                        switch (col.Value.type)
                        {
                            case "int":
                            case "uint":
                                bw.Write((byte)0);
                                break;
                            case "float":
                                bw.Write((byte)1);
                                break;
                            case "string":
                                bw.Write((byte)2);
                                break;
                            case "locstring":
                                bw.Write((byte)3);
                                break;
                            default:
                                throw new NotImplementedException($"Unknown column type {col.Value.type}");
                        }

                        colList.Add(col.Key);

                        bw.Write(col.Value.verified);

                        bw.WriteStringBlockString(stringBlock, col.Key);
                        bw.WriteStringBlockString(stringBlock, col.Value.foreignTable);
                        bw.WriteStringBlockString(stringBlock, col.Value.foreignColumn);
                        bw.WriteStringBlockString(stringBlock, col.Value.comment);
                    }

                    // Versions
                    foreach (var version in def.versionDefinitions)
                    {
                        // Layout hashes
                        bw.Write(version.layoutHashes.Length);
                        foreach (var hash in version.layoutHashes)
                            bw.Write(Convert.ToInt32(hash, 16));

                        // Builds
                        bw.Write(version.builds.Length);
                        foreach (var build in version.builds)
                        {
                            bw.Write((byte)build.expansion);
                            bw.Write((byte)build.major);
                            bw.Write((byte)build.minor);
                            bw.Write(build.build);
                        }

                        // Build ranges
                        bw.Write(version.buildRanges.Length);
                        foreach (var range in version.buildRanges)
                        {
                            bw.Write((byte)range.minBuild.expansion);
                            bw.Write((byte)range.minBuild.major);
                            bw.Write((byte)range.minBuild.minor);
                            bw.Write(range.minBuild.build);
                            bw.Write((byte)range.maxBuild.expansion);
                            bw.Write((byte)range.maxBuild.major);
                            bw.Write((byte)range.maxBuild.minor);
                            bw.Write(range.maxBuild.build);
                        }

                        // Definitions
                        bw.Write(version.definitions.Length);
                        foreach (var defn in version.definitions)
                        {
                            var flags = 0;
                            if (defn.isSigned) flags |= 1;
                            if (defn.isNonInline) flags |= 2;
                            if (defn.isID) flags |= 4;
                            if (defn.isRelation) flags |= 8;

                            bw.Write((byte)flags);
                            bw.Write((byte)defn.size);
                            bw.Write((ushort)colList.IndexOf(defn.name));
                            bw.Write((byte)defn.arrLength);

                            bw.WriteStringBlockString(stringBlock, defn.comment);
                        }

                        // Comment
                        bw.WriteStringBlockString(stringBlock, version.comment);
                    }

                    // Set chunk length
                    var tblPos = bw.BaseStream.Position;
                    bw.BaseStream.Position = lengthPos;
                    bw.Write((uint)(tblPos - lengthPos - 4));
                    bw.BaseStream.Position = tblPos;

                    // Set offset in main offets table
                    if (offsetsPos != 0)
                    {
                        var index = Array.IndexOf([.. dbds.Keys], tableName);
                        bw.BaseStream.Position = offsetsPos + (index * 4);
                        bw.Write((uint)lengthPos - 4);
                        bw.BaseStream.Position = tblPos;
                    }
                }

                if (enumMap != null && enumMap.Count > 0)
                {
                    bw.Write(['E', 'M', 'A', 'P']);
                    bw.Write(enumMap.Count);
                    foreach (var mapping in enumMap)
                    {
                        bw.Write((byte)mapping.meta);
                        bw.WriteStringBlockString(stringBlock, mapping.tableName);
                        bw.WriteStringBlockString(stringBlock, mapping.columnName);

                        if (mapping.arrIndex.HasValue)
                            bw.Write((sbyte)1);
                        else
                            bw.Write((sbyte)-1);

                        bw.WriteStringBlockString(stringBlock, mapping.metaValue);
                        bw.WriteStringBlockString(stringBlock, mapping.conditionalTable);
                        bw.WriteStringBlockString(stringBlock, mapping.conditionalColumn);
                        bw.WriteStringBlockString(stringBlock, mapping.conditionalValue);
                        bw.WriteStringBlockString(stringBlock, mapping.comment);
                    }
                }

                if(enumDefinitions != null && enumDefinitions.Count > 0)
                {
                    bw.Write(['E', 'D', 'F', 'S']);
                    bw.Write(enumDefinitions.Count);

                    foreach(var definition in enumDefinitions)
                    {
                        bw.Write((byte)definition.Value.metaType);
                        bw.WriteStringBlockString(stringBlock, definition.Key);
                        bw.Write(definition.Value.entries.Count);
                        foreach(var entry in definition.Value.entries)
                        {
                            bw.Write(entry.value);
                            bw.WriteStringBlockString(stringBlock, entry.name);
                            bw.WriteStringBlockString(stringBlock, entry.comment);
                            bw.Write(entry.buildRanges.Length);
                            foreach(var range in entry.buildRanges)
                            {
                                bw.Write((byte)range.minBuild.expansion);
                                bw.Write((byte)range.minBuild.major);
                                bw.Write((byte)range.minBuild.minor);
                                bw.Write(range.minBuild.build);
                                bw.Write((byte)range.maxBuild.expansion);
                                bw.Write((byte)range.maxBuild.major);
                                bw.Write((byte)range.maxBuild.minor);
                                bw.Write(range.maxBuild.build);
                            }

                            bw.Write(entry.builds.Length);
                            foreach(var build in entry.builds)
                            {
                                bw.Write((byte)build.expansion);
                                bw.Write((byte)build.major);
                                bw.Write((byte)build.minor);
                                bw.Write(build.build);
                            }
                        }
                    }
                }
            }
        }

        private static uint MakeHash(string name)
        {
            uint[] s_hashtable = new uint[] {
                0x486E26EE, 0xDCAA16B3, 0xE1918EEF, 0x202DAFDB,
                0x341C7DC7, 0x1C365303, 0x40EF2D37, 0x65FD5E49,
                0xD6057177, 0x904ECE93, 0x1C38024F, 0x98FD323B,
                0xE3061AE7, 0xA39B0FA1, 0x9797F25F, 0xE4444563,
            };

            uint v = 0x7fed7fed;
            uint x = 0xeeeeeeee;
            for (int i = 0; i < name.Length; i++)
            {
                byte c = (byte)name[i];
                v += x;
                v ^= s_hashtable[(c >> 4) & 0xf] - s_hashtable[c & 0xf];
                x = x * 33 + v + c + 3;
            }
            return v;
        }
    }
}
