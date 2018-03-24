using DBDefsLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using static DBDefsLib.Structs;

namespace DBDTest
{
    class Program
    {
        public static Dictionary<string, DBDefinition> definitionCache = new Dictionary<string, DBDefinition>();
        public static Dictionary<string, List<string>> duplicateFileLookup = new Dictionary<string, List<string>>();
        public static bool foundError = false;
        
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: <definitionsdir>");
                Environment.Exit(1);
            }

            var definitionDir = args[0];

            var buildList = new List<string>();

            foreach (var file in Directory.GetFiles(definitionDir))
            {
                var reader = new DBDReader();
                definitionCache.Add(Path.GetFileNameWithoutExtension(file).ToLower(), reader.Read(file));

                foreach (var versionDef in definitionCache[Path.GetFileNameWithoutExtension(file).ToLower()].versionDefinitions)
                {
                    foreach (var versionBuild in versionDef.builds)
                    {
                        var buildString = versionBuild.ToString();
                        if (!buildList.Contains(buildString)){
                            buildList.Add(buildString);
                        }
                    }
                }
            }

            foreach (var dir in Directory.GetDirectories("Z:/DBCs/"))
            {
                var buildDir = dir.Replace("Z:/DBCs/", "");
                if (!buildList.Contains(buildDir))
                {
                    continue;
                }

                if (Directory.Exists(Path.Combine(dir, "DBFilesClient"))){
                    foreach (var file in Directory.GetFiles(Path.Combine(dir, "DBFilesClient")))
                    {
                        LoadDBC(file);
                    }
                }
                else
                {
                    foreach (var file in Directory.GetFiles(dir))
                    {
                        LoadDBC(file);
                    }
                }
            }

            if (foundError)
            {
                Environment.Exit(1);
            }
        }

        static void LoadDBC(string filename)
        {
            var name = Path.GetFileNameWithoutExtension(filename);

            if (!definitionCache.ContainsKey(name.ToLower()))
            {
                return;
            }

            var buildDir = Path.GetDirectoryName(filename).Replace("Z:\\DBCs\\", "").Replace("\\DBFilesClient", "");

            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    var md5sum = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "");

                    if (!duplicateFileLookup.ContainsKey(name))
                    {
                        duplicateFileLookup.Add(name, new List<string>());
                        duplicateFileLookup[name].Add(md5sum);
                    }
                    else
                    {
                        if (duplicateFileLookup[name].Contains(md5sum))
                        {
                            return;
                        }
                        else
                        {
                            duplicateFileLookup[name].Add(md5sum);
                        }
                    }
                }
            }

            using (var bin = new BinaryReader(File.OpenRead(filename)))
            {
                if (bin.BaseStream.Length == 0) return;

                var magic = new string(bin.ReadChars(4));

                uint recordCount = 0;
                uint recordSize = 0;
                uint fieldCount = 0;
                uint build = 0;
                string layoutHash = "";

                switch(magic)
                {
                    case "WDBC":
                        var dbcheader = bin.Read<Structs.WDBCHeader>();
                        recordCount = dbcheader.recordCount;
                        recordSize = dbcheader.recordSize;
                        fieldCount = dbcheader.fieldCount;
                        break;
                    case "WDB2":
                        var db2header = bin.Read<Structs.WDB2Header>();
                        recordCount = db2header.recordCount;
                        recordSize = db2header.recordSize;
                        fieldCount = db2header.fieldCount;
                        build = db2header.build;
                        break;
                    case "WDB3":
                        var db3header = bin.Read<Structs.WDB2Header>();
                        recordCount = db3header.recordCount;
                        recordSize = db3header.recordSize;
                        fieldCount = db3header.fieldCount;
                        build = db3header.build;
                        break;
                    case "WDB4":
                        var db4header = bin.Read<Structs.WDB4Header>();
                        recordCount = db4header.recordCount;
                        recordSize = db4header.recordSize;
                        fieldCount = db4header.fieldCount;
                        build = db4header.build;
                        break;
                    case "WDB5":
                        var db5header = bin.Read<Structs.WDB5Header>();
                        recordCount = db5header.recordCount;
                        recordSize = db5header.recordSize;
                        fieldCount = db5header.fieldCount;
                        if (db5header.layoutHash >= 21473 && db5header.layoutHash < 21737)
                        {
                            build = db5header.layoutHash;
                        }
                        else
                        {
                            layoutHash = db5header.layoutHash.ToString("X8");
                        }
                        break;
                    case "WDB6":
                        var db6header = bin.Read<Structs.WDB6Header>();
                        recordCount = db6header.recordCount;
                        recordSize = db6header.recordSize;
                        fieldCount = db6header.fieldCount;
                        layoutHash = db6header.layoutHash.ToString("X8");
                        break;
                    case "WDC1":
                        var dc1header = bin.Read<Structs.WDC1Header>();
                        recordCount = dc1header.recordCount;
                        recordSize = 0; // TODO: Bit shit
                        fieldCount = dc1header.fieldCount;
                        layoutHash = dc1header.layoutHash.ToString("X8");
                        break;
                    case "WDC2":
                        var dc2header = bin.Read<Structs.WDC2Header>();
                        recordCount = dc2header.recordCount;
                        recordSize = 0; // TODO: Bit shit
                        fieldCount = dc2header.fieldCount;
                        layoutHash = dc2header.layoutHash.ToString("X8");
                        break;
                    default:
                        throw new Exception("Unknown DBC type " + magic + " encountered!");
                }

                var dbd = definitionCache[Path.GetFileNameWithoutExtension(filename).ToLower()];
                foreach (var versionDef in dbd.versionDefinitions)
                {
                    foreach (var versionBuild in versionDef.builds)
                    {
                        if (versionBuild.ToString() == buildDir)
                        {
                            // Check field sizes
                            var fields = versionDef.definitions.Length;
                            foreach (var definition in versionDef.definitions)
                            {
                                if (definition.arrLength > 0)
                                {
                                    fields += definition.arrLength;
                                    fields -= 1;
                                }

                                if (definition.isNonInline)
                                {
                                    fields -= 1;
                                }

                                if (definition.isRelation)
                                {
                                    fields -= 1;
                                }

                                if (dbd.columnDefinitions[definition.name].type == "locstring")
                                {
                                    var tempBuild = new Build(buildDir);
                                    if (tempBuild.build < 6692)
                                    {
                                        fields += 8;
                                    }
                                    else if (tempBuild.build > 6692 && (tempBuild.expansion < 4 && tempBuild.build < 11927))
                                    {
                                        fields += 16;
                                    }
                                }
                            }
                            if (fieldCount != fields)
                            {
                                foundError = true;
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("[" + buildDir + "][" + Path.GetFileNameWithoutExtension(filename) + "] Field count wrong! DBC: " + fieldCount + ", DBD: " + fields);
                                Console.ResetColor();
                            }

                            // Check record sizes
                            var dbdRecordSize = 0;
                            foreach (var definition in versionDef.definitions)
                            {
                                if (definition.isNonInline || definition.isRelation) continue;

                                var fieldSize = 0;
                                switch (dbd.columnDefinitions[definition.name].type)
                                {
                                    case "int":
                                    case "uint":
                                        fieldSize = definition.size;
                                        break;
                                    case "string":
                                    case "float":
                                        fieldSize = 32;
                                        break;
                                    case "locstring":
                                        var tempBuild = new Build(buildDir);
                                        if (tempBuild.build < 6692)
                                        {
                                            fieldSize = 32 * 9;
                                        }
                                        else if (tempBuild.build > 6692 && (tempBuild.expansion < 4 && tempBuild.build < 11927))
                                        {
                                            fieldSize = 32 * 17;
                                        }
                                        else
                                        {
                                            fieldSize = 32;
                                        }
                                        break;
                                    default:
                                        throw new Exception("Unknown type: " + dbd.columnDefinitions[definition.name].type + "!");
                                }

                                if(definition.arrLength > 0)
                                {
                                    dbdRecordSize += (fieldSize / 8) * definition.arrLength;
                                }
                                else
                                {

                                    dbdRecordSize += (fieldSize / 8);
                                }
                            }

                            if (recordSize != 0 && recordSize != dbdRecordSize)
                            {
                                foundError = true;
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("[" + buildDir + "][" + Path.GetFileNameWithoutExtension(filename) + "] Record size wrong! DBC: " + recordSize + ", DBD: " + dbdRecordSize);
                                Console.ResetColor();
                            }
                        }
                    }
                }

                //Console.WriteLine("[" + buildDir + "][" + name + "] magic: " + magic + ", fieldcount: " + fieldCount + ", recordcount: " + recordCount + ", build: " + build + ", layoutHash: " + layoutHash);

                if(layoutHash != "")
                {
                    var found = false;
                    foreach (var version in definitionCache[name.ToLower()].versionDefinitions)
                    {
                        foreach (var layouthash in version.layoutHashes)
                        {
                            if(layouthash == layoutHash)
                            {
                                found = true;
                            }
                        }
                    }
                    if (!found) { foundError = true; Console.WriteLine("   Unable to find layoutHash " + layoutHash + " in definitions!"); }
                }
            }
        }
    }
}
