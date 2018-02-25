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
            foreach (var file in Directory.GetFiles("../../../definitions/"))
            {
                var reader = new DBDReader();
                definitionCache.Add(Path.GetFileNameWithoutExtension(file), reader.Read(file));
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
