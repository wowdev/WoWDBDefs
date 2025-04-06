using DBDefsLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static DBDefsLib.Structs;

namespace DBDefsConverter
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1 || args.Length > 4)
            {
                throw new ArgumentException("Invalid argument count, need at least 1 argument: indbdfile/indbddir (outdir, default current dir) (json, xml, dbml, bdbd) (build number for dbml export)");
            }

            var inFile = args[0];
            var outDir = Directory.GetCurrentDirectory();
            var exportFormat = "json";
            Build exportBuild = null;

            if (args.Length >= 2)
            {
                outDir = args[1];
                if (!Directory.Exists(outDir))
                {
                    Directory.CreateDirectory(outDir);
                }
            }

            if (args.Length == 3)
            {
                switch (args[2])
                {
                    case "json":
                    case "xml":
                    case "bdbd":
                        exportFormat = args[2];
                        break;
                    default:
                        throw new ArgumentException("Export format should be json or xml");
                }
            }

            if (args.Length == 4)
            {
                if (!args[2].Equals("dbml"))
                {
                    throw new ArgumentException(
                        "Converting to a specific build is only supported for export format dbml");
                }

                exportFormat = args[2];
                var succeededParsingBuild = Build.TryParse(args[3], out var parsedBuild);
                if (!succeededParsingBuild)
                {
                    throw new ArgumentException(
                        "Couldn't parse build. Make sure format looks like this: \"Expansion.Major.Minor.Build\"");
                }

                exportBuild = parsedBuild;
            }

            if (Directory.Exists(inFile))
            {
                var files = Directory.GetFiles(inFile);
                switch (exportFormat)
                {
                    case "xml":
                    case "json":
                        DoExport(exportFormat, outDir, files);
                        break;
                    case "dbml":
                        DoExportDBML(outDir, exportBuild, files);
                        break;
                    case "bdbd":
                        DoExportBDBD(inFile, outDir, files);
                        break;
                }
            }
            else if (File.Exists(inFile))
            {
                switch (exportFormat)
                {
                    case "xml":
                    case "json":
                        DoExport(exportFormat, outDir, inFile);
                        break;
                    case "dbml":
                        DoExportDBML(outDir, exportBuild, inFile);
                        break;
                    case "bdbd":
                        DoExportBDBD(inFile, outDir, inFile);
                        break;
                }
            }
            else
            {
                throw new FileNotFoundException("Unable to find directory/file " + args[0]);
            }
        }

        private static void DoExport(string exportFormat, string outDir, params string[] files)
        {
            var jsonserializer = new JsonSerializer { NullValueHandling = NullValueHandling.Ignore };
            var xmlserializer = new DBDXMLSerializer();

            foreach (var file in files)
            {
                Console.WriteLine("Exporting " + file);

                var reader = new DBDReader();
                var target = Path.Combine(outDir, Path.ChangeExtension(Path.GetFileName(file), exportFormat));

                Console.WriteLine($"Saving {exportFormat.ToUpper()} to {target}");
                using (StreamWriter writer = File.CreateText(target))
                {
                    switch (exportFormat)
                    {
                        case "json":
                            jsonserializer.Serialize(writer, reader.Read(file));
                            break;
                        case "xml":
                            xmlserializer.Serialize(writer, reader.Read(file));
                            break;
                    }
                }
            }
        }

        private static void DoExportDBML(string outDir, Build build, params string[] files)
        {
            var dbmlSerializer = new DBDDBMLSerializer();
            var dbmlDocument = new DBMLDocument
            {
                Project = new DBMLProject
                {
                    Name = $"DBC_{build.expansion}_{build.major}_{build.minor}_{build.build}",
                    Note = $"Database Client tables for World of Warcraft {build}"
                }
            };

            var shippedTables = new List<string>();

            foreach (var file in files)
            {
                Console.WriteLine("Parsing " + file);

                var reader = new DBDReader();
                var dbdIdentifier = Path.GetFileNameWithoutExtension(file);
                var dbDefinition = reader.Read(file);
                var buildDefinition = dbDefinition.versionDefinitions
                    .Where(d => d.builds.Equals(build) ||
                                d.buildRanges.Any(br => br.Contains(build)))
                    .Cast<VersionDefinitions?>()
                    .FirstOrDefault();

                if (buildDefinition is null)
                {
                    continue;
                }

                shippedTables.Add(dbdIdentifier);
                var columns = new List<DBMLColumn>();
                foreach (var definition in buildDefinition.Value.definitions)
                {
                    if (!dbDefinition.columnDefinitions.ContainsKey(definition.name))
                        continue;

                    var columnDefinition = dbDefinition.columnDefinitions[definition.name];
                    var column = new DBMLColumn
                    {
                        Name = definition.name,
                        Type = columnDefinition.type,
                        Settings = new DBMLColumnSettings
                        {
                            IsPrimaryKey = definition.isID,
                            RelationshipType = !string.IsNullOrEmpty(columnDefinition.foreignTable)
                                ? DBMLColumnRelationshipType.OneToMany
                                : DBMLColumnRelationshipType.None,
                            RelationshipTable = columnDefinition.foreignTable,
                            RelationshipColumn = columnDefinition.foreignColumn,
                            Note = columnDefinition.comment
                        }
                    };
                    columns.Add(column);
                }

                var table = new DBMLTable
                {
                    Name = dbdIdentifier,
                    Note = $"{dbdIdentifier} client database table for version {build}",
                    Columns = columns,
                    Schema = "DBC"
                };
                dbmlDocument.Tables.Add(table);
            }

            // Filter out relations of unshipped tables
            foreach (var table in dbmlDocument.Tables)
            {
                foreach (var column in table.Columns)
                {
                    if (shippedTables.Contains(column.Settings.RelationshipTable))
                        continue;
                    column.Settings.RelationshipTable = string.Empty;
                    column.Settings.RelationshipType = DBMLColumnRelationshipType.None;
                }
            }

            // Now we can write our DBML file
            var outFile = $"{build}.dbml";
            Console.WriteLine($"Writing {outFile}");
            var target = Path.Combine(outDir, outFile);
            using var writer = File.CreateText(target);
            dbmlSerializer.Serialize(writer, dbmlDocument);
        }

        private static void DoExportBDBD(string input, string outFile, params string[] files)
        {
            var dbds = new Dictionary<string, DBDefinition>();
            Console.WriteLine("Reading DBDs...");
            foreach (var file in files)
            {
                var reader = new DBDReader();
                dbds.Add(Path.GetFileNameWithoutExtension(file), reader.Read(file));
            }

            var tableNameToDBC = new Dictionary<string, uint>();
            var tableNameToDB2 = new Dictionary<string, uint>();

            if (File.Exists(Path.Combine(Path.Combine("..", Path.GetDirectoryName(input)), "manifest.json")))
            {
                var manifest = File.ReadAllText(Path.Combine(Path.Combine("..", Path.GetDirectoryName(input)), "manifest.json"));
                var manifestObj = JsonConvert.DeserializeObject<dynamic>(manifest);
                foreach (dynamic dbdManifest in manifestObj)
                {
                    if (dbdManifest.dbcFileDataID != null)
                        tableNameToDBC.Add((string)dbdManifest.tableName, (uint)dbdManifest.dbcFileDataID);

                    if (dbdManifest.db2FileDataID != null)
                        tableNameToDB2.Add((string)dbdManifest.tableName, (uint)dbdManifest.db2FileDataID);
                }
            }
            else
            {
                Console.WriteLine("manifest.json not found in directory above input, skipping DBC/DB2 FDID assignment.");
            }

            Console.WriteLine("Done, writing BDBD...");

            var outputPath = outFile;
            if (Directory.Exists(outFile))
                outputPath = Path.Combine(outFile, "out.bdbd");

            var stringBlock = new Dictionary<string, int>();
            foreach (var dbd in dbds)
            {
                if (!stringBlock.ContainsKey(dbd.Key))
                    stringBlock.Add(dbd.Key, 0);

                foreach (var col in dbd.Value.columnDefinitions)
                {
                    if (!stringBlock.ContainsKey(col.Key))
                        stringBlock.Add(col.Key, 0);

                    if (!string.IsNullOrEmpty(col.Value.foreignTable) && !stringBlock.ContainsKey(col.Value.foreignTable))
                        stringBlock.Add(col.Value.foreignTable, 0);

                    if (!string.IsNullOrEmpty(col.Value.foreignColumn) && !stringBlock.ContainsKey(col.Value.foreignColumn))
                        stringBlock.Add(col.Value.foreignColumn, 0);

                    if (!string.IsNullOrEmpty(col.Value.comment) && !stringBlock.ContainsKey(col.Value.comment))
                        stringBlock.Add(col.Value.comment, 0);
                }

                foreach (var version in dbd.Value.versionDefinitions)
                {
                    foreach (var defn in version.definitions)
                    {
                        if (!string.IsNullOrEmpty(defn.comment) && !stringBlock.ContainsKey(defn.comment))
                            stringBlock.Add(defn.comment, 0);
                    }

                    if (!string.IsNullOrEmpty(version.comment) && !stringBlock.ContainsKey(version.comment))
                        stringBlock.Add(version.comment, 0);
                }
            }

            using (var fs = new FileStream(outputPath, FileMode.Truncate))
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
                    bw.Write(tableNameToDBC.TryGetValue(tableName, out var dbcFDID) ? dbcFDID : 0); // DBC FDID
                    bw.Write(tableNameToDB2.TryGetValue(tableName, out var db2FDID) ? db2FDID : 0); // DB2 FDID

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
            }

            Console.WriteLine($"Done, wrote {outputPath}");
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

public static class BinaryWriterExtensions
{
    public static void WriteStringBlockString(this BinaryWriter bw, Dictionary<string, int> stringBlock, string stringToWrite)
    {
        if (string.IsNullOrEmpty(stringToWrite))
        {
            bw.Write(-1);
            return;
        }
         
        if (stringBlock.TryGetValue(stringToWrite, out var stringIndex))
            bw.Write(stringIndex);
        else
            throw new KeyNotFoundException("String not found in stringblock");
    }
}