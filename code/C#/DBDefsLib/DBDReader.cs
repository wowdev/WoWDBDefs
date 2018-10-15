using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static DBDefsLib.Structs;

namespace DBDefsLib
{
    public class DBDReader
    {
        public DBDefinition Read(string file, bool validate = false)
        {
            if (!File.Exists(file))
            {
                throw new FileNotFoundException("Unable to find definitions file: " + file);
            }

            var columnDefinitionDictionary = new Dictionary<string, ColumnDefinition>();

            var lines = File.ReadAllLines(file);
            var lineNumber = 0;

            if (lines[0].StartsWith("COLUMNS"))
            {
                lineNumber++;
                while (true)
                {
                    var line = lines[lineNumber++];

                    // Column definitions are done after encountering a newline
                    if (string.IsNullOrWhiteSpace(line)) break;

                    // Create a new column definition to store information in
                    var columnDefinition = new ColumnDefinition();

                    /* TYPE READING */
                    // List of valid types, uint should be removed soon-ish 
                    var validTypes = new List<string> { "uint", "int", "float", "string", "locstring" };

                    // Check if line has a space in case someone didn't assign a type to a column name
                    if(!line.Contains(" "))
                    {
                        throw new Exception("Line " + line + " in file " + Path.GetFileNameWithoutExtension(file) + " does not contain a space between type and column name!");
                    }

                    // Read line up to space (end of type) or < (foreign key)
                    var type = line.Substring(0, line.IndexOfAny(new char[] { ' ', '<' }));

                    // Check if type is valid, throw exception if not!
                    if (!validTypes.Contains(type))
                    {
                        throw new Exception("Invalid type: " + type + " on line " + lineNumber);
                    }
                    else
                    {
                        columnDefinition.type = type;
                    }

                    /* FOREIGN KEY READING */
                    // Only read foreign key if foreign key identifier is found right after type (it could also be in comments)
                    if (line.StartsWith(type + "<"))
                    {
                        // Read foreign key info between < and > without < and > in result, then split on :: to separate table and field
                        var foreignKey = line.Substring(line.IndexOf('<') + 1, line.IndexOf('>') - line.IndexOf('<') - 1).Split(new string[] { "::" }, StringSplitOptions.None);

                        // There should only be 2 values in foreignKey (table and col)
                        if(foreignKey.Length != 2)
                        {
                            throw new Exception("Invalid foreign key length: " + foreignKey.Length);
                        }
                        else
                        {
                            columnDefinition.foreignTable = foreignKey[0];
                            columnDefinition.foreignColumn = foreignKey[1];
                        }
                    }

                    /* NAME READING */
                    var name = "";
                    // If there's only one space on the line at the same locaiton as the first one, assume a simple line like "uint ID", this can be better
                    if(line.LastIndexOf(' ') == line.IndexOf(' '))
                    {
                        name = line.Substring(line.IndexOf(' ') + 1);
                    }
                    else
                    {
                        // Location of first space (after type)
                        var start = line.IndexOf(' ');

                        // Second space (after name)
                        var end = line.IndexOf(' ', start + 1) - start - 1;

                        name = line.Substring(start + 1, end);
                    }

                    // If name ends in ? it's unverified
                    if (name.EndsWith("?"))
                    {
                        columnDefinition.verified = false;
                        name = name.Remove(name.Length - 1);
                    }
                    else
                    {
                        columnDefinition.verified = true;
                    }

                    /* COMMENT READING */
                    if (line.Contains("//"))
                    {
                        columnDefinition.comment = line.Substring(line.IndexOf("//") + 2).Trim();
                    }

                    // Add to dictionary
                    if (columnDefinitionDictionary.ContainsKey(name))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Collision with existing column name while adding new column name! Skipping..");
                        Console.ResetColor();
                    }
                    else
                    {
                        columnDefinitionDictionary.Add(name, columnDefinition);
                    }
                }
            }
            else
            {
                throw new Exception("File does not start with column definitions!");
            }

            // There will be less comments from this point on, stuff used in above code is mostly repeated

            var versionDefinitions = new List<VersionDefinitions>();

            var definitions = new List<Definition>();
            var layoutHashes = new List<string>();
            var comment = "";
            var builds = new List<Build>();
            var buildRanges = new List<BuildRange>();

            for(var i = lineNumber; i < lines.Length; i++)
            {
                var line = lines[i];

                if (string.IsNullOrWhiteSpace(line))
                {
                    versionDefinitions.Add(
                        new VersionDefinitions()
                        {
                            builds = builds.ToArray(),
                            buildRanges = buildRanges.ToArray(),
                            layoutHashes = layoutHashes.ToArray(),
                            comment = comment,
                            definitions = definitions.ToArray()
                        }
                    );

                    definitions = new List<Definition>();
                    layoutHashes = new List<string>();
                    comment = "";
                    builds = new List<Build>();
                    buildRanges = new List<BuildRange>();
                }

                if (line.StartsWith("LAYOUT")){
                    var splitLayoutHashes = line.Remove(0, 7).Split(new string[] { ", " }, StringSplitOptions.None);
                    layoutHashes.AddRange(splitLayoutHashes);
                }

                if (line.StartsWith("BUILD"))
                {
                    var splitBuilds = line.Remove(0, 6).Split(new string[] { ", " }, StringSplitOptions.None);
                    foreach(var splitBuild in splitBuilds)
                    {
                        if (splitBuild.Contains("-"))
                        {
                            var splitRange = splitBuild.Split('-');
                            buildRanges.Add(
                                new BuildRange(new Build(splitRange[0]), new Build(splitRange[1]))
                            );
                        }
                        else{
                            var build = new Build(splitBuild);
                            builds.Add(build);
                        }
                    }
                }

                if (line.StartsWith("COMMENT"))
                {
                    comment = line.Substring(7).Trim();
                }

                if (!line.StartsWith("LAYOUT") && !line.StartsWith("BUILD") && !line.StartsWith("COMMENT") && !string.IsNullOrWhiteSpace(line))
                {
                    var definition = new Definition();

                    // Default to everything being inline
                    definition.isNonInline = false;

                    if (line.Contains("$"))
                    {
                        var annotationStart = line.IndexOf("$");
                        var annotationEnd = line.IndexOf("$", 1);

                        var annotations = new List<string>(line.Substring(annotationStart + 1, annotationEnd - annotationStart - 1).Split(','));

                        if (annotations.Contains("id"))
                        {
                            definition.isID = true;
                        }

                        if (annotations.Contains("noninline"))
                        {
                            definition.isNonInline = true;
                        }

                        if (annotations.Contains("relation"))
                        {
                            definition.isRelation = true;
                        }

                        line = line.Remove(annotationStart, annotationEnd + 1);
                    }

                    if (line.Contains("<"))
                    {
                        var size = line.Substring(line.IndexOf('<') + 1, line.IndexOf('>') - line.IndexOf('<') - 1);

                        if (size[0] == 'u')
                        {
                            definition.isSigned = false;
                            definition.size = int.Parse(size.Replace("u", ""));
                        }
                        else
                        {
                            definition.isSigned = true;
                            definition.size = int.Parse(size);
                        }

                        line = line.Remove(line.IndexOf('<'), line.IndexOf('>') - line.IndexOf('<') + 1);
                    }

                    if (line.Contains("["))
                    {
                        int.TryParse(line.Substring(line.IndexOf('[') + 1, line.IndexOf(']') - line.IndexOf('[') - 1), out definition.arrLength);
                        line = line.Remove(line.IndexOf('['), line.IndexOf(']') - line.IndexOf('[') + 1);
                    }

                    if (line.Contains("//"))
                    {
                        definition.comment = line.Substring(line.IndexOf("//") + 2).Trim();
                        line = line.Remove(line.IndexOf("//")).Trim();
                    }

                    definition.name = line;

                    // Check if this column name is known in column definitions, if not throw exception
                    if (!columnDefinitionDictionary.ContainsKey(definition.name))
                    {
                        throw new KeyNotFoundException("Unable to find " + definition.name + " in column definitions!");
                    }
                    else
                    {
                        // Temporary unsigned format update conversion code
                        if(columnDefinitionDictionary[definition.name].type == "uint") {
                            definition.isSigned = false;
                        }
                    }

                    definitions.Add(definition);
                }

                if (lines.Length == (i + 1))
                {
                    versionDefinitions.Add(
                        new VersionDefinitions()
                        {
                            builds = builds.ToArray(),
                            buildRanges = buildRanges.ToArray(),
                            layoutHashes = layoutHashes.ToArray(),
                            comment = comment,
                            definitions = definitions.ToArray()
                        }
                    );
                }
            }

            // Validation is optional!
            if (validate)
            {
                var newColumnDefDict = new Dictionary<string, ColumnDefinition>();
                foreach (var column in columnDefinitionDictionary)
                {
                    newColumnDefDict.Add(column.Key, column.Value);
                }

                var seenBuilds = new List<Build>();
                var seenLayoutHashes = new List<string>();

                foreach (var column in columnDefinitionDictionary)
                {
                    var found = false;

                    foreach (var version in versionDefinitions)
                    {
                        foreach (var definition in version.definitions)
                        {
                            if (column.Key == definition.name)
                            {
                                if(definition.name == "ID" && !definition.isID)
                                {
                                    Console.ForegroundColor = ConsoleColor.Yellow;
                                    Console.WriteLine(Path.GetFileNameWithoutExtension(file) + "." + definition.name + " is called ID and might be a primary key.");
                                    Console.ResetColor();
                                }
                                found = true;
                                break;
                            }
                        }
                    }

                    if (!found)
                    {
                        Console.WriteLine("Column definition " + column.Key + " is never used in version definitions!");
                        newColumnDefDict.Remove(column.Key);
                    }
                }

                columnDefinitionDictionary = newColumnDefDict;

                foreach (var version in versionDefinitions)
                {
                    foreach (var build in version.builds)
                    {
                        if (seenBuilds.Contains(build))
                        {
                            throw new Exception("Build " + build.ToString() + " is already defined!");
                        }
                        else
                        {
                            seenBuilds.Add(build);
                        }
                    }

                    foreach (var layoutHash in version.layoutHashes)
                    {
                        if (seenLayoutHashes.Contains(layoutHash))
                        {
                            throw new Exception("Layout hash " + layoutHash + " is already defined!");
                        }
                        else
                        {
                            seenLayoutHashes.Add(layoutHash);
                        }

                        if (layoutHash.Length != 8)
                        {
                            throw new Exception("Layout hash \"" + layoutHash + "\" is wrong length for file " + file);
                        }
                    }

                    // Check if int/uint columns have sizes set or the other way around
                    foreach (var definition in version.definitions)
                    {
                        if ((columnDefinitionDictionary[definition.name].type == "int" || columnDefinitionDictionary[definition.name].type == "uint") && definition.size == 0)
                        {
                            throw new Exception("Version definition " + definition.name + " is an int/uint but is missing size in file " + file + "!");
                        }

                        if ((columnDefinitionDictionary[definition.name].type != "int" && columnDefinitionDictionary[definition.name].type != "uint") && definition.size != 0){
                            throw new Exception("Version definition " + definition.name + " is NOT an int/uint but has size in file " + file + "!");
                        }
                    }

                    if(version.definitions.GroupBy(n => n.name).Any(c => c.Count() > 1))
                    {
                        throw new Exception("Version definitions contains multiple columns of the same name!");
                    }
                }

                for (var i = 0; i < versionDefinitions.Count; i++)
                {
                    for (var j = 0; j < versionDefinitions.Count; j++)
                    {
                        if (i == j) continue; // Do not compare same entry

                        for (var b = 0; b < versionDefinitions[i].buildRanges.Length; b++)
                        {
                            for (var c = 0; c < versionDefinitions[j].builds.Length; c++)
                            {
                                if (versionDefinitions[i].buildRanges[b].Contains(versionDefinitions[j].builds[c]))
                                {
                                    throw new Exception("Build " + versionDefinitions[j].builds[c] + " conflicts with " + versionDefinitions[i].buildRanges[b] + "!");
                                }
                            }

                            for (var c = 0; c < versionDefinitions[j].buildRanges.Length; c++)
                            {
                                if (versionDefinitions[i].buildRanges[b].Contains(versionDefinitions[j].buildRanges[c].minBuild) || versionDefinitions[i].buildRanges[b].Contains(versionDefinitions[j].buildRanges[c].maxBuild))
                                {
                                    throw new Exception("Build " + versionDefinitions[j].buildRanges[c] + " conflicts with " + versionDefinitions[i].buildRanges[b] + "!");
                                }
                            }
                        }

                        if (versionDefinitions[i].definitions.SequenceEqual(versionDefinitions[j].definitions))
                        {
                            if (versionDefinitions[i].layoutHashes.Length > 0 && versionDefinitions[j].layoutHashes.Length > 0 && !versionDefinitions[i].layoutHashes.SequenceEqual(versionDefinitions[j].layoutHashes)){
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine(Path.GetFileNameWithoutExtension(file) + " has 2 identical version definitions (" + (i + 1) + " and " + (j + 1) + ") but two different layouthashes, ignoring...");
                                Console.ResetColor();
                            }
                            else
                            {
                                throw new Exception(Path.GetFileNameWithoutExtension(file) + " has 2 identical version definitions (" + (i + 1) + " and " + (j + 1) + ")!");
                            }
                        }
                    }
                }
            }

            return new DBDefinition
            {
                columnDefinitions = columnDefinitionDictionary,
                versionDefinitions = versionDefinitions.ToArray()
            };
        }
    }
}
