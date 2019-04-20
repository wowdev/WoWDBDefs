using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace DBDefsDumper
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                throw new ArgumentException("Not enough arguments! Required: file, outdir, (build in x.x.x format)");
            }

            if (!File.Exists(args[0]))
            {
                throw new FileNotFoundException("File not found!");
            }

            var file = MemoryMappedFile.CreateFromFile(args[0], FileMode.Open);

            var maps = EXEParsing.GenerateMap(file.CreateViewAccessor());

            ulong translate(ulong search) => maps.Select(map => map.translateMemoryToFile(search)).FirstOrDefault(r => r != 0x0);

            using (var bin = new BinaryReader(file.CreateViewStream()))
            {
                var chunkSize = 1024;

                // Find version
                var buildPattern = new byte?[] { 0x42, 0x75, 0x69, 0x6C, 0x64, 0x20, null, null, null, null, null, 0x20, 0x28, null, null, null, null, null, 0x29, 0x20, 0x28 };
                var buildPatternLength = buildPattern.Length;

                var build = "";

                if (args.Length == 3)
                {
                    build = args[2];
                }

                while (true)
                {
                    if ((bin.BaseStream.Length - bin.BaseStream.Position) < chunkSize)
                    {
                        break;
                    }

                    var posInStack = Search(bin.ReadBytes(chunkSize), buildPattern);

                    if (posInStack != chunkSize)
                    {
                        var matchPos = bin.BaseStream.Position - chunkSize + posInStack;

                        bin.BaseStream.Position = matchPos;
                        bin.ReadBytes(6);
                        var buildNumber = new string(bin.ReadChars(5));
                        bin.ReadBytes(2);
                        var patch = new string(bin.ReadChars(5));
                        build = patch + "." + buildNumber;
                    }
                    else
                    {
                        bin.BaseStream.Position = bin.BaseStream.Position - buildPatternLength;
                    }
                }


                if (build == "")
                {
                    // Retry with backup pattern (crash log output)
                    bin.BaseStream.Position = 0;

                    buildPattern = new byte?[] { 0x00, 0x3C, 0x56, 0x65, 0x72, 0x73, 0x69, 0x6F, 0x6E, 0x3E, 0x20 }; // <Version> 
                    buildPatternLength = buildPattern.Length;

                    while (true)
                    {
                        if ((bin.BaseStream.Length - bin.BaseStream.Position) < chunkSize)
                        {
                            break;
                        }

                        var posInStack = Search(bin.ReadBytes(chunkSize), buildPattern);

                        if (posInStack != chunkSize)
                        {
                            var matchPos = bin.BaseStream.Position - chunkSize + posInStack;

                            bin.BaseStream.Position = matchPos;
                            bin.ReadBytes(11);
                            build = new string(bin.ReadChars(11));
                        }
                        else
                        {
                            bin.BaseStream.Position = bin.BaseStream.Position - buildPatternLength;
                        }
                    }
                }

                if (build == "")
                {
                    // Retry with RenderService pattern..
                    bin.BaseStream.Position = 0;

                    buildPattern = new byte?[] { 0x52, 0x65, 0x6e, 0x64, 0x65, 0x72, 0x53, 0x65, 0x72, 0x76, 0x69, 0x63, 0x65, 0x20, null, null, null, null, null, 0x00 }; // <Version> 
                    buildPatternLength = buildPattern.Length;

                    while (true)
                    {
                        if ((bin.BaseStream.Length - bin.BaseStream.Position) < chunkSize)
                        {
                            break;
                        }

                        var posInStack = Search(bin.ReadBytes(chunkSize), buildPattern);

                        if (posInStack != chunkSize)
                        {
                            var matchPos = bin.BaseStream.Position - chunkSize + posInStack;

                            bin.BaseStream.Position = matchPos;
                            bin.ReadBytes(14);
                            build = new string(bin.ReadChars(5));
                            if(args.Length == 3)
                            {
                                build = args[2];
                            }
                            else
                            {
                                Console.WriteLine("Expansion, major and minor version not found in binary. Please enter it in this format X.X.X: ");
                                build = Console.ReadLine() + "." + build;
                            }
                        }
                        else
                        {
                            bin.BaseStream.Position = bin.BaseStream.Position - buildPatternLength;
                        }
                    }
                }

                if (build == "")
                {
                    Console.WriteLine("Build was not found! Please enter a build in this format: X.X.X.XXXXX");
                    build = Console.ReadLine();
                }

                if (build == "8.0.1.26321")
                {
                    Console.WriteLine("Build 8.0.1.26321 has incorrect DBMeta, skipping..");
                    return;
                }
                // Reset position for DBMeta reading
                bin.BaseStream.Position = 0;

                // Extract DBMeta
                var metas = new Dictionary<string, DBMeta>();

                var patternBuilder = new PatternBuilder();

                foreach(var pattern in patternBuilder.patterns)
                {
                    // Skip versions of the pattern that aren't for this expansion
                    if (build.StartsWith("1"))
                    {
                        if (!pattern.compatiblePatches.Contains(build.Substring(0, 6)))
                        {
                            Console.WriteLine("Skipping " + pattern.name + " as it does not list " + build + " as compatible!");
                            continue;
                        }

                        if (!pattern.compatiblePatches.Contains(build.Substring(0, 6)))
                        {
                            Console.WriteLine("Skipping " + pattern.name + " as it does not list " + build + " as compatible!");
                            continue;
                        }

                        if (pattern.minBuild != 0 && pattern.minBuild > int.Parse(build.Substring(7)))
                        {
                            Console.WriteLine("Skipping " + pattern.name + " as minimum build " + pattern.minBuild + " exceeds build of " + build.Substring(6));
                            continue;
                        }

                        if (pattern.maxBuild != 0 && int.Parse(build.Substring(7)) > pattern.maxBuild)
                        {
                            Console.WriteLine("Skipping " + pattern.name + " as maximum build " + pattern.maxBuild + " exceeds build of " + build.Substring(6));
                            continue;
                        }
                    }
                    else
                    {
                        if (!pattern.compatiblePatches.Contains(build.Substring(0, 5)))
                        {
                            Console.WriteLine("Skipping " + pattern.name + " as it does not list " + build + " as compatible!");
                            continue;
                        }

                        if (!pattern.compatiblePatches.Contains(build.Substring(0, 5)))
                        {
                            Console.WriteLine("Skipping " + pattern.name + " as it does not list " + build + " as compatible!");
                            continue;
                        }

                        if (pattern.minBuild != 0 && pattern.minBuild > int.Parse(build.Substring(6)))
                        {
                            Console.WriteLine("Skipping " + pattern.name + " as minimum build " + pattern.minBuild + " exceeds build of " + build.Substring(6));
                            continue;
                        }

                        if (pattern.maxBuild != 0 && int.Parse(build.Substring(6)) > pattern.maxBuild)
                        {
                            Console.WriteLine("Skipping " + pattern.name + " as maximum build " + pattern.maxBuild + " exceeds build of " + build.Substring(6));
                            continue;
                        }
                    }

                    var patternBytes = ParsePattern(pattern.cur_pattern).ToArray();
                    var patternLength = patternBytes.Length;

                    while (true)
                    {
                        if ((bin.BaseStream.Length - bin.BaseStream.Position) < chunkSize)
                        {
                            break;
                        }

                        var posInStack = Search(bin.ReadBytes(chunkSize), patternBytes);

                        if (posInStack != chunkSize)
                        {
                            var matchPos = bin.BaseStream.Position - chunkSize + posInStack;

                            Console.WriteLine("Pattern " + pattern.name + " matched at " + matchPos);

                            if (pattern.offsets.ContainsKey(Name.FDID))
                            {
                                bin.BaseStream.Position = matchPos + pattern.offsets[Name.FDID];
                                var fdid = bin.ReadUInt32();
                                if (fdid < 53183)
                                {
                                    Console.WriteLine("Invalid filedataid " + fdid + ", skipping match..");
                                    continue;
                                }
                            }

                            if (pattern.offsets.ContainsKey(Name.RECORD_SIZE))
                            {
                                bin.BaseStream.Position = matchPos + pattern.offsets[Name.RECORD_SIZE];
                                if (bin.ReadUInt32() == 0)
                                {
                                    Console.WriteLine("Record size is 0, skipping match..");
                                    continue;
                                }
                            }

                            if (pattern.offsets.ContainsKey(Name.DB_NAME))
                            {
                                bin.BaseStream.Position = matchPos + pattern.offsets[Name.DB_NAME];
                                if (bin.ReadUInt32() < 10)
                                {
                                    Console.WriteLine("Name offset is invalid, skipping match..");
                                    continue;
                                }

                                bin.BaseStream.Position = matchPos + pattern.offsets[Name.DB_NAME];
                                var targetOffset = (long)translate(bin.ReadUInt64());
                                if (targetOffset > bin.BaseStream.Length)
                                {
                                    Console.WriteLine("Name offset is out of range of file, skipping match..");
                                    continue;
                                }
                            }

                            if (pattern.offsets.ContainsKey(Name.DB_FILENAME))
                            {
                                bin.BaseStream.Position = matchPos + pattern.offsets[Name.DB_FILENAME];
                                if (bin.ReadUInt32() < 10)
                                {
                                    Console.WriteLine("Name offset is invalid, skipping match..");
                                    continue;
                                }

                                bin.BaseStream.Position = matchPos + pattern.offsets[Name.DB_FILENAME];
                                var targetOffset = (long)translate(bin.ReadUInt64());
                                if (targetOffset > bin.BaseStream.Length)
                                {
                                    Console.WriteLine("Name offset is out of range of file, skipping match..");
                                    continue;
                                }
                            }

                            if (pattern.offsets.ContainsKey(Name.NUM_FIELD_IN_FILE))
                            {
                                bin.BaseStream.Position = matchPos + pattern.offsets[Name.NUM_FIELD_IN_FILE];
                                if (bin.ReadUInt32() > 5000)
                                {
                                    Console.WriteLine("Num fields in file is over 5000, skipping match..");
                                    continue;
                                }

                            }
                            if (pattern.offsets.ContainsKey(Name.FIELD_TYPES_IN_FILE) && pattern.offsets.ContainsKey(Name.FIELD_SIZES_IN_FILE))
                            {
                                bin.BaseStream.Position = matchPos + pattern.offsets[Name.FIELD_TYPES_IN_FILE];
                                var fieldTypesInFile = bin.ReadInt64();
                                bin.BaseStream.Position = matchPos + pattern.offsets[Name.FIELD_SIZES_IN_FILE];
                                var fieldSizesInFileOffs = bin.ReadInt64();
                                if(fieldTypesInFile == fieldSizesInFileOffs)
                                {
                                    Console.WriteLine("Field types in file offset == field sizes in file offset, skipping match..");
                                    continue;
                                }
                            }

                            if (pattern.offsets.ContainsKey(Name.DB_CACHE_FILENAME))
                            {
                                bin.BaseStream.Position = matchPos + pattern.offsets[Name.DB_CACHE_FILENAME];
                                bin.BaseStream.Position = (long)translate((ulong)bin.ReadInt64());
                                var adbname = bin.ReadCString();

                                bin.BaseStream.Position = matchPos + pattern.offsets[Name.DB_CACHE_FILENAME];

                                if (!adbname.EndsWith("adb"))
                                {
                                    Console.WriteLine("ADB filename does not end in adb, skipping match..");
                                    continue;
                                }
                            }

                            bin.BaseStream.Position = matchPos;
                            var meta = ReadMeta(bin, pattern);

                            if (pattern.offsets.ContainsKey(Name.DB_NAME))
                            {
                                bin.BaseStream.Position = (long)translate((ulong)meta.nameOffset);
                                var filename = bin.ReadCString();
                                if(filename.Contains("DBFilesClient"))
                                {
                                    filename = filename.Substring(filename.IndexOf("\\") + 1);
                                }

                                metas.TryAdd(Path.GetFileNameWithoutExtension(filename), meta);
                            }else if (pattern.offsets.ContainsKey(Name.DB_FILENAME))
                            {
                                bin.BaseStream.Position = (long)translate((ulong)meta.dbFilenameOffs);
                                var name = bin.ReadCString();
                                metas.TryAdd(Path.GetFileNameWithoutExtension(name), meta);
                            }
                            
                            bin.BaseStream.Position = matchPos + patternLength;
                        }
                        else
                        {
                            bin.BaseStream.Position = bin.BaseStream.Position - patternLength;
                        }
                    }

                    bin.BaseStream.Position = 0;
                }

                var outputDirectory = Path.Combine(args[1], build);

                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                // Process DBMetas
                foreach(var meta in metas)
                {
                    if((long)translate((ulong)meta.Value.field_offsets_offs) > bin.BaseStream.Length)
                    {
                        Console.WriteLine("Skipping reading of " + meta.Key + " because first offset is way out of range!");
                        continue;
                    }

                    var writer = new StreamWriter(Path.Combine(outputDirectory, meta.Key + ".dbd"));

                    writer.WriteLine("COLUMNS");

                    Console.Write("Writing " + meta.Key + ".dbd..");

                    var fieldCount = 0;
                    if(meta.Value.num_fields == 0 && meta.Value.num_fields_in_file != 0)
                    {
                        fieldCount = meta.Value.num_fields_in_file;
                    }
                    else
                    {
                        fieldCount = meta.Value.num_fields;
                    }

                    var field_offsets = ReadFieldArray(bin, fieldCount, (long)translate((ulong)meta.Value.field_offsets_offs));
                    var field_sizes = ReadFieldArray(bin, fieldCount, (long)translate((ulong)meta.Value.field_sizes_offs));
                    var field_types = ReadFieldArray(bin, fieldCount, (long)translate((ulong)meta.Value.field_types_offs));
                    var field_flags = ReadFieldArray(bin, fieldCount, (long)translate((ulong)meta.Value.field_flags_offs));
                    var field_sizes_in_file = ReadFieldArray(bin, fieldCount, (long)translate((ulong)meta.Value.field_sizes_in_file_offs));
                    var field_types_in_file = ReadFieldArray(bin, fieldCount, (long)translate((ulong)meta.Value.field_types_in_file_offs));
                    var field_flags_in_file = ReadFieldArray(bin, fieldCount, (long)translate((ulong)meta.Value.field_flags_in_file_offs));
                    var field_names_in_file = ReadFieldOffsetArray(bin, fieldCount, (long)translate((ulong)meta.Value.namesInFileOffs));

                    if (meta.Value.id_column == -1)
                    {
                        writer.WriteLine("int ID");
                    }

                    var columnNames = new List<string>();
                    var columnTypeFlags = new List<Tuple<int, int>>();

                    for(var i = 0; i < meta.Value.num_fields_in_file; i++)
                    {
                        if (field_flags_in_file.Count == 0)
                        {
                            columnTypeFlags.Add(new Tuple<int, int>(field_types_in_file[i], 0));
                        }
                        else
                        {
                            columnTypeFlags.Add(new Tuple<int, int>(field_types_in_file[i], field_flags_in_file[i]));
                        }
                    }

                    if (meta.Value.num_fields != 0 && (meta.Value.num_fields_in_file != meta.Value.num_fields))
                    {
                        if(meta.Value.num_fields_in_file > field_flags.Count())
                        {
                            columnTypeFlags.Add(new Tuple<int, int>(field_types[meta.Value.num_fields_in_file], 0));
                        }
                        else
                        {
                            columnTypeFlags.Add(new Tuple<int, int>(field_types[meta.Value.num_fields_in_file], field_flags[meta.Value.num_fields_in_file]));
                        }
                    }

                    for(var i = 0; i < columnTypeFlags.Count; i++)
                    {
                        if(field_names_in_file.Count > 0)
                        {
                            bin.BaseStream.Position = (long)translate(field_names_in_file[i]);
                            columnNames.Add(CleanRealName(bin.ReadCString()));
                        }
                        else
                        {
                            columnNames.Add(GenerateName(i, build));
                        }

                        var t = TypeToT(columnTypeFlags[i].Item1, (FieldFlags)columnTypeFlags[i].Item2);
                        if (field_names_in_file.Count > 0)
                        {
                            if (t.Item1 == "locstring")
                            {
                                writer.WriteLine(t.Item1 + " " + columnNames[i] + "_lang");
                            }
                            else
                            {
                                if (t.Item1 == "uint")
                                {
                                    writer.WriteLine("int " + columnNames[i]);
                                }
                                else
                                {
                                    writer.WriteLine(t.Item1 + " " + columnNames[i]);
                                }
                            }
                        }
                        else
                        {
                            if (t.Item1 == "locstring")
                            {
                                writer.WriteLine(t.Item1 + " " + columnNames[i] + "_lang?");
                            }
                            else
                            {
                                if (t.Item1 == "uint")
                                {
                                    writer.WriteLine("int " + columnNames[i] + "?");
                                }
                                else
                                {
                                    writer.WriteLine(t.Item1 + " " + columnNames[i] + "?");
                                }
                            }
                        }
                    }

                    writer.WriteLine();

                    if(meta.Value.layout_hash != 0)
                    {
                        writer.WriteLine("LAYOUT " + meta.Value.layout_hash.ToString("X8").ToUpper());
                    }

                    writer.WriteLine("BUILD " + build);

                    if(meta.Value.sparseTable == 1)
                    {
                        writer.WriteLine("COMMENT table is sparse");
                    }

                    if(meta.Value.id_column == -1)
                    {
                        writer.WriteLine("$noninline,id$ID<32>");
                    }

                    for (var i = 0; i < meta.Value.num_fields_in_file; i++)
                    {
                        var typeFlags = ("int", 32);

                        if(field_flags_in_file.Count == 0)
                        {
                            typeFlags = TypeToT(field_types_in_file[i], 0);
                        }
                        else
                        {
                            typeFlags = TypeToT(field_types_in_file[i], (FieldFlags)field_flags_in_file[i]);
                        }

                        if (meta.Value.id_column == i)
                        {
                            writer.Write("$id$");
                        }

                        if(build.StartsWith("7.3.5") || build.StartsWith("8"))
                        {
                            if (meta.Value.column_8C == i)
                            {
                                writer.Write("$relation$");
                                if (meta.Value.column_90 != i)
                                {
                                    throw new Exception("No column_90 but there is column_8C send help!");
                                }
                            }
                        }

                        writer.Write(columnNames[i]);

                        if(typeFlags.Item1 == "locstring")
                        {
                            writer.Write("_lang");
                        }

                        if(typeFlags.Item2 > 0)
                        {
                            if(typeFlags.Item1 == "uint")
                            {
                                writer.Write("<u" + typeFlags.Item2 + ">");
                            }
                            else
                            {
                                writer.Write("<" + typeFlags.Item2 + ">");
                            }
                        }

                        if(field_sizes_in_file[i] != 1)
                        {
                            // 6.0.1 has sizes in bytes
                            if (build.StartsWith("6.0.1"))
                            {
                                var supposedSize = 0;
                                
                                if((typeFlags.Item1 == "uint" || typeFlags.Item1 == "int") && typeFlags.Item2 != 32)
                                {
                                    supposedSize = typeFlags.Item2 / 8;
                                }
                                else
                                {
                                    supposedSize = 4;
                                }

                                var fixedSize = field_sizes_in_file[i] / supposedSize;
                                if(fixedSize > 1)
                                {
                                    writer.Write("[" + fixedSize + "]");
                                }
                            }
                            else
                            {
                                writer.Write("[" + field_sizes_in_file[i] + "]");
                            }
                        }

                        writer.WriteLine();
                    }

                    if (meta.Value.num_fields != 0 && (meta.Value.num_fields_in_file != meta.Value.num_fields))
                    {
                        var i = meta.Value.num_fields_in_file;
                        var typeFlags = TypeToT(columnTypeFlags[i].Item1, (FieldFlags)columnTypeFlags[i].Item2);

                        writer.Write("$noninline,relation$" + columnNames[i]);

                        if (typeFlags.Item1 == "locstring")
                        {
                            writer.Write("_lang");
                        }

                        if (typeFlags.Item2 > 0)
                        {
                            if (typeFlags.Item1 == "uint")
                            {
                                writer.Write("<u" + typeFlags.Item2 + ">");
                            }
                            else if(typeFlags.Item1 == "int")
                            {
                                writer.Write("<" + typeFlags.Item2 + ">");
                            }
                        }

                        if (field_sizes[i] != 1)
                        {
                            writer.Write("[" + field_sizes[i] + "]");
                        }
                    }

                    writer.Flush();
                    writer.Close();

                    Console.Write("..done!\n");
                }
            }

            Environment.Exit(0);
        }

        private static List<int> ReadFieldArray(BinaryReader bin, int fieldCount, long offset)
        {
            var returnList = new List<int>();

            if(offset != 0)
            {
                bin.BaseStream.Position = offset;
                for (var i = 0; i < fieldCount; i++)
                {
                    returnList.Add(bin.ReadInt32());
                }
            }

            return returnList;
        }

        private static List<ulong> ReadFieldOffsetArray(BinaryReader bin, int fieldCount, long offset)
        {
            var returnList = new List<ulong>();

            if (offset != 0)
            {
                bin.BaseStream.Position = offset;
                for (var i = 0; i < fieldCount; i++)
                {
                    returnList.Add(bin.ReadUInt64());
                }
            }

            return returnList;
        }

        private static string GenerateName(int fieldIndex, string build)
        {
            // This function should generate a name that is the same between dumps of the same build
            return "Field_" + build.Replace(".", "_") + "_" + fieldIndex.ToString().PadLeft(3, '0');
        }

        private static string CleanRealName(string name)
        {
            if (name.Length > 2 && name.Substring(0, 2) == "m_")
            {
                return name.Remove(0, 2);
            }
            else
            {
                return name;
            }
        }

        private static DBMeta ReadMeta(BinaryReader bin, Pattern pattern)
        {
            var matchPos = bin.BaseStream.Position;

            var meta = new DBMeta();
            foreach(var offset in pattern.offsets)
            {
                bin.BaseStream.Position = matchPos + offset.Value;
                switch (offset.Key)
                {
                    case Name.DB_NAME:
                        meta.nameOffset = bin.ReadInt64();
                        break;
                    case Name.NUM_FIELD_IN_FILE:
                        meta.num_fields_in_file = bin.ReadInt32();
                        break;
                    case Name.RECORD_SIZE:
                        meta.record_size = bin.ReadInt32();
                        break;
                    case Name.NUM_FIELD:
                        meta.num_fields = bin.ReadInt32();
                        break;
                    case Name.ID_COLUMN:
                        meta.id_column = bin.ReadInt32();
                        break;
                    case Name.SPARSE_TABLE:
                        meta.sparseTable = bin.ReadByte();
                        break;
                    case Name.FIELD_OFFSETS:
                        meta.field_offsets_offs = bin.ReadInt64();
                        break;
                    case Name.FIELD_SIZES:
                        meta.field_sizes_offs = bin.ReadInt64();
                        break;
                    case Name.FIELD_TYPES:
                        meta.field_types_offs = bin.ReadInt64();
                        break;
                    case Name.FIELD_FLAGS:
                        meta.field_flags_offs = bin.ReadInt64();
                        break;
                    case Name.FIELD_SIZES_IN_FILE:
                        meta.field_sizes_in_file_offs = bin.ReadInt64();
                        break;
                    case Name.FIELD_TYPES_IN_FILE:
                        meta.field_types_in_file_offs = bin.ReadInt64();
                        break;
                    case Name.FIELD_FLAGS_IN_FILE:
                        meta.field_flags_in_file_offs = bin.ReadInt64();
                        break;
                    case Name.FLAGS_58_21:
                        meta.flags_58_2_1 = bin.ReadByte();
                        break;
                    case Name.TABLE_HASH:
                        meta.table_hash = bin.ReadInt32();
                        break;
                    case Name.LAYOUT_HASH:
                        meta.layout_hash = bin.ReadInt32();
                        break;
                    case Name.FLAGS_68_421:
                        meta.flags_68_4_2_1 = bin.ReadByte();
                        break;
                    case Name.FIELD_NUM_IDX_INT:
                        meta.nbUniqueIdxByInt = bin.ReadInt32();
                        break;
                    case Name.FIELD_NUM_IDX_STRING:
                        meta.nbUniqueIdxByString = bin.ReadInt32();
                        break;
                    case Name.FIELD_IDX_INT:
                        meta.uniqueIdxByInt = bin.ReadInt32();
                        break;
                    case Name.FIELD_IDX_STRING:
                        meta.uniqueIdxByString = bin.ReadInt32();
                        break;
                    case Name.UNK88:
                        meta.bool_88 = bin.ReadByte();
                        break;
                    case Name.FIELD_RELATION:
                        meta.column_8C = bin.ReadInt32();
                        break;
                    case Name.FIELD_RELATION_IN_FILE:
                        meta.column_90 = bin.ReadInt32();
                        break;
                    case Name.SORT_FUNC:
                        meta.sortFunctionOffs = bin.ReadInt64();
                        break;
                    case Name.UNKC0:
                        meta.bool_C0 = bin.ReadByte();
                        break;
                    case Name.FDID:
                        meta.fileDataID = bin.ReadInt32();
                        break;
                    case Name.DB_CACHE_FILENAME:
                        meta.cacheNameOffset = bin.ReadInt64();
                        break;
                    case Name.CONVERT_STRINGREFS:
                        meta.string_ref_offs = bin.ReadInt64();
                        break;
                    case Name.DB_FILENAME:
                        meta.dbFilenameOffs = bin.ReadInt64();
                        break;
                    case Name.FIELD_ENCRYPTED:
                        meta.field_encrypted = bin.ReadInt64();
                        break;
                    case Name.SQL_QUERY:
                        meta.sql_query = bin.ReadInt64();
                        break;
                    case Name.SIBLING_TABLE_HASH:
                        meta.siblingTableHash = bin.ReadInt32();
                        break;
                    case Name.FIELD_NAMES:
                        bin.ReadInt64();
                        break;
                    case Name.FIELD_NAMES_IN_FILE:
                        meta.namesInFileOffs = bin.ReadInt64();
                        break;
                    case Name.DB_NAME_DUPLICATE:
                        bin.ReadInt64();
                        break;
                    case Name.UNK_BOOL_601_x24:
                    case Name.UNK_BOOL_601dbc_x38:
                    case Name.UNK_BOOL_601dbc_x39:
                    case Name.UNK_BOOL_601dbc_x3a:
                    case Name.UNK_BOOL_601dbc_x3b:
                    case Name.UNK_FLAGS_601_x48_421:
                        bin.ReadByte();
                        break;
                    default:
                        throw new Exception("Unknown name: " + offset.Key);
                }
            }
            return meta;
        }

        public enum FieldFlags : int
        {
            f_maybe_fk = 1,
            f_maybe_compressed = 2, // according to simca
            f_unsigned = 4,
            f_localized = 8,
        };
        public static (string, int) TypeToT(int type, FieldFlags flag)
        {
            switch (type)
            {
                case 0:
                    switch (flag)
                    {
                        case 0 | 0 | 0 | 0:
                        case 0 | 0 | 0 | FieldFlags.f_maybe_fk:
                        case 0 | 0 | FieldFlags.f_maybe_compressed | 0:
                        case 0 | 0 | FieldFlags.f_maybe_compressed | FieldFlags.f_maybe_fk:
                            return ("int", 32);
                        case 0 | FieldFlags.f_unsigned | 0 | 0:
                        case 0 | FieldFlags.f_unsigned | 0 | FieldFlags.f_maybe_fk:
                        case 0 | FieldFlags.f_unsigned | FieldFlags.f_maybe_compressed | 0:
                        case 0 | FieldFlags.f_unsigned | FieldFlags.f_maybe_compressed | FieldFlags.f_maybe_fk:
                            return ("uint", 32);
                        default:
                            throw new Exception("Unknown flag combination!");
                    }
                case 1:
                    switch (flag)
                    {
                        case 0 | 0 | 0 | 0:
                            return ("int", 64);
                        case 0 | FieldFlags.f_unsigned | 0 | 0:
                        case 0 | FieldFlags.f_unsigned | 0 | FieldFlags.f_maybe_fk:
                            return ("uint", 64);
                        default:
                            throw new Exception("Unknown flag combination!");
                    }
                case 2:
                    switch (flag)
                    {
                        case 0 | 0 | 0 | 0:
                            return ("string", 0);
                        case FieldFlags.f_localized | 0 | 0 | 0:
                            return ("locstring", 0);
                        default:
                            throw new Exception("Unknown flag combination!");
                    }
                case 3:
                    switch (flag)
                    {
                        case 0 | 0 | 0 | 0:
                        case 0 | 0 | FieldFlags.f_maybe_compressed | 0:
                            return ("float", 0);
                        default:
                            throw new Exception("Unknown flag combination!");
                    }
                case 4:
                    switch (flag)
                    {
                        case 0 | 0 | 0 | 0:
                        case 0 | 0 | FieldFlags.f_maybe_compressed | 0:
                            return ("int", 8);
                        case 0 | FieldFlags.f_unsigned | 0 | 0:
                        case 0 | FieldFlags.f_unsigned | FieldFlags.f_maybe_compressed | 0:
                            return ("uint", 8);
                        default:
                            throw new Exception("Unknown flag combination!");
                    }
                case 5:
                    switch (flag)
                    {
                        case 0 | 0 | 0 | 0:
                        case 0 | 0 | 0 | FieldFlags.f_maybe_fk:
                        case 0 | 0 | FieldFlags.f_maybe_compressed | 0:
                        case 0 | 0 | FieldFlags.f_maybe_compressed | FieldFlags.f_maybe_fk:
                            return ("int", 16);
                        case 0 | FieldFlags.f_unsigned | 0 | 0:
                        case 0 | FieldFlags.f_unsigned | 0 | FieldFlags.f_maybe_fk:
                        case 0 | FieldFlags.f_unsigned | FieldFlags.f_maybe_compressed | 0:
                        case 0 | FieldFlags.f_unsigned | FieldFlags.f_maybe_compressed | FieldFlags.f_maybe_fk:
                            return ("uint", 16);
                        default:
                            throw new Exception("Unknown flag combination!");
                    }
                default:
                    throw new Exception("Ran into unknown field type: " + type);
            }
        }
        public static int Search(byte[] haystack, byte?[] needle)
        {
            int first = 0;
            for (; ; ++first)
            {
                int it = first;
                for (int s_it = 0; ; ++it, ++s_it)
                {
                    if (s_it == needle.Length)
                    {
                        return first;
                    }
                    if (it == haystack.Length)
                    {
                        return haystack.Length;
                    }
                    if (needle[s_it].HasValue && haystack[it] != needle[s_it].Value)
                    {
                        break;
                    }
                }
            }
        }
        public static List<byte?> ParsePattern(string pattern)
        {
            // Parse pattern
            var explodedPattern = pattern.Split(' ');

            var patternList = new List<byte?>();
            foreach (var part in explodedPattern)
            {
                if (part == string.Empty)
                {
                    continue;
                }

                if (part != "?")
                {
                    patternList.Add(Convert.ToByte(part));
                }
                else
                {
                    patternList.Add(null);
                }
            }

            return patternList;
        }
    }
    
    #region BinaryReaderExtensions
    static class BinaryReaderExtensios
    {
        /// <summary> Reads the NULL terminated string from 
        /// the current stream and advances the current position of the stream by string length + 1.
        /// <seealso cref="BinaryReader.ReadString"/>
        /// </summary>
        public static string ReadCString(this BinaryReader reader)
        {
            return reader.ReadCString(Encoding.UTF8);
        }

        /// <summary> Reads the NULL terminated string from 
        /// the current stream and advances the current position of the stream by string length + 1.
        /// <seealso cref="BinaryReader.ReadString"/>
        /// </summary>
        public static string ReadCString(this BinaryReader reader, Encoding encoding)
        {
            var bytes = new List<byte>();
            byte b;
            while ((b = reader.ReadByte()) != 0)
                bytes.Add(b);
            return encoding.GetString(bytes.ToArray());
        }

        public static T Read<T>(this BinaryReader bin)
        {
            var bytes = bin.ReadBytes(Marshal.SizeOf(typeof(T)));
            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            T ret = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            handle.Free();
            return ret;
        }
    }
    #endregion
}
