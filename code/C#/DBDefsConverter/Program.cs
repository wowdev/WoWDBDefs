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
            if (args.Length < 1 || args.Length > 3)
            {
                throw new ArgumentException("Invalid argument count, need at least 1 argument: indbdfile/indbddir (outdir, default current dir) (json, xml)");
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
                switch (args[2])
                {
                    case "json":
                    case "xml":
                        exportFormat = args[2];
                        break;
                    default:
                        throw new ArgumentException("Export format should be json");
                }
            }

            if (Directory.Exists(args[0]))
            {
                var files = Directory.GetFiles(args[0]);
                DoExport(exportFormat, outDir, files);
            }
            else if (File.Exists(args[0]))
            {
                DoExport(exportFormat, outDir, args[0]);
            }
            else
            {
                throw new FileNotFoundException("Unable to find directory/file " + args[0]);
            }
        }

        private static void DoExport(string exportFormat, string outDir, params string[] files)
        {
            JsonSerializer jsonserializer = new JsonSerializer { NullValueHandling = NullValueHandling.Ignore };
            DBDXMLSerializer xmlserializer = new DBDXMLSerializer();

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
    }
}
