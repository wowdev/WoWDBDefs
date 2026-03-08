using DBDefsLib.Structs;
using System;
using System.Collections.Generic;
using System.IO;

namespace DBDefsLib
{
    public class DBDMReader
    {
        private List<MappingDefinition> Read(Stream stream)
        {
            var reader = new StreamReader(stream);
            var lines = reader.ReadLines();

            reader.Close();
            reader.Dispose();

            var mappings = new List<MappingDefinition>();

            var lineNumber = 0;
            while (lineNumber < lines.Count)
            {
                var line = lines[lineNumber++];

                // Mapping file could contain empty lines for organization
                if (string.IsNullOrEmpty(line))
                    continue;

                var mappingDefinition = new MappingDefinition();
                var split =  line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                // Comment available in mapping line, we handle split differently here as well
                if (line.Contains("//"))
                {
                    var indexOfComment = line.IndexOf("//", StringComparison.OrdinalIgnoreCase);

                    var lineWithComment = line[..indexOfComment];
                    split = lineWithComment.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    // Ignore "// " in comments
                    var comment = line[(indexOfComment + 3)..];
                    mappingDefinition.comment = comment;
                }

                // "Meta TableName::ColumnName" are required in the Mapping file
                if (split.Length < 2)
                    throw new Exception($"Line: {lineNumber} has invalid size for fields, expected: 2, current: {split.Length}");

                // Retrieve the meta type
                var metaType = split[0];
                if (!Enum.TryParse(metaType, out mappingDefinition.meta))
                    throw new Exception($"Line: {lineNumber} does not contain a valid MetaType: {metaType}");

                // Retrieve the tablename and column name
                var tableColumnSplit = split[1].Split("::");
                if (tableColumnSplit.Length > 2)
                    throw new Exception($"Line: {lineNumber} has invalid size for Table::Column, excepted: 2, current: {tableColumnSplit.Length}");

                mappingDefinition.tableName = tableColumnSplit[0];

                // Array column
                var columnName = tableColumnSplit[1];
                if (columnName.Contains('[') && columnName.Contains(']'))
                {
                    var indexOfStartArr = columnName.IndexOf('[');
                    var indexOfEndArr = columnName.IndexOf(']');

                    var arrIndexString = columnName[(indexOfStartArr + 1)..indexOfEndArr];
                    if (!int.TryParse(arrIndexString, out var arrIndex))
                        throw new Exception($"Line: {lineNumber} has invalid array index for column: {columnName}");

                    mappingDefinition.columnName = columnName[..indexOfStartArr];
                    mappingDefinition.arrIndex = arrIndex;
                }
                else
                {
                    mappingDefinition.columnName = columnName;
                }

                if (split.Length >= 3)
                    mappingDefinition.metaValue = split[2];

                if (split.Length == 4)
                {
                    var conditionalData = split[3];
                    if (!conditionalData.Contains("="))
                        throw new Exception($"Line: {lineNumber} has no conditional Table::Column value assignment");

                    var assignmentIndexOf = conditionalData.IndexOf('=');
                    tableColumnSplit = conditionalData[..assignmentIndexOf].Split("::");
                    if (tableColumnSplit.Length > 2)
                        throw new Exception($"Line: {lineNumber} has invalid size for Conditional Table::Column, excepted: 2, current: {tableColumnSplit.Length}");

                    mappingDefinition.conditionalTable = tableColumnSplit[0];
                    mappingDefinition.conditionalColumn = tableColumnSplit[1];
                    mappingDefinition.conditionalValue = conditionalData[(assignmentIndexOf + 1)..];
                }

                mappings.Add(mappingDefinition);
            }

            return mappings;
        }

        public List<MappingDefinition> Read(string file)
        {
            if (!File.Exists(file))
                throw new FileNotFoundException($"Unable to find mapping file: {file}");

            using var stream = File.Open(file, FileMode.Open, FileAccess.Read);
            return Read(stream);
        }
    }
}
