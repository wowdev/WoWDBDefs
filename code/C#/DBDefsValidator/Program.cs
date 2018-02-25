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
                Console.WriteLine(Path.GetFileNameWithoutExtension(file));
                definitionCache.Add(Path.GetFileNameWithoutExtension(file), reader.Read(file));
            }
            Console.WriteLine("Done, press enter to exit");
            Console.ReadLine();
        }
    }
}
