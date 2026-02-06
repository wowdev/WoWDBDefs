namespace DBDefsLib.Structs
{
    public struct ColumnDefinition
    {
        public string type;
        public string foreignTable;
        public string foreignColumn;
        public bool verified;
        public string comment;
    }
}
