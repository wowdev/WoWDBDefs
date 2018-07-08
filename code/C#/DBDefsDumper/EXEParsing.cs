using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace DBDefsDumper
{
    public class EXEParsing
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

        public struct Map
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

        public static List<Map> GenerateMap(MemoryMappedViewAccessor stream)
        {

            stream.Read(0, out uint magic);

            var maps = new List<Map>();

            if (magic == 0xCFFAEDFE || magic == 0xFEEDFACF) // 64 bit Mach-O binary
            {
                long offset = 0;

                stream.Read(offset, out Header header);
                offset += Marshal.SizeOf(header);

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
            }else if(magic == 0x905A4D)
            {
                stream.Read(60, out uint ntHeaderPos);
                stream.Read(ntHeaderPos + 6, out short sectionCount);
                stream.Read(ntHeaderPos + 20, out short sizeOfOptionalHeader);
                stream.Read(ntHeaderPos + 48, out ulong imageBase);
                var offset = ntHeaderPos + 24 + sizeOfOptionalHeader;

                for (var i = 0; i < sectionCount; i++)
                {
                    var sectionOffset = offset + (i * 40);
                    stream.Read(sectionOffset + 8, out uint virtualSize);
                    stream.Read(sectionOffset + 12, out uint virtualAddress);
                    stream.Read(sectionOffset + 16, out uint sizeOfRawData);
                    stream.Read(sectionOffset + 20, out uint pointerToRawData);

                    var map = new Map
                    {
                        memoryStart = imageBase + virtualAddress,
                        memoryEnd = imageBase + virtualAddress + virtualSize,
                        fileStart = pointerToRawData,
                        fileEnd = pointerToRawData + sizeOfRawData
                    };

                    maps.Add(map);
                }
            }
            else
            {
                throw new Exception("Unsupported executable file!");
            }

            return maps;
        }
    }
}
