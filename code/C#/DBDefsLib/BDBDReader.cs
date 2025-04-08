using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static DBDefsLib.Structs;

namespace DBDefsLib
{
    public static class BDBDReader
    {
        public static Dictionary<string, TableInfo> Read(Stream stream, string tableName = "")
        {
            var tableInfo = new Dictionary<string, TableInfo>(StringComparer.InvariantCultureIgnoreCase);

            using (var bin = new BinaryReader(stream))
            {
                var magic = bin.ReadChars(4);
                if (new string(magic) != "BDBD")
                {
                    throw new Exception("Invalid BDBD file");
                }

                var version = bin.ReadUInt32();
                if (version != 1)
                {
                    throw new Exception("Unsupported BDBD version: " + version);
                }

                var stringBlockMagic = bin.ReadChars(4);
                if (new string(stringBlockMagic) != "STRB")
                {
                    throw new Exception("Invalid string block magic");
                }

                var stringBlockSize = bin.ReadInt32();
                var stringBlock = bin.ReadBytes(stringBlockSize);

                var tablesMagic = bin.ReadChars(4);
                if (new string(tablesMagic) != "TBLS")
                {
                    throw new Exception("Invalid tables block magic");
                }

                // Skip for now
                var tableCount = bin.ReadInt32();
                bin.BaseStream.Position += tableCount * 12;

                for (int i = 0; i < tableCount; i++)
                {
                    var tableMagic = bin.ReadChars(4);
                    if (new string(tableMagic) != "TABL")
                    {
                        throw new Exception("Invalid table block magic");
                    }

                    var length = bin.ReadInt32();
                    var table = new TableInfo
                    {
                        tableHash = bin.ReadUInt32(),
                        dbcFileDataID = bin.ReadUInt32(),
                        db2FileDataID = bin.ReadUInt32(),
                        tableName = ReadStringBlockString(ref stringBlock, bin.ReadInt32())
                    };

                    if (!string.IsNullOrEmpty(tableName))
                    {
                        if (table.tableName != tableName)
                        {
                            bin.BaseStream.Position += length - 16;
                            continue;
                        }
                    }

                    var colCount = bin.ReadUInt16();
                    var verCount = bin.ReadUInt16();

                    table.dbd = new DBDefinition
                    {
                        columnDefinitions = [],
                        versionDefinitions = new VersionDefinitions[verCount]
                    };

                    for (int c = 0; c < colCount; c++)
                    {
                        var typeByte = bin.ReadByte();
                        var type = typeByte switch
                        {
                            0 => "int",
                            1 => "float",
                            2 => "string",
                            3 => "locstring",
                            _ => throw new Exception("Unknown column type: " + typeByte)
                        };

                        var verified = bin.ReadByte();
                        var columnName = ReadStringBlockString(ref stringBlock, bin.ReadInt32());
                        var foreignTable = ReadStringBlockString(ref stringBlock, bin.ReadInt32());
                        var foreignColumn = ReadStringBlockString(ref stringBlock, bin.ReadInt32());
                        var comment = ReadStringBlockString(ref stringBlock, bin.ReadInt32());

                        var columnDef = new ColumnDefinition
                        {
                            type = type,
                            verified = verified != 0,
                            foreignTable = foreignTable,
                            foreignColumn = foreignColumn,
                            comment = comment
                        };

                        table.dbd.columnDefinitions.Add(columnName, columnDef);
                    }

                    for (int v = 0; v < verCount; v++)
                    {
                        var versionDefinition = new VersionDefinitions();

                        var layoutHashCount = bin.ReadInt32();
                        versionDefinition.layoutHashes = new string[layoutHashCount];
                        for (int l = 0; l < layoutHashCount; l++)
                            versionDefinition.layoutHashes[l] = bin.ReadInt32().ToString("X8");

                        var buildCount = bin.ReadInt32();
                        versionDefinition.builds = new Build[buildCount];
                        for (int b = 0; b < buildCount; b++)
                            versionDefinition.builds[b] = new Build(bin.ReadByte(), bin.ReadByte(), bin.ReadByte(), bin.ReadUInt32());

                        var buildRangeCount = bin.ReadInt32();
                        versionDefinition.buildRanges = new BuildRange[buildRangeCount];
                        for (int b = 0; b < buildRangeCount; b++)
                        {
                            var start = new Build(bin.ReadByte(), bin.ReadByte(), bin.ReadByte(), bin.ReadUInt32());
                            var end = new Build(bin.ReadByte(), bin.ReadByte(), bin.ReadByte(), bin.ReadUInt32());
                            versionDefinition.buildRanges[b] = new BuildRange(start, end);
                        }

                        var numCols = bin.ReadInt32();
                        versionDefinition.definitions = new Definition[numCols];

                        for(int vc = 0; vc < numCols; vc++)
                        {
                            var flags = bin.ReadByte();
                            var size = bin.ReadByte();
                            var colIndex = bin.ReadUInt16();
                            var arrLength = bin.ReadByte();
                            var comment = ReadStringBlockString(ref stringBlock, bin.ReadInt32());

                            versionDefinition.definitions[vc] = new Definition
                            {
                                size = size,
                                arrLength = arrLength,
                                name = table.dbd.columnDefinitions.ElementAt(colIndex).Key,
                                isSigned = (flags & 0x01) != 0,
                                isNonInline = (flags & 0x02) != 0,
                                isID = (flags & 0x04) != 0,
                                isRelation = (flags & 0x08) != 0,
                                comment = comment
                            };
                        }

                        versionDefinition.comment = ReadStringBlockString(ref stringBlock, bin.ReadInt32());
                        table.dbd.versionDefinitions[v] = versionDefinition;
                    }

                    tableInfo.Add(table.tableName, table);
                }
            }

            return tableInfo;
        }

        public static TableInfo ReadSingle(Stream stream, string tableName)
        {
            return Read(stream, tableName)[tableName];
        }

        private static string ReadStringBlockString(ref byte[] stringBlock, int offset)
        {
            if (offset == -1)
                return "";

            if (offset < 0 || offset >= stringBlock.Length)
                throw new ArgumentOutOfRangeException("Offset is out of range of the string block");

            var size = BitConverter.ToUInt16(stringBlock, offset);
            var stringBytes = new byte[size];
            for (var i = 0; i < size; i++)
            {
                stringBytes[i] = stringBlock[offset + 2 + i];
            }
            return System.Text.Encoding.UTF8.GetString(stringBytes);
        }
    }
}
