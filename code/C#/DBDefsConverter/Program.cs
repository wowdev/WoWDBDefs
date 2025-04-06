using DBDefsLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static DBDefsLib.Structs;

namespace DBDefsConverter
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1 || args.Length > 4)
            {
                throw new ArgumentException("Invalid argument count, need at least 1 argument: indbdfile/indbddir (outdir, default current dir) (json, xml, dbml, bdbd) (build number for dbml export)");
            }

            var inFile = args[0];
            var outDir = Directory.GetCurrentDirectory();
            var exportFormat = "json";
            Build exportBuild = null;

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
                    case "bdbd":
                        exportFormat = args[2];
                        break;
                    default:
                        throw new ArgumentException("Export format should be json or xml");
                }
            }

            if (args.Length == 4)
            {
                if (!args[2].Equals("dbml"))
                {
                    throw new ArgumentException(
                        "Converting to a specific build is only supported for export format dbml");
                }

                exportFormat = args[2];
                var succeededParsingBuild = Build.TryParse(args[3], out var parsedBuild);
                if (!succeededParsingBuild)
                {
                    throw new ArgumentException(
                        "Couldn't parse build. Make sure format looks like this: \"Expansion.Major.Minor.Build\"");
                }

                exportBuild = parsedBuild;
            }

            if (Directory.Exists(inFile))
            {
                var files = Directory.GetFiles(inFile);
                switch (exportFormat)
                {
                    case "xml":
                    case "json":
                        DoExport(exportFormat, outDir, files);
                        break;
                    case "dbml":
                        DoExportDBML(outDir, exportBuild, files);
                        break;
                    case "bdbd":
                        DoExportBDBD(inFile, outDir, files);
                        break;
                }
            }
            else if (File.Exists(inFile))
            {
                switch (exportFormat)
                {
                    case "xml":
                    case "json":
                        DoExport(exportFormat, outDir, inFile);
                        break;
                    case "dbml":
                        DoExportDBML(outDir, exportBuild, inFile);
                        break;
                    case "bdbd":
                        DoExportBDBD(inFile, outDir, inFile);
                        break;
                }
            }
            else
            {
                throw new FileNotFoundException("Unable to find directory/file " + args[0]);
            }
        }

        private static void DoExport(string exportFormat, string outDir, params string[] files)
        {
            var jsonserializer = new JsonSerializer { NullValueHandling = NullValueHandling.Ignore };
            var xmlserializer = new DBDXMLSerializer();

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

        private static void DoExportDBML(string outDir, Build build, params string[] files)
        {
            var dbmlSerializer = new DBDDBMLSerializer();
            var dbmlDocument = new DBMLDocument
            {
                Project = new DBMLProject
                {
                    Name = $"DBC_{build.expansion}_{build.major}_{build.minor}_{build.build}",
                    Note = $"Database Client tables for World of Warcraft {build}"
                }
            };

            var shippedTables = new List<string>();

            foreach (var file in files)
            {
                Console.WriteLine("Parsing " + file);

                var reader = new DBDReader();
                var dbdIdentifier = Path.GetFileNameWithoutExtension(file);
                var dbDefinition = reader.Read(file);
                var buildDefinition = dbDefinition.versionDefinitions
                    .Where(d => d.builds.Equals(build) ||
                                d.buildRanges.Any(br => br.Contains(build)))
                    .Cast<VersionDefinitions?>()
                    .FirstOrDefault();

                if (buildDefinition is null)
                {
                    continue;
                }

                shippedTables.Add(dbdIdentifier);
                var columns = new List<DBMLColumn>();
                foreach (var definition in buildDefinition.Value.definitions)
                {
                    if (!dbDefinition.columnDefinitions.ContainsKey(definition.name))
                        continue;

                    var columnDefinition = dbDefinition.columnDefinitions[definition.name];
                    var column = new DBMLColumn
                    {
                        Name = definition.name,
                        Type = columnDefinition.type,
                        Settings = new DBMLColumnSettings
                        {
                            IsPrimaryKey = definition.isID,
                            RelationshipType = !string.IsNullOrEmpty(columnDefinition.foreignTable)
                                ? DBMLColumnRelationshipType.OneToMany
                                : DBMLColumnRelationshipType.None,
                            RelationshipTable = columnDefinition.foreignTable,
                            RelationshipColumn = columnDefinition.foreignColumn,
                            Note = columnDefinition.comment
                        }
                    };
                    columns.Add(column);
                }

                var table = new DBMLTable
                {
                    Name = dbdIdentifier,
                    Note = $"{dbdIdentifier} client database table for version {build}",
                    Columns = columns,
                    Schema = "DBC"
                };
                dbmlDocument.Tables.Add(table);
            }

            // Filter out relations of unshipped tables
            foreach (var table in dbmlDocument.Tables)
            {
                foreach (var column in table.Columns)
                {
                    if (shippedTables.Contains(column.Settings.RelationshipTable))
                        continue;
                    column.Settings.RelationshipTable = string.Empty;
                    column.Settings.RelationshipType = DBMLColumnRelationshipType.None;
                }
            }

            // Now we can write our DBML file
            var outFile = $"{build}.dbml";
            Console.WriteLine($"Writing {outFile}");
            var target = Path.Combine(outDir, outFile);
            using var writer = File.CreateText(target);
            dbmlSerializer.Serialize(writer, dbmlDocument);
        }

        private static void DoExportBDBD(string input, string outFile, params string[] files)
        {
            var dbds = new Dictionary<string, DBDefinition>();
            Console.WriteLine("Reading DBDs...");
            foreach (var file in files)
            {
                var reader = new DBDReader();
                dbds.Add(Path.GetFileNameWithoutExtension(file), reader.Read(file));
            }

            var tableNameToDBC = new Dictionary<string, uint>();
            var tableNameToDB2 = new Dictionary<string, uint>();

            if (File.Exists(Path.Combine(Path.Combine("..", Path.GetDirectoryName(input)), "manifest.json")))
            {
                var manifest = File.ReadAllText(Path.Combine(Path.Combine("..", Path.GetDirectoryName(input)), "manifest.json"));
                var manifestObj = JsonConvert.DeserializeObject<dynamic>(manifest);
                foreach (dynamic dbdManifest in manifestObj)
                {
                    if (dbdManifest.dbcFileDataID != null)
                        tableNameToDBC.Add((string)dbdManifest.tableName, (uint)dbdManifest.dbcFileDataID);

                    if (dbdManifest.db2FileDataID != null)
                        tableNameToDB2.Add((string)dbdManifest.tableName, (uint)dbdManifest.db2FileDataID);
                }
            }
            else
            {
                Console.WriteLine("manifest.json not found in directory above input, skipping DBC/DB2 FDID assignment.");
            }

            Console.WriteLine("Done, writing BDBD...");

            var outputPath = outFile;
            if (Directory.Exists(outFile))
                outputPath = Path.Combine(outFile, "out.bdbd");

            BDBDWriter.Save(dbds, outputPath, tableNameToDBC, tableNameToDB2);

            Console.WriteLine($"Done, wrote {outputPath}");
        }
    }
}
