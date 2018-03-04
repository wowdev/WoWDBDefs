using DBDefsLib;
using System;
using System.Collections.Generic;
using System.IO;
using static DBDefsLib.Structs;

namespace DBDefsTest
{
    class Program
    {
        public static Dictionary<string, DBDefinition> definitionCache = new Dictionary<string, DBDefinition>();

        static void Main(string[] args)
        {
            if(args.Length < 1)
            {
                Console.WriteLine("Usage: <definitionsdir>");
                Environment.Exit(1);
            }

            var definitionDir = args[0];

            var rewrite = false;

            foreach (var file in Directory.GetFiles(definitionDir))
            {
                var reader = new DBDReader();
                definitionCache.Add(Path.GetFileNameWithoutExtension(file), reader.Read(file));

                if (rewrite)
                {
                    var writer = new DBDWriter();
                    writer.Save(definitionCache[Path.GetFileNameWithoutExtension(file)], Path.Combine(definitionDir, Path.GetFileNameWithoutExtension(file) + ".dbd"));
                }
            }

            Console.WriteLine("Read " + definitionCache.Count + " database definitions!");

            var foreignKeys = 0;
            foreach(var definition in definitionCache)
            {
                foreach(var columnDefinition in definition.Value.columnDefinitions)
                {
                    if (!string.IsNullOrEmpty(columnDefinition.Value.foreignTable) || !string.IsNullOrEmpty(columnDefinition.Value.foreignColumn))
                    {
                        if(definitionCache.ContainsKey(columnDefinition.Value.foreignTable) && definitionCache[columnDefinition.Value.foreignTable].columnDefinitions.ContainsKey(columnDefinition.Value.foreignColumn))
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            //Console.WriteLine(definition.Key + "." + columnDefinition.Key + " has a foreign key to " + columnDefinition.Value.foreignTable + "." + columnDefinition.Value.foreignColumn);
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine(definition.Key + "." + columnDefinition.Key + " has a foreign key to " + columnDefinition.Value.foreignTable + "." + columnDefinition.Value.foreignColumn + " WHICH DOES NOT EXIST!");
                        }

                        foreignKeys++;

                        Console.ResetColor();
                    }
                }
            }

            Console.WriteLine("Checked " + foreignKeys + " foreign keys!");

            Console.WriteLine("Done, press enter to exit");
            Console.ReadLine();
        }
    }
}
