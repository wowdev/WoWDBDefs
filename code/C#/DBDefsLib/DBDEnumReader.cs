using DBDefsLib.Constants;
using DBDefsLib.Structs;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace DBDefsLib
{
    /// <summary>
    /// This handles both .dbdf and .dbde reading (<see cref="MetaType.FLAGS"/> and <see cref="MetaType.ENUM"/>)
    /// </summary>
    public class DBDEnumReader
    {
        public EnumDefinition Read(Stream stream, MetaType metaType)
        {
            var reader = new StreamReader(stream);
            var lines = reader.ReadLines();

            reader.Close();
            reader.Dispose();

            var entries = new List<EnumEntry>();

            var lineNumber = 0;
            while (lineNumber < lines.Count)
            {
                var line = lines[lineNumber++];

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var entry = new EnumEntry
                {
                    builds = [],
                    buildRanges = []
                };

                // Optional build qualifier: (BUILD range1, range2, build1) ...
                if (line.StartsWith("(BUILD "))
                {
                    var closeIndex = line.IndexOf(')');
                    if (closeIndex < 0)
                        throw new Exception($"Line {lineNumber}: Missing closing ')' for build qualifier");

                    var (builds, buildRanges) = Utils.ParseBuildQualifier(line[1..closeIndex], lineNumber);
                    entry.builds = builds.ToArray();
                    entry.buildRanges = buildRanges.ToArray();

                    line = line[(closeIndex + 1)..].TrimStart();
                }

                // Optional comment after //
                if (line.Contains("//"))
                {
                    var commentIndex = line.IndexOf("//", StringComparison.Ordinal);
                    entry.comment = line[(commentIndex + 2)..].Trim();
                    line = line[..commentIndex].TrimEnd();
                }

                // Remaining: "value [name]"
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0)
                    throw new Exception($"Line {lineNumber}: Empty entry after stripping qualifier/comment");

                var valueStr = parts[0];
                if (metaType == MetaType.FLAGS)
                {
                    if (!valueStr.StartsWith("0x", StringComparison.OrdinalIgnoreCase) || !long.TryParse(valueStr[2..], NumberStyles.HexNumber, null, out entry.value))
                        throw new Exception($"Line {lineNumber}: Invalid flag value '{valueStr}', expected hex prefixed with 0x");
                }
                else
                {
                    if (!long.TryParse(valueStr, out entry.value))
                        throw new Exception($"Line {lineNumber}: Invalid enum value '{valueStr}'");
                }

                if (parts.Length >= 2)
                    entry.name = parts[1];

                entries.Add(entry);
            }

            return new EnumDefinition { metaType = metaType, entries = entries };
        }

        public EnumDefinition Read(string file, MetaType metaType)
        {
            if (!File.Exists(file))
                throw new FileNotFoundException($"Unable to find meta file: {file}");

            using var stream = File.Open(file, FileMode.Open, FileAccess.Read);
            return Read(stream, metaType);
        }
    }
}
