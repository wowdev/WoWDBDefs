using DBDefsLib.Constants;
using DBDefsLib.Structs;
using System.IO;
using System.Text;

namespace DBDefsLib
{
    public class DBDEnumWriter
    {
        public void Save(EnumDefinition enumDefinition, string filePath)
        {
            var sb = new StringBuilder();

            foreach (var entry in enumDefinition.entries)
            {
                if ((entry.builds != null && entry.builds.Length > 0) || entry.buildRanges != null && entry.buildRanges.Length > 0)
                {
                    sb.Append($"(BUILD ");

                    if (entry.builds != null && entry.builds.Length > 0)
                    {
                        for (int i = 0; i < entry.builds.Length; i++)
                        {
                            sb.Append(entry.builds[i].ToString());
                            if (i < entry.builds.Length - 1 || (entry.buildRanges != null && entry.buildRanges.Length > 0))
                                sb.Append(", ");
                        }
                    }

                    if (entry.buildRanges != null && entry.buildRanges.Length > 0)
                    {
                        for (int i = 0; i < entry.buildRanges.Length; i++)
                        {
                            sb.Append(entry.buildRanges[i].ToString());
                            if (i < entry.buildRanges.Length - 1)
                                sb.Append(", ");
                        }
                    }

                    sb.Append(") ");
                }

                if (enumDefinition.metaType == MetaType.FLAGS)
                    sb.Append("0x" + entry.value.ToString("X"));
                else
                    sb.Append(entry.value);

                if (!string.IsNullOrEmpty(entry.name))
                    sb.Append($" {entry.name}");

                if (!string.IsNullOrEmpty(entry.comment))
                    sb.Append($" // {entry.comment}");

                sb.AppendLine();
            }

            File.WriteAllText(filePath, sb.ToString());
        }
    }
}
