namespace DBDTest.Structs
{
    public struct WDBCHeader
    {
        public uint recordCount;
        public uint fieldCount;
        public uint recordSize;
        public uint stringTableSize;
    }

    public struct WDB2Header // Also WDB3 header
    {
        public uint recordCount;
        public uint fieldCount;
        public uint recordSize;
        public uint stringTableSize;
        public uint tableHash;
        public uint build;
        public uint timestampLastWritten;
        public uint minID;
        public uint maxID;
        public uint locale;
        public uint copyTableSize;
    }

    public struct WDB4Header
    {
        public uint recordCount;
        public uint fieldCount;
        public uint recordSize;
        public uint stringTableSize;
        public uint tableHash;
        public uint build;
        public uint timestampLastWritten;
        public uint minID;
        public uint maxID;
        public uint locale;
        public uint copyTableSize;
        public uint flags;
    }

    public struct WDB5Header
    {
        public uint recordCount;
        public uint fieldCount;
        public uint recordSize;
        public uint stringTableSize;
        public uint tableHash;
        public uint layoutHash;
        public uint timestampLastWritten;
        public uint minID;
        public uint maxID;
        public uint locale;
        public uint copyTableSize;
        public uint flags;
        public uint idIndex;
    }

    public struct WDB6Header
    {
        public uint recordCount;
        public uint fieldCount;
        public uint recordSize;
        public uint stringTableSize;
        public uint tableHash;
        public uint layoutHash;
        public uint timestampLastWritten;
        public uint minID;
        public uint maxID;
        public uint locale;
        public uint copyTableSize;
        public uint flags;
        public uint idIndex;
        public uint totalFieldCount;
        public uint commonTableSize;
    }

    public struct WDC1Header
    {
        public uint recordCount;
        public uint fieldCount;
        public uint recordSize;
        public uint stringTableSize;
        public uint tableHash;
        public uint layoutHash;
        public uint timestampLastWritten;
        public uint minID;
        public uint maxID;
        public uint locale;
        public uint copyTableSize;
        public uint flags;
        public uint idIndex;
        public uint totalFieldCount;
        public uint bitpackedDataOffset;
        public uint lookupColumnCount;
        public uint offsetMapOffset;
        public uint idListSize;
        public uint fieldStorageInfoSize;
        public uint commonDataSize;
        public uint palletDataSize;
        public uint relationshipDataSize;
    }
}
