using System;
using System.Collections.Generic;
using System.Text;

namespace DBDefsDumper
{
    public struct DBMeta
    {
        public long nameOffset;
        public long cacheNameOffset;
        public int fileDataID;
        public int num_fields_in_file;
        public int record_size;
        public int num_fields;
        public int id_column;
        public byte sparseTable;
        public long field_offsets_offs;
        public long field_sizes_offs;
        public long field_types_offs;
        public long field_flags_offs;
        public long field_sizes_in_file_offs;
        public long field_types_in_file_offs;
        public long field_flags_in_file_offs;
        public byte flags_58_2_1;
        public int table_hash;
        public int layout_hash;
        public byte flags_68_4_2_1;
        public int nbUniqueIdxByInt;
        public int nbUniqueIdxByString;
        public long uniqueIdxByInt;
        public long uniqueIdxByString;
        public byte bool_88;
        public int column_8C;
        public int column_90;
        public long sortFunctionOffs;
        public long table_name;
        public byte bool_C0;
        public long string_ref_offs;
        public long field_encrypted;
        public long sql_query;
        public long dbFilenameOffs;
        public int siblingTableHash;
        public long namesInFileOffs;
        /* 
        //probs not in osx
        const char** field_names_in_file; 
        const char** field_names;     
        const char* fk_clause;        
        */
    }
}
