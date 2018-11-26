using DBDefsLib;
using DBDTest.Structs;
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
        
        private static string dbcDir;

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

            dbcDir = args[1];

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
                builds.Add(new Build(dir.Replace(dbcDir + "\\", "")));
            }

            builds.Sort();

            foreach(var build in builds)
            {
                if (build.expansion != 8 || build.major != 1) continue;

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

            var buildDir = Path.GetDirectoryName(filename).Replace(dbcDir + "\\", "").Replace("\\DBFilesClient", "");

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

                var fileDef = new FileDefinition();

                switch(magic)
                {
                    case "WDBC":
                        var dbcheader = bin.Read<WDBCHeader>();
                        fileDef.recordSize = dbcheader.recordSize;
                        fileDef.fieldCount = dbcheader.fieldCount;
                        break;
                    case "WDB2":
                        var db2header = bin.Read<WDB2Header>();
                        fileDef.recordSize = db2header.recordSize;
                        fileDef.fieldCount = db2header.fieldCount;
                        fileDef.build = db2header.build;
                        break;
                    case "WDB3":
                        var db3header = bin.Read<WDB2Header>();
                        fileDef.recordSize = db3header.recordSize;
                        fileDef.fieldCount = db3header.fieldCount;
                        fileDef.build = db3header.build;
                        break;
                    case "WDB4":
                        var db4header = bin.Read<WDB4Header>();
                        fileDef.recordSize = db4header.recordSize;
                        fileDef.fieldCount = db4header.fieldCount;
                        fileDef.build = db4header.build;
                        break;
                    case "WDB5":
                        var db5header = bin.Read<WDB5Header>();
                        fileDef.recordSize = db5header.recordSize;
                        fileDef.fieldCount = db5header.fieldCount;
                        if (db5header.layoutHash >= 21473 && db5header.layoutHash < 21737)
                        {
                            fileDef.build = db5header.layoutHash;
                        }
                        else
                        {
                            fileDef.layoutHash = db5header.layoutHash.ToString("X8");
                        }
                        break;
                    case "WDB6":
                        var db6header = bin.Read<WDB6Header>();
                        fileDef.recordSize = db6header.recordSize;
                        fileDef.fieldCount = db6header.fieldCount;
                        fileDef.layoutHash = db6header.layoutHash.ToString("X8");
                        break;
                    case "WDC1":
                        var dc1header = bin.Read<WDC1Header>();
                        fileDef.recordSize = 0; // TODO: Bit shit
                        fileDef.fieldCount = dc1header.fieldCount;
                        fileDef.layoutHash = dc1header.layoutHash.ToString("X8");
                        fileDef.fields = new FieldStructure[fileDef.fieldCount];
                        if (dc1header.totalFieldCount != dc1header.fieldCount)
                        {
                            throw new Exception("WDC1 field count (" + dc1header.fieldCount + ") and total field count (" + dc1header.totalFieldCount + " ) do not match!");
                        }
                        break;
                    case "WDC2":
                        var dc2header = bin.Read<WDC2Header>();
                        fileDef.recordSize = 0; // TODO: Bit shit
                        fileDef.fieldCount = dc2header.fieldCount;
                        fileDef.layoutHash = dc2header.layoutHash.ToString("X8");
                        fileDef.fields = new FieldStructure[fileDef.fieldCount];
                        if (dc2header.totalFieldCount != dc2header.fieldCount)
                        {
                            throw new Exception("WDC2 field count (" + dc2header.fieldCount + ") and total field count (" + dc2header.totalFieldCount + " ) do not match!");
                        }
                        break;
                    case "WDC3":
                        var dc3header = bin.Read<WDC3Header>();
                        fileDef.recordSize = 0; // TODO: Bit shit
                        fileDef.fieldCount = dc3header.fieldCount;
                        fileDef.layoutHash = dc3header.layoutHash.ToString("X8");
                        fileDef.fields = new FieldStructure[fileDef.fieldCount];
                        if (dc3header.totalFieldCount != dc3header.fieldCount)
                        {
                            throw new Exception("WDC3 field count (" + dc3header.fieldCount + ") and total field count (" + dc3header.totalFieldCount + " ) do not match!");
                        }

                        bin.BaseStream.Position += (40 * dc3header.sectionCount);

                        for (var i = 0; i < fileDef.fieldCount; i++)
                        {
                            var size = bin.ReadUInt16();
                            var offset = bin.ReadUInt16();
                            //Console.WriteLine("[Structure][Field " + i + "] Size: " + size + ", Offset: " + offset);
                        }

                        for(var i = 0; i < dc3header.fieldStorageInfoSize / 24; i++)
                        {
                            var offset = bin.ReadUInt16();
                            var size = bin.ReadUInt16();
                            var additionalDataSize = bin.ReadUInt32();
                            var type = bin.ReadUInt32();
                            //Console.WriteLine("[Storage][Field " + i + "] Offset: " + bin.ReadUInt16() + ", Size: " + bin.ReadUInt16() + ", Additional Data Size: " + bin.ReadUInt32() + ", Type: " + bin.ReadUInt32());
                            var val1 = bin.ReadUInt32();
                            var val2 = bin.ReadUInt32();
                            var val3 = bin.ReadUInt32();
                            if (type == 4)
                            {
                                fileDef.fields[i].arrayCount = val3;
                            }
                            //Console.WriteLine("[Storage][Field " + i + "] " + val1 + ", " + val2 + ", " + val3);
                        }

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
                        if (fileDef.layoutHash != "" && versionLayoutHash == fileDef.layoutHash)
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
                            var tempBuild = new Build(buildDir);

                            if (definition.name.StartsWith("Padding_") || definition.isNonInline)
                                continue;

                            int arrLength = Math.Max(definition.arrLength, 1);

                            if (definition.isRelation || tempBuild.build > 21478) // As of WDB5, DBC field_count counts arrays as 1
                            {
                                fields++;
                                continue;
                            }

                            if (dbd.columnDefinitions[definition.name].type == "locstring")
                            {
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

                        if (fileDef.fieldCount != fields)
                        {
                            foundError = true;
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("[" + buildDir + "][" + Path.GetFileNameWithoutExtension(filename) + "] Field count wrong! DBC: " + fileDef.fieldCount + ", DBD: " + fields);
                            Console.ResetColor();
                        }

                        // Check record sizes
                        var dbdRecordSize = 0;
                        var i = 0;
                        foreach (var definition in versionDef.definitions)
                        {
                            if (definition.isNonInline || definition.isRelation) continue;

                            int arrLength = Math.Max(definition.arrLength, 1);

                            if(arrLength > 1)
                            {
                                if(fileDef.fields[i].arrayCount != 0 && fileDef.fields[i].arrayCount != arrLength)
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine("[" + buildDir + "][" + Path.GetFileNameWithoutExtension(filename) + "] Array length for field " + definition.name + " wrong! DBC: " + fileDef.fields[i].arrayCount + ", DBD: " + arrLength);
                                    Console.ResetColor();
                                }
                            }

                            i++;

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

                        if (fileDef.recordSize != 0 && fileDef.recordSize != dbdRecordSize)
                        {
                            foundError = true;
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("[" + buildDir + "][" + Path.GetFileNameWithoutExtension(filename) + "] Record size wrong! DBC: " + fileDef.recordSize + ", DBD: " + dbdRecordSize);
                            Console.ResetColor();
                        }

                        dbChecked = true;

                        break;
                    }
                }

                if (!layoutHashFound && fileDef.layoutHash != "")
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
