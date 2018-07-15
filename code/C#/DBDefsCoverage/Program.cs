using DBDefsLib;
using System;
using System.IO;
using System.Linq;
using CsvHelper;

namespace DBDefsCoverage
{
    class Program
    {
        static void Main(string[] args)
        {
            if (!Directory.Exists(args[0]))
            {
                throw new DirectoryNotFoundException("Directory " + args[0] + " does not exist!");
            }

            var builds = File.ReadAllLines("builds.txt");
            var files = Directory.GetFiles(args[0]);

            var textWriter = new StreamWriter("output.csv");
            var csv = new CsvWriter(textWriter);

            csv.WriteField("");

            for (var b = 0; b < builds.Length; b++)
            {
                string rotatedString = "";
                foreach(var character in builds[b]){
                    rotatedString += character + Environment.NewLine;
                }
                csv.WriteField(rotatedString);
            }

            csv.NextRecord();

            for (var f = 0; f < files.Length; f++)
            {
                var file = files[f];
                var dbName = Path.GetFileNameWithoutExtension(file);

                var reader = new DBDReader();
                var dbd = reader.Read(file);

                csv.WriteField(dbName);

                for (var b = 0; b < builds.Length; b++)
                {
                    var containsBuild = false;
                    var build = new Build(builds[b]);

                    foreach(var versionDefinition in dbd.versionDefinitions)
                    {
                        if (versionDefinition.builds.Contains(build)){
                            containsBuild = true;
                        }

                        foreach(var buildRange in versionDefinition.buildRanges)
                        {
                            if(buildRange.Contains(build))
                            {
                                containsBuild = true;
                            }
                        }
                    }

                    if (containsBuild)
                    {
                        csv.WriteField("X");
                    }
                    else
                    {
                        csv.WriteField("");
                    }
                }

                csv.NextRecord();
            }

            csv.Flush();    
        }
    }
}
