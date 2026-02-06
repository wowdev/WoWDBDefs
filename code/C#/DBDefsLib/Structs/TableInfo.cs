namespace DBDefsLib.Structs
{
    public struct TableInfo
    {
        public string tableName;
        public uint tableHash;
        public uint dbcFileDataID;
        public uint db2FileDataID;
        public DBDefinition dbd;
    }
}
