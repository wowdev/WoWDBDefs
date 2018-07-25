using DBDefsLib;
using Newtonsoft.Json;
using System;
using System.IO;
using static DBDefsLib.Structs;

namespace DBDefsConverter
{
    class Program
    {
        static void Main(string[] args)
        {
            if(args.Length != 1 || args.Length > 3)
            {
                throw new ArgumentException("Invalid argument count, need at least 1 argument: indbdfile/indbddir (outdir, default current dir) (json/xml, default:json)");
            }

            var inFile = args[0];
            var outDir = Directory.GetCurrentDirectory();
            var exportFormat = "json";

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
                if (args[2] == "json" || args[2] == "xml")
                {
                    exportFormat = args[2];
                }
                else
                {
                    throw new ArgumentException("Export format should be either json or xml");
                }
            }

            if (Directory.Exists(args[0]))
            {
                foreach (var file in Directory.GetFiles(args[0]))
                {
                    var reader = new DBDReader();
                    Console.WriteLine("Exporting " + file);
                    if (exportFormat == "json")
                    {
                        var target = Path.Combine(outDir, Path.GetFileNameWithoutExtension(file) + ".json"); ;
                        ExportJSON(reader.Read(file), target);
                    }
                    else if (exportFormat == "xml")
                    {
                        var target = Path.Combine(outDir, Path.GetFileNameWithoutExtension(file) + ".xml"); ;
                        ExportXML(reader.Read(file), target);
                    }
                }
            }
            else if (File.Exists(args[0]))
            {
                var reader = new DBDReader();
                Console.WriteLine("Exporting " + args[0]);
                if (exportFormat == "json")
                {
                    var target = Path.Combine(outDir, Path.GetFileNameWithoutExtension(args[0]) + ".json"); ;
                    ExportJSON(reader.Read(args[0]), target);
                }
                else if (exportFormat == "xml")
                {
                    var target = Path.Combine(outDir, Path.GetFileNameWithoutExtension(args[0]) + ".xml"); ;
                    ExportXML(reader.Read(args[0]), target);
                }
            }
            else
            {
                throw new FileNotFoundException("Unable to find directory/file " + args[0]);
            }
        }

        private static void ExportJSON(DBDefinition definition, string target)
        {
            Console.WriteLine("Saving JSON to " + target);
            using (StreamWriter file = File.CreateText(target))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.NullValueHandling = NullValueHandling.Ignore;
                serializer.Serialize(file, definition);
            }
        }

        private static void ExportXML(DBDefinition definition, string target)
        {
            throw new NotImplementedException();
        }
    }
}
