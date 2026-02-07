using DBDefsLib.Constants;

namespace DBDefsLib.Structs
{
    public record struct MappingDefinition
    {
        public MetaType meta;
        public string tableName;
        public string columnName;
        public int? arrIndex;
        public string metaValue;
        public string conditionalTable;
        public string conditionalColumn;
        public string conditionalValue;
        public string comment;
    }
}
