namespace DBDTest.Structs
{
    public struct FileDefinition
    {
        public uint fieldCount;
        public uint recordSize;
        public uint build;
        public string layoutHash;
        public FieldStructure[] fields;
    }

    public struct FieldStructure
    {
        public uint fieldSizeBits;
        public uint arrayCount;
        public ushort fieldSize;
        public ushort fieldOffset;
    }

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
        public ushort flags;
        public ushort idIndex;
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
        public ushort flags;
        public ushort idIndex;
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
        public uint minID;
        public uint maxID;
        public uint locale;
        public uint copyTableSize;
        public ushort flags;
        public ushort idIndex;
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

    public struct WDC2Header
    {
        public uint recordCount;
        public uint fieldCount;
        public uint recordSize;
        public uint stringTableSize;
        public uint tableHash;
        public uint layoutHash;
        public uint minID;
        public uint maxID;
        public uint locale;
        public ushort flags;
        public ushort idIndex;
        public uint totalFieldCount;
        public uint bitpackedDataOffset;
        public uint lookupColumnCount;
        public uint fieldStorageInfoSize;
        public uint commonDataSize;
        public uint palletDataSize;
        public uint sectionCount;
    }

    public struct WDC3Header
    {
        public uint recordCount;
        public uint fieldCount;
        public uint recordSize;
        public uint stringTableSize;
        public uint tableHash;
        public uint layoutHash;
        public uint minID;
        public uint maxID;
        public uint locale;
        public ushort flags;
        public ushort idIndex;
        public uint totalFieldCount;
        public uint bitpackedDataOffset;
        public uint lookupColumnCount;
        public uint fieldStorageInfoSize;
        public uint commonDataSize;
        public uint palletDataSize;
        public uint sectionCount;
    }
}
