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

        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: <definitionsdir>");
                Environment.Exit(1);
            }

            var definitionDir = args[0];

            foreach (var file in Directory.GetFiles(definitionDir))
            {
                var reader = new DBDReader();
                definitionCache.Add(Path.GetFileNameWithoutExtension(file).ToLower(), reader.Read(file));
            }

            foreach (var dir in Directory.GetDirectories("Z:/DBCs/"))
            {
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

            Console.ReadLine();
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
                uint fieldCount = 0;
                uint build = 0;
                string layoutHash = "";

                switch(magic)
                {
                    case "WDBC":
                        var dbcheader = bin.Read<Structs.WDBCHeader>();
                        recordCount = dbcheader.recordCount;
                        fieldCount = dbcheader.fieldCount;
                        break;
                    case "WDB2":
                        var db2header = bin.Read<Structs.WDB2Header>();
                        recordCount = db2header.recordCount;
                        fieldCount = db2header.fieldCount;
                        build = db2header.build;
                        break;
                    case "WDB3":
                        var db3header = bin.Read<Structs.WDB2Header>();
                        recordCount = db3header.recordCount;
                        fieldCount = db3header.fieldCount;
                        build = db3header.build;
                        break;
                    case "WDB4":
                        var db4header = bin.Read<Structs.WDB4Header>();
                        recordCount = db4header.recordCount;
                        fieldCount = db4header.fieldCount;
                        build = db4header.build;
                        break;
                    case "WDB5":
                        var db5header = bin.Read<Structs.WDB5Header>();
                        recordCount = db5header.recordCount;
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
                        fieldCount = db6header.fieldCount;
                        layoutHash = db6header.layoutHash.ToString("X8");
                        break;
                    case "WDC1":
                        var dc1header = bin.Read<Structs.WDC1Header>();
                        recordCount = dc1header.recordCount;
                        fieldCount = dc1header.fieldCount;
                        layoutHash = dc1header.layoutHash.ToString("X8");
                        break;
                    default:
                        throw new Exception("Unknown DBC type " + magic + " encountered!");
                }

                Console.WriteLine("Loaded " + name + " from build dir " + buildDir + " (magic: " + magic + ", fieldcount: " + fieldCount + ", recordcount: " + recordCount + ", build: " + build + ", layoutHash: " + layoutHash + ")");

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
                    if (!found) { Console.WriteLine("   Unable to find layoutHash " + layoutHash + " in definitions!"); }
                }
            }
        }
    }
}
