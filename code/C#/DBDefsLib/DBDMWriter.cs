using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DBDefsLib
{
    public class DBDMWriter
    {
        public static void Save(List<Structs.MappingDefinition> mappingDefinitions, string filePath)
        {
            var sb = new StringBuilder();

            var mappingDefsSorted = mappingDefinitions.OrderBy(md => md.meta.ToString()).ThenBy(md => md.tableName).ThenBy(md => md.columnName).ThenBy(md => md.arrIndex);

            foreach (var def in mappingDefsSorted)
            {
                sb.Append($"{def.meta} {def.tableName}::{def.columnName}");

                if (def.arrIndex.HasValue)
                    sb.Append($"[{def.arrIndex.Value}]");

                if (!string.IsNullOrEmpty(def.metaValue))
                    sb.Append($" {def.metaValue}");

                if (!string.IsNullOrEmpty(def.conditionalTable) && !string.IsNullOrEmpty(def.conditionalColumn) && !string.IsNullOrEmpty(def.conditionalValue))
                    sb.Append($" {def.conditionalTable}::{def.conditionalColumn}={def.conditionalValue}");

                if (!string.IsNullOrEmpty(def.comment))
                    sb.Append($" // {def.comment}");

                sb.AppendLine();
            }

            File.WriteAllText(filePath, sb.ToString());
        }
    }
}
