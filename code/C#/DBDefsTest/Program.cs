using DBDefsLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: <definitionsdir> <dbcdir>");
                Environment.Exit(1);
            }

            var definitionDir = args[0];

            if (!Directory.Exists(definitionDir))
            {
                throw new DirectoryNotFoundException("Directory " + definitionDir + " does not exist!");
            }

            var dbcDir = args[1];

            if (!Directory.Exists(dbcDir))
            {
                throw new DirectoryNotFoundException("Directory " + dbcDir + " does not exist!");
            }

            foreach (var file in Directory.GetFiles(definitionDir))
            {
                var reader = new DBDReader();
                definitionCache.Add(Path.GetFileNameWithoutExtension(file).ToLower(), reader.Read(file));
            }

            var builds = new List<Build>();

            foreach (var dir in Directory.GetDirectories(dbcDir))
            {
                builds.Add(new Build(dir.Replace(dbcDir, "")));
            }

            builds.Sort();

            foreach(var build in builds)
            {
                Console.WriteLine("Checking " + build + "..");
                if (Directory.Exists(Path.Combine(dbcDir, build.ToString(), "DBFilesClient")))
                {
                    foreach (var file in Directory.GetFiles(Path.Combine(dbcDir, build.ToString(), "DBFilesClient")))
                    {
                        LoadDBC(file);
                    }
                }
                else
                {
                    foreach (var file in Directory.GetFiles(Path.Combine(dbcDir, build.ToString())))
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
                var dbChecked = false;
                var layoutHashFound = false;

                foreach (var versionDef in dbd.versionDefinitions)
                {
                    var versionDefMatches = false;

                    foreach (var buildRange in versionDef.buildRanges)
                    {
                        if (buildRange.Contains(new Build(buildDir)))
                        {
                            versionDefMatches = true;
                        }
                    }

                    if (versionDef.builds.Contains(new Build(buildDir)))
                    {
                        versionDefMatches = true;
                    }

                    foreach (var versionLayoutHash in versionDef.layoutHashes)
                    {
                        if (layoutHash != "" && versionLayoutHash == layoutHash)
                        {
                            layoutHashFound = true;
                            versionDefMatches = true;
                        }
                    }

                    if (versionDefMatches)
                    {
                        // Check field sizes
                        var fields = 0;
                        foreach (var definition in versionDef.definitions)
                        {
                            if (definition.name.StartsWith("Padding_") || definition.isNonInline)
                                continue;

                            int arrLength = Math.Max(definition.arrLength, 1);

                            if (definition.isRelation)
                            {
                                fields++;
                                continue;
                            }

                            if (dbd.columnDefinitions[definition.name].type == "locstring")
                            {
                                var tempBuild = new Build(buildDir);
                                if (tempBuild.build < 6692)
                                {
                                    fields += (9 * arrLength);
                                }
                                else if (tempBuild.build >= 6692 && (tempBuild.expansion < 4 && tempBuild.build < 11927))
                                {
                                    fields += (17 * arrLength);
                                }
                                else
                                {
                                    fields += arrLength;
                                }
                            }
                            else
                            {
                                fields += arrLength;
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

                            int arrLength = Math.Max(definition.arrLength, 1);

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
                                        fieldSize = (32 * 9) * arrLength;
                                    }
                                    else if (tempBuild.build >= 6692 && (tempBuild.expansion < 4 && tempBuild.build < 11927))
                                    {
                                        fieldSize = (32 * 17) * arrLength;
                                    }
                                    else
                                    {
                                        fieldSize = 32;
                                    }
                                    break;
                                default:
                                    throw new Exception("Unknown type: " + dbd.columnDefinitions[definition.name].type + "!");
                            }

                            if (definition.arrLength > 0)
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

                        dbChecked = true;

                        break;
                    }
                }

                if (!layoutHashFound && layoutHash != "")
                {
                    foundError = true;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[" + buildDir + "][" + Path.GetFileNameWithoutExtension(filename) + "] Unable to find layouthash in definitions!");
                    Console.ResetColor();
                }

                if (!dbChecked)
                {
                    foundError = true;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[" + buildDir + "][" + Path.GetFileNameWithoutExtension(filename) + "] Unable to find applicable definitions!");
                    Console.ResetColor();
                }
            }
        }
    }
}
