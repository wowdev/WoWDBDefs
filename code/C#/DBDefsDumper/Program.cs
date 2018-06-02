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
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Header
        {
            public UInt32 magic;
            public UInt32 cpu;
            public UInt32 subtype;

            public UInt32 fileType;

            public UInt32 commandsCount;

            public UInt32 commandsSize;
            public UInt32 flags;

            public UInt32 reserved;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Command
        {
            public UInt32 id;
            public UInt32 size;
        }

        struct Map
        {
            public UInt64 memoryStart;
            public UInt64 memoryEnd;
            public UInt64 fileStart;
            public UInt64 fileEnd;

            public UInt64 translateMemoryToFile(UInt64 offset)
            {
                if (offset < memoryStart || offset > memoryEnd)
                {
                    return 0x0;
                }

                var delta = offset - memoryStart;

                return fileStart + delta;
            }
        }

        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                throw new ArgumentException("Not enough arguments! Required: file");
            }

            if (!File.Exists(args[0]))
            {
                throw new FileNotFoundException("File not found!");
            }

            var file = MemoryMappedFile.CreateFromFile(args[0], FileMode.Open);
            var stream = file.CreateViewAccessor();

            #region BinaryMagic 
            long offset = 0;

            stream.Read(offset, out Header header);
            offset += Marshal.SizeOf(header);

            var maps = new List<Map>();

            for (var i = 0; i < header.commandsCount; i++)
            {
                stream.Read(offset, out Command command);

                if (command.id == 25)
                {
                    var segmentOffset = offset + 4 + 4 + 16; // Segment start + id + size + name; 
                    var vmemOffs = stream.ReadUInt64(segmentOffset);
                    var vmemSize = stream.ReadUInt64(segmentOffset + 8);
                    var fileOffs = stream.ReadUInt64(segmentOffset + 16);
                    var fileSize = stream.ReadUInt64(segmentOffset + 24);

                    var map = new Map
                    {
                        memoryStart = vmemOffs,
                        memoryEnd = vmemOffs + vmemSize,
                        fileStart = fileOffs,
                        fileEnd = fileOffs + fileSize
                    };

                    maps.Add(map);
                }

                offset += command.size;
            }


            Func<UInt64, UInt64> translate = search => maps.Select(map => map.translateMemoryToFile(search)).FirstOrDefault(r => r != 0x0);
            #endregion

            using (var bin = new BinaryReader(file.CreateViewStream()))
            {
                // Extract DBMeta
                var metas = new Dictionary<string, DBMeta>();
                var pattern = ParsePattern().ToArray();
                var patternLength = pattern.Length;
                var chunkSize = 1024;

                while (true)
                {
                    if ((bin.BaseStream.Length - bin.BaseStream.Position) < chunkSize)
                    {
                        break;
                    }

                    var posInStack = Search(bin.ReadBytes(chunkSize), pattern);

                    if (posInStack != chunkSize)
                    {
                        var matchPos = bin.BaseStream.Position - chunkSize + posInStack;

                        bin.BaseStream.Position = matchPos;

                        var meta = bin.Read<DBMeta>();

                        if (meta.fileDataID > 801575 && meta.fileDataID < 5000000 && meta.record_size > 0)
                        {
                            bin.BaseStream.Position = (long)translate((ulong)meta.nameOffset);
                            metas.Add(bin.ReadCString(), meta);
                        }

                        bin.BaseStream.Position = matchPos + patternLength;
                    }
                    else
                    {
                        bin.BaseStream.Position = bin.BaseStream.Position - patternLength;
                    }
                }

                // Find version
                var buildPattern = new byte?[] { 0x20, 0x5B, 0x42, 0x75, 0x69, 0x6C, 0x64, 0x20, null, null, null, null, null, 0x20, 0x28, null, null, null, null, null, 0x29, 0x20, 0x28 };
                var buildPatternLength = buildPattern.Length;
                bin.BaseStream.Position = 0;

                var build = "";

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
                        bin.ReadBytes(8);
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

                if(build == "")
                {
                    throw new Exception("Build was not found!");
                }

                if (!Directory.Exists("definitions"))
                {
                    Directory.CreateDirectory("definitions");
                }

                // Process DBMetas
                foreach(var meta in metas)
                {
                    var writer = new StreamWriter(Path.Combine("definitions", meta.Key + ".dbd"));

                    writer.WriteLine("COLUMNS");

                    Console.Write("Writing " + meta.Key + ".dbd..");

                    var field_offsets = new List<int>();
                    bin.BaseStream.Position = (long)translate((ulong)meta.Value.field_offsets_offs);
                    for (var i = 0; i < meta.Value.num_fields; i++)
                    {
                        field_offsets.Add(bin.ReadInt32());
                    }

                    var field_sizes = new List<int>();
                    bin.BaseStream.Position = (long)translate((ulong)meta.Value.field_sizes_offs);
                    for (var i = 0; i < meta.Value.num_fields; i++)
                    {
                        field_sizes.Add(bin.ReadInt32());
                    }

                    var field_types = new List<int>();
                    bin.BaseStream.Position = (long)translate((ulong)meta.Value.field_types_offs);
                    for (var i = 0; i < meta.Value.num_fields; i++)
                    {
                        field_types.Add(bin.ReadInt32());
                    }

                    var field_flags = new List<int>();
                    bin.BaseStream.Position = (long)translate((ulong)meta.Value.field_flags_offs);
                    for (var i = 0; i < meta.Value.num_fields; i++)
                    {
                        field_flags.Add(bin.ReadInt32());
                    }

                    var field_sizes_in_file = new List<int>();
                    bin.BaseStream.Position = (long)translate((ulong)meta.Value.field_sizes_in_file_offs);
                    for (var i = 0; i < meta.Value.num_fields; i++)
                    {
                        field_sizes_in_file.Add(bin.ReadInt32());
                    }

                    var field_types_in_file = new List<int>();
                    bin.BaseStream.Position = (long)translate((ulong)meta.Value.field_types_in_file_offs);
                    for (var i = 0; i < meta.Value.num_fields; i++)
                    {
                        field_types_in_file.Add(bin.ReadInt32());
                    }

                    // Read field flags in file
                    var field_flags_in_file = new List<int>();
                    bin.BaseStream.Position = (long)translate((ulong)meta.Value.field_flags_in_file_offs);
                    for (var i = 0; i < meta.Value.num_fields; i++)
                    {
                        field_flags_in_file.Add(bin.ReadInt32());
                    }

                    if (meta.Value.id_column == -1)
                    {
                        writer.WriteLine("int ID");
                    }

                    var columnNames = new List<string>();
                    var columnTypeFlags = new List<Tuple<int, int>>();

                    for(var i = 0; i < meta.Value.num_fields_in_file; i++)
                    {
                        columnTypeFlags.Add(new Tuple<int, int>(field_types_in_file[i], field_flags_in_file[i]));
                    }

                    if (meta.Value.num_fields_in_file != meta.Value.num_fields)
                    {
                        columnTypeFlags.Add(new Tuple<int, int>(field_types[meta.Value.num_fields_in_file], field_flags[meta.Value.num_fields_in_file]));
                    }

                    for(var i = 0; i < columnTypeFlags.Count; i++)
                    {
                        columnNames.Add("field_" + new Random().Next(1, int.MaxValue).ToString().PadLeft(9, '0'));

                        var t = TypeToT(columnTypeFlags[i].Item1, (FieldFlags)columnTypeFlags[i].Item2);
                        if(t.Item1 == "locstring")
                        {
                            writer.WriteLine(t.Item1 + " " + columnNames[i]+ "_lang");
                        }
                        else
                        {
                            writer.WriteLine(t.Item1 + " " + columnNames[i]);
                        }
                    }

                    writer.WriteLine();

                    writer.WriteLine("LAYOUT " + meta.Value.layout_hash.ToString("X8").ToUpper());
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
                        var typeFlags = TypeToT(field_types_in_file[i], (FieldFlags)field_flags_in_file[i]);

                        if(meta.Value.id_column == i)
                        {
                            writer.Write("$id$");
                        }

                        if(meta.Value.column_8C == i)
                        {
                            writer.Write("$relation$");
                            if(meta.Value.column_90 != i)
                            {
                                throw new Exception("No column_90 but there is column_8C send help!");
                            }
                        }

                        writer.Write(columnNames[i]);

                        if(typeFlags.Item1 == "locstring")
                        {
                            writer.Write("_lang");
                        }

                        if(typeFlags.Item2 > 0)
                        {
                            writer.Write("<" + typeFlags.Item2 + ">");
                        }

                        if(field_sizes_in_file[i] != 1)
                        {
                            writer.Write("[" + field_sizes[i] + "]");
                        }

                        writer.WriteLine();
                    }

                    if (meta.Value.num_fields_in_file != meta.Value.num_fields)
                    {
                        var i = meta.Value.num_fields_in_file;
                        var typeFlags = TypeToT(field_types[i], (FieldFlags)field_flags[i]);

                        writer.Write("$noninline,relation$" + columnNames[i]);

                        if (typeFlags.Item1 == "locstring")
                        {
                            writer.Write("_lang");
                        }

                        if (typeFlags.Item2 > 0)
                        {
                            writer.Write("<" + typeFlags.Item2 + ">");
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

        private struct DBMeta
        {
            public long nameOffset;         // 0
            public int fileDataID;          // 8
            public int num_fields_in_file;  // 12
            public int record_size;         // 16
            public int num_fields;          // 20
            public int id_column;           // 24
            public byte sparseTable;        // 25
            public byte padding0;           // 29
            public byte padding1;           // 30
            public byte padding2;           // 31
            public long field_offsets_offs; // 39
            public long field_sizes_offs;   // 47
            public long field_types_offs;   // 55
            public long field_flags_offs;   // 63
            public long field_sizes_in_file_offs;   // 71
            public long field_types_in_file_offs;   // 79
            public long field_flags_in_file_offs;   // 87
            public byte flags_58_2_1;       // 88
            public byte padding3;           // 89
            public byte padding4;           // 90
            public byte padding5;           // 91
            public int table_hash;          // 95
            public int padding6;            // 99
            public int layout_hash;         // 103
            public byte flags_68_4_2_1;     // 104
            public byte padding7;           // 105
            public byte padding8;           // 106
            public byte padding9;           // 107
            public int nbUniqueIdxByInt;    // 111 
            public int nbUniqueIdxByString; // 115
            public int padding10;           // 119
            public long uniqueIdxByInt;     // 127
            public long uniqueIdxByString;  // 135
            public byte bool_88;            // 136
            public byte padding11;          // 137
            public byte padding12;          // 138
            public byte padding13;          // 139
            public int column_8C;           // 143
            public int column_90;           // 147
            public int padding14;           // 151
            public long sortFunctionOffs;   // 159
            public long table_name;         // 167
            /* 
            //probs not in osx
            const char** field_names_in_file; // 175
            const char** field_names;       // 183
            const char* fk_clause;          // 191
            char bool_C0;                   // 192
            */

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
        public static List<byte?> ParsePattern()
        {
            // Parse pattern
            var explodedPattern = BuildPattern().Split(' ');

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
        public static string BuildPattern()
        {
            var pattern__pointer = "? ? ? ? 01 00 00 00 ";
            var pattern__optional_pointer = "? ? ? ? ? 00 00 00 ";
            var pattern__boolean = "? 00 00 00 ";
            var pattern__uint8 = pattern__boolean;
            var pattern__field_reference = "? ? 00 00 ";
            var pattern__field_reference_or_none = "? ? ? ? ";
            var pattern__hash = "? ? ? ? ";
            var pattern__4_byte_padding = "00 00 00 00 ";
            var pattern__fdid = "? ? ? ? ";
            var pattern__record_size = "? ? 00 00 ";

            var pattern = pattern__pointer
                    + pattern__fdid
                    + pattern__field_reference
                    + pattern__record_size
                    + pattern__field_reference
                    + pattern__field_reference_or_none
                    + pattern__boolean
                    + pattern__pointer
                    + pattern__pointer
                    + pattern__pointer
                    + pattern__pointer
                    + pattern__pointer
                    + pattern__pointer
                    + pattern__pointer
                    + pattern__uint8
                    + pattern__hash
                    + pattern__4_byte_padding
                    + pattern__hash
                    + pattern__uint8
                    + pattern__field_reference
                    + pattern__field_reference
                    + pattern__4_byte_padding
                    + pattern__optional_pointer
                    + pattern__optional_pointer
                    + pattern__boolean
                    + pattern__field_reference_or_none
                    + pattern__field_reference_or_none
                    + pattern__4_byte_padding
                    + pattern__optional_pointer
                    + pattern__boolean
                    + pattern__4_byte_padding;

            return pattern;
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