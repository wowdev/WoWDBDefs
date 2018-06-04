using System.Collections.Generic;

namespace DBDefsDumper
{
    class PatternBuilder
    {
        public List<Pattern> patterns = new List<Pattern>();

        public PatternBuilder()
        {
            patterns.Add(
            new Pattern("8.0.1")
                .Pointer(Name.DB_NAME)
                .FileDataID(Name.FDID)
                .FieldReference(Name.NUM_FIELD_IN_FILE)
                .RecordSize(Name.RECORD_SIZE)
                .FieldReference(Name.NUM_FIELD)
                .OptionalFieldReference(Name.ID_COLUMN)
                .Boolean(Name.SPARSE_TABLE)
                .Pointer(Name.FIELD_OFFSETS)
                .Pointer(Name.FIELD_SIZES)
                .Pointer(Name.FIELD_TYPES)
                .Pointer(Name.FIELD_FLAGS)
                .Pointer(Name.FIELD_SIZES_IN_FILE)
                .Pointer(Name.FIELD_TYPES_IN_FILE)
                .Pointer(Name.FIELD_FLAGS_IN_FILE)
                .Uint8(Name.FLAGS_58_21)
                .Hash(Name.TABLE_HASH)
                .Append("00", "00", "00", "00")
                .Hash(Name.LAYOUT_HASH)
                .Uint8(Name.FLAGS_68_421)
                .FieldReference(Name.FIELD_NUM_IDX_INT)
                .FieldReference(Name.FIELD_NUM_IDX_STRING)
                .OptionalPointer(Name.FIELD_IDX_INT)
                .OptionalPointer(Name.FIELD_IDX_STRING)
                .Boolean(Name.UNK88)
                .OptionalFieldReference(Name.FIELD_RELATION)
                .OptionalFieldReference(Name.FIELD_RELATION_IN_FILE)
                .OptionalPointer(Name.SORT_FUNC)
                .Boolean(Name.UNKC0)
            );

            patterns.Add(
            new Pattern("7.3.5-release")
                .Pointer(Name.DB_NAME)
                .FieldReference(Name.NUM_FIELD_IN_FILE)
                .RecordSize(Name.RECORD_SIZE)
                .FieldReference(Name.NUM_FIELD)
                .OptionalFieldReference(Name.ID_COLUMN)
                .Boolean(Name.SPARSE_TABLE)
                .Pointer(Name.FIELD_OFFSETS)
                .Pointer(Name.FIELD_SIZES)
                .Pointer(Name.FIELD_TYPES)
                .Pointer(Name.FIELD_FLAGS)
                .Pointer(Name.FIELD_SIZES_IN_FILE)
                .Pointer(Name.FIELD_TYPES_IN_FILE)
                .Pointer(Name.FIELD_FLAGS_IN_FILE)
                .Uint8(Name.FLAGS_58_21)
                .Hash(Name.TABLE_HASH)
                .Append("00", "00", "00", "00")
                .Hash(Name.LAYOUT_HASH)
                .Uint8(Name.FLAGS_68_421)
                .FieldReference(Name.FIELD_NUM_IDX_INT)
                .FieldReference(Name.FIELD_NUM_IDX_STRING)
                .OptionalPointer(Name.FIELD_IDX_INT)
                .OptionalPointer(Name.FIELD_IDX_STRING)
                .Boolean(Name.UNK88)
                .OptionalFieldReference(Name.FIELD_RELATION)
                .OptionalFieldReference(Name.FIELD_RELATION_IN_FILE)
                .OptionalPointer(Name.SORT_FUNC)
                .Boolean(Name.UNKC0)
            );

            patterns.Add(
                new Pattern("7.{2.5,3.{0,2}}-release") // note: also matches release-7.3.5 even though different struct
                .Pointer(Name.DB_NAME)
                .FieldReference(Name.NUM_FIELD_IN_FILE)
                .RecordSize(Name.RECORD_SIZE)
                .FieldReference(Name.NUM_FIELD)
                .OptionalFieldReference(Name.ID_COLUMN)
                .Boolean(Name.SPARSE_TABLE)
                .Pointer(Name.FIELD_OFFSETS)
                .Pointer(Name.FIELD_SIZES)
                .Pointer(Name.FIELD_TYPES)
                .Pointer(Name.FIELD_FLAGS)
                .Pointer(Name.FIELD_SIZES_IN_FILE)
                .Pointer(Name.FIELD_TYPES_IN_FILE)
                .Pointer(Name.FIELD_FLAGS_IN_FILE)
                .Uint8(Name.FLAGS_58_21)
                .Hash(Name.TABLE_HASH)
                .Append("00", "00", "00", "00")
                .Hash(Name.LAYOUT_HASH)
                .Uint8(Name.FLAGS_68_421)
                .FieldReference(Name.FIELD_NUM_IDX_INT)
                .FieldReference(Name.FIELD_NUM_IDX_STRING)
                .OptionalPointer(Name.FIELD_IDX_INT)
                .OptionalPointer(Name.FIELD_IDX_STRING)
                .OptionalFieldReference(Name.FIELD_RELATION)
                .OptionalFieldReference(Name.FIELD_RELATION_IN_FILE)
                .OptionalPointer(Name.SORT_FUNC)
                .Boolean(Name.UNKC0)
            );

            patterns.Add(
                new Pattern("6.0.1-db2-internal") // note: conflicts with internal-6.0.1-dbc
               .Pointer(Name.DB_FILENAME)
               .Pointer(Name.DB_CACHE_FILENAME)
               .FieldReference(Name.NUM_FIELD_IN_FILE)
               .RecordSize(Name.RECORD_SIZE)
               .Pointer(Name.CONVERT_STRINGREFS)
               .Append("00", "00", "00", "00")
               .Append("?", "?", "?", "?")
               .Pointer(Name.FIELD_OFFSETS)
               .Pointer(Name.FIELD_SIZES_IN_FILE)
               .Pointer(Name.FIELD_TYPES_IN_FILE)
               .Hash(Name.TABLE_HASH)
               .Append("?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?")
               .Pointer(Name.FIELD_ENCRYPTED)
            );

            patterns.Add(
                new Pattern("6.0.1-dbc-internal") // note: conflicts with internal-6.0.1-db2
               .Pointer(Name.DB_FILENAME)
               .FieldReference(Name.NUM_FIELD_IN_FILE)
               .RecordSize(Name.RECORD_SIZE)
               .Pointer(Name.CONVERT_STRINGREFS)
               .Append("00", "00", "00", "00")
               .Append("?", "?", "?", "?")
               .Pointer(Name.FIELD_OFFSETS)
               .Pointer(Name.FIELD_SIZES_IN_FILE)
               .Pointer(Name.FIELD_TYPES_IN_FILE)
               .Append("?", "?", "?", "?")
               .Append("00", "00", "00", "00")
               .Pointer(Name.FIELD_ENCRYPTED)
               .Pointer(Name.FIELD_NAMES_IN_FILE)
               .Pointer(Name.SQL_QUERY)
            );
            
            patterns.Add(
                new Pattern("5.0.1-dbc-internal") // note: subset of internal-6.0.1-dbc, so conflicts with that
               .Pointer(Name.DB_FILENAME)
               .FieldReference(Name.NUM_FIELD_IN_FILE)
               .RecordSize(Name.RECORD_SIZE)
               .Pointer(Name.CONVERT_STRINGREFS)
               .Append("?", "?", "?", "?")
               .Append("?", "?", "?", "?")
               .Pointer(Name.FIELD_OFFSETS)
               .Pointer(Name.FIELD_SIZES_IN_FILE)
               .Pointer(Name.FIELD_TYPES_IN_FILE)
            );

            patterns.Add(
                new Pattern("5.0.1-db2-internal") // note: subset of internal-6.0.1-db2, so conflicts with that and internal-5.0.1-dbc
               .Pointer(Name.DB_FILENAME)
               .Pointer(Name.DB_CACHE_FILENAME)
               .FieldReference(Name.NUM_FIELD_IN_FILE)
               .RecordSize(Name.RECORD_SIZE)
               .Pointer(Name.CONVERT_STRINGREFS)
               .Append("?", "?", "?", "?")
               .Append("?", "?", "?", "?")
               .Pointer(Name.FIELD_OFFSETS)
               .Pointer(Name.FIELD_SIZES_IN_FILE)
               .Pointer(Name.FIELD_TYPES_IN_FILE)
               .Hash(Name.TABLE_HASH)
            );
        }
    }
}
