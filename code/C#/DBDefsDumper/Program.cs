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
            var file = MemoryMappedFile.CreateFromFile(@"wow", FileMode.Open);
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

            var bin = new BinaryReader(file.CreateViewStream());

            long startOffset;

            while (bin.BaseStream.Position < bin.BaseStream.Length)
            {
                // Find FileDataID for Achievement
                if (bin.ReadUInt32() == 1260179)
                {
                    startOffset = bin.BaseStream.Position - 12;
                    break;
                }
            }

            // Roll back 8 bytes to get to name offset
            bin.BaseStream.Position -= 12;

            var metas = new Dictionary<string, DBMeta>();

            var readingMeta = true;

            while (readingMeta)
            {
                var meta = bin.Read<DBMeta>();

                if(meta.fileDataID < 801575 || meta.fileDataID > 5000000)
                {
                    Console.WriteLine("FileDataID " + meta.fileDataID + " doesn't seem valid, we're probably done reading meta and stuff.");
                    readingMeta = false;
                    break;
                }

                var nameOffs = translate((ulong)meta.nameOffset);
                var prevPos = bin.BaseStream.Position;
                bin.BaseStream.Position = (long)nameOffs;
                var name = bin.ReadCString();
                bin.BaseStream.Position = prevPos;

                metas.Add(name, meta);
                Console.WriteLine(name + " (" + meta.fileDataID + ")");
                Console.WriteLine(meta.column_8C + " " + meta.column_90);

                if(name == "ChrClassVillain" || name == "DeclinedWordCases" || name == "UiPartyPose")
                {
                    bin.ReadBytes(24);
                }

            }

            Console.ReadLine();
            Environment.Exit(0);
        }

        // Size should be 0xc1 (193)
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