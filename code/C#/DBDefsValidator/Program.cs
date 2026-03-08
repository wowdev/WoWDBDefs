using DBDefsLib;
using DBDefsLib.Structs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DBDefsTest
{
    class Program
    {
        public static Dictionary<string, DBDefinition> definitionCache = new Dictionary<string, DBDefinition>();

        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: <definitionsdir> (rewrite when done: bool, default false) (verbose: bool, default true) (rawRepoDir: location of WoWDBDefsRaw repository, default none)");
                Environment.Exit(1);
            }

            var definitionDir = args[0];

            var rewrite = false;
            var verbose = true;
            var checkRaw = false;
            var rawRepoDir = "";

            if (args.Length >= 2 && args[1] == "true")
                rewrite = true;
            if (args.Length >= 3 && args[2] == "false")
                verbose = false;
            if (args.Length >= 4)
            {
                checkRaw = true;
                rawRepoDir = args[3];
            }

            var errorEncountered = new List<string>();

            foreach (var file in Directory.GetFiles(definitionDir))
            {
                var dbName = Path.GetFileNameWithoutExtension(file);

                var reader = new DBDReader();
                try
                {
                    definitionCache.Add(dbName, reader.Read(file, true));

                    if (verbose)
                        Console.WriteLine("Read " + definitionCache[dbName].versionDefinitions.Length + " versions and " + definitionCache[dbName].columnDefinitions.Count + " columns for " + dbName);

                    if (rewrite)
                    {
                        var writer = new DBDWriter();
                        writer.Save(definitionCache[dbName], Path.Combine(definitionDir, dbName + ".dbd"), true);
                    }
                }
                catch (Exception ex)
                {
                    errorEncountered.Add(dbName);
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Failed to read " + dbName + ": " + ex);
                    Console.ResetColor();
                }
            }

            Console.WriteLine("Read " + definitionCache.Count + " database definitions!");

            var foreignKeys = 0;
            foreach (var definition in definitionCache)
            {
                foreach (var columnDefinition in definition.Value.columnDefinitions)
                {
                    if (!string.IsNullOrEmpty(columnDefinition.Value.foreignTable) || !string.IsNullOrEmpty(columnDefinition.Value.foreignColumn))
                    {
                        if (definitionCache.ContainsKey(columnDefinition.Value.foreignTable) && definitionCache[columnDefinition.Value.foreignTable].columnDefinitions.ContainsKey(columnDefinition.Value.foreignColumn))
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            //Console.WriteLine(definition.Key + "." + columnDefinition.Key + " has a foreign key to " + columnDefinition.Value.foreignTable + "." + columnDefinition.Value.foreignColumn);
                        }
                        else
                        {
                            errorEncountered.Add(definition.Key);
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine(definition.Key + "." + columnDefinition.Key + " has a foreign key to " + columnDefinition.Value.foreignTable + "." + columnDefinition.Value.foreignColumn + " WHICH DOES NOT EXIST!");
                        }

                        foreignKeys++;

                        Console.ResetColor();
                    }
                }
            }

            Console.WriteLine("Checked " + foreignKeys + " foreign keys!");

            if (checkRaw)
            {
                Console.WriteLine("Checking for differences between raw definitions and target definitions (limited to 9.0+)");

                if (!Directory.Exists(rawRepoDir))
                {
                    throw new DirectoryNotFoundException("Could not find WoWDBDefsRaw repository");
                }

                foreach (var definition in definitionCache)
                {
                    foreach (var versionDefinition in definition.Value.versionDefinitions)
                    {
                        foreach (var build in versionDefinition.builds)
                        {
                            // TODO: There are issues in older expansions, but limit to 9.0+ for now
                            if (build.expansion < 9)
                                continue;

                            var rawDefLocation = Path.Combine(rawRepoDir, build.ToString(), definition.Key + ".dbd");

                            // Definitions for really old versions aren't in raw repo, we can't check those.
                            if (!File.Exists(rawDefLocation))
                                continue;

                            var rawDefReader = new DBDReader();
                            var rawDef = rawDefReader.Read(rawDefLocation);

                            if (versionDefinition.definitions.Length != rawDef.versionDefinitions[0].definitions.Length)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("[" + definition.Key + "] [" + build + "] Column count mismatch between raw (" + rawDef.versionDefinitions[0].definitions.Length + ") and target definition (" + versionDefinition.definitions.Length + ")");
                                errorEncountered.Add(definition.Key);
                                continue;
                            }

                            for (var i = 0; i < versionDefinition.definitions.Length; i++)
                            {
                                var targetColDef = versionDefinition.definitions[i];
                                var rawColDef = rawDef.versionDefinitions[0].definitions[i];

                                if (rawColDef.isSigned != targetColDef.isSigned)
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine("[" + definition.Key + "] [" + build + "] " + rawColDef.name + " <-> " + targetColDef.name + " signedness mismatch: " + rawColDef.isSigned + " <-> " + targetColDef.isSigned);
                                    errorEncountered.Add(definition.Key);
                                }

                                if (rawColDef.size != targetColDef.size)
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine("[" + definition.Key + "] [" + build + "] " + rawColDef.name + " <-> " + targetColDef.name + " size mismatch: " + rawColDef.size + " <-> " + targetColDef.size);
                                    errorEncountered.Add(definition.Key);
                                }

                                if (rawColDef.arrLength != targetColDef.arrLength)
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine("[" + definition.Key + "] [" + build + "] " + rawColDef.name + " <-> " + targetColDef.name + " array length mismatch: " + rawColDef.arrLength + " <-> " + targetColDef.arrLength);
                                    errorEncountered.Add(definition.Key);
                                }

                                if (rawColDef.isRelation != targetColDef.isRelation)
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine("[" + definition.Key + "] [" + build + "] " + rawColDef.name + " <-> " + targetColDef.name + " relation mismatch: " + rawColDef.isRelation + " <-> " + targetColDef.isRelation);
                                    errorEncountered.Add(definition.Key);
                                }

                                Console.ResetColor();
                            }
                        }
                    }
                }
            }

            errorEncountered = errorEncountered.Distinct().ToList();

            if (errorEncountered.Count != 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("There have been errors in the following DBDs:");
                foreach (var dbName in errorEncountered)
                    Console.WriteLine(" - " + dbName);
                Environment.Exit(1);
            }
            else
            {
                Console.WriteLine("Done");
            }
        }
    }
}
