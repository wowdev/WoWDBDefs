using System.Collections.Generic;

namespace DBDefsDumper
{
    class Pattern
    {
        public string name;
        public List<string> compatiblePatches;
        public int cur_pos;
        public string cur_pattern;
        public int minBuild;
        public int maxBuild;
        public Dictionary<string, int> offsets = new Dictionary<string, int>();

        public Pattern(string name, List<string> compatiblePatches, int minBuild, int maxBuild)
        {
            this.name = name;
            this.compatiblePatches = compatiblePatches;
            this.cur_pos = 0;
            this.cur_pattern = "";
            this.minBuild = minBuild;
            this.maxBuild = maxBuild;
        }

        public Pattern(string name, List<string> compatiblePatches)
        {
            this.name = name;
            this.compatiblePatches = compatiblePatches;
            this.cur_pos = 0;
            this.cur_pattern = "";
            this.minBuild = 0;
            this.maxBuild = 0;
        }

        // Utilities
        public Pattern Append(params string[] args)
        {
            foreach (var arg in args)
            {
                this.cur_pattern += arg + " ";
                this.cur_pos += 1;
            }
            return this;
        }

        public Pattern PadTo(int align)
        {
            while (this.cur_pos % align != 0)
            {
                this.Append("00");
            }
            return this;
        }

        public void Remember(string name)
        {
            offsets[name] = this.cur_pos;
        }

        // Types
        public Pattern Pointer(string name)
        {
            this.PadTo(8);
            this.Remember(name);
            this.Append("?", "?", "?", "?", "01", "00", "00", "00");
            return this;
        }

        public Pattern OptionalPointer(string name)
        {
            this.PadTo(8);
            this.Remember(name);
            this.Append("?", "?", "?", "?", "?", "00", "00", "00");
            return this;
        }

        public Pattern FileDataID(string name)
        {
            this.PadTo(4);
            this.Remember(name);
            this.Append("?", "?", "?", "?");
            return this;
        }

        public Pattern FieldReference(string name)
        {
            this.PadTo(4);
            this.Remember(name);
            this.Append("?", "?", "00", "00");
            return this;
        }

        public Pattern OptionalFieldReference(string name)
        {
            this.PadTo(4);
            this.Remember(name);
            this.Append("?", "?", "?", "?");
            return this;
        }

        public Pattern RecordSize(string name)
        {
            return this.FieldReference(name);
        }

        public Pattern Uint8(string name)
        {
            this.PadTo(1);
            this.Remember(name);
            this.Append("?");
            return this;
        }

        public Pattern Boolean(string name)
        {
            return this.Uint8(name);
        }

        public Pattern Hash(string name)
        {
            this.PadTo(4);
            this.Remember(name);
            this.Append("?", "?", "?", "?");
            return this;
        }
    }

    class Name
    {
        public const string DB_NAME = "db name";
        public const string DB_FILENAME = "db file name";
        public const string DB_CACHE_FILENAME = "db adb file name";
        public const string FDID = "fdid";
        public const string NUM_FIELD_IN_FILE = "fields in file";
        public const string RECORD_SIZE = "record size";
        public const string NUM_FIELD = "fields";
        public const string ID_COLUMN = "id column";
        public const string SPARSE_TABLE = "sparse table";
        public const string FIELD_OFFSETS = "offsets";
        public const string FIELD_SIZES = "sizes";
        public const string FIELD_TYPES = "types";
        public const string FIELD_FLAGS = "flags";
        public const string FIELD_NAMES = "field names";
        public const string FIELD_SIZES_IN_FILE = "sizes in file";
        public const string FIELD_TYPES_IN_FILE = "types in file";
        public const string FIELD_FLAGS_IN_FILE = "flags in file";
        public const string FIELD_NAMES_IN_FILE = "names in file";
        public const string DB_NAME_DUPLICATE = "duplicate name";
        public const string FLAGS_58_21 = "flags 58: 2|1";
        public const string TABLE_HASH = "table";
        public const string SIBLING_TABLE_HASH = "the sparse, or non-sparse equivalent";
        public const string LAYOUT_HASH = "layout";
        public const string FLAGS_68_421 = "flags 68: 4|2|1";
        public const string FIELD_NUM_IDX_INT = "nbUniqueIdxByInt";
        public const string FIELD_NUM_IDX_STRING = "nbUniqueIdxByString";
        public const string FIELD_IDX_INT = "uniqueIdxByInt";
        public const string FIELD_IDX_STRING = "uniqueIdxByString";
        public const string UNK88 = "unk88";
        public const string FIELD_RELATION = "relation";
        public const string FIELD_RELATION_IN_FILE = "relation in file";
        public const string SORT_FUNC = "sort function";
        public const string UNKC0 = "unkC0";
        public const string CONVERT_STRINGREFS = "convert stringrefs";
        public const string FIELD_ENCRYPTED = "encrypted";
        public const string SQL_QUERY = "sql query";
        public const string UNK_BOOL_601_x24 = "unknown bool, always true";
        public const string UNK_FLAGS_601_x48_421 = "possibly flags: 4|2|1"; // todo is this FLAGS_68_421?
        public const string UNK_BOOL_601dbc_x38 = "unkown bool x38 6.0.1";
        public const string UNK_BOOL_601dbc_x39 = "unkown bool x39 6.0.1";
        public const string UNK_BOOL_601dbc_x3a = "unkown bool x3a 6.0.1, always false";
        public const string UNK_BOOL_601dbc_x3b = "unkown bool x3b 6.0.1";
    }
}
