using System.IO;

namespace DBDefsDumper.Versions
{
    class v7_3_5_25807
    {
        public static DBMeta ReadMeta(BinaryReader bin)
        {
            var meta = new DBMeta();

            meta.nameOffset = bin.ReadInt64();
            meta.num_fields_in_file = bin.ReadInt32();
            meta.record_size = bin.ReadInt32();
            meta.num_fields = bin.ReadInt32();
            meta.id_column = bin.ReadInt32();
            meta.sparseTable = bin.ReadByte();
            bin.ReadBytes(7);
            meta.field_offsets_offs = bin.ReadInt64();
            meta.field_sizes_offs = bin.ReadInt64();
            meta.field_types_offs = bin.ReadInt64();
            meta.field_flags_offs = bin.ReadInt64();
            meta.field_sizes_in_file_offs = bin.ReadInt64();
            meta.field_types_in_file_offs = bin.ReadInt64();
            meta.field_flags_in_file_offs = bin.ReadInt64();
            meta.flags_58_2_1 = bin.ReadByte();
            bin.ReadBytes(3);
            meta.table_hash = bin.ReadInt32();
            bin.ReadBytes(4);
            meta.layout_hash = bin.ReadInt32();
            meta.flags_68_4_2_1 = bin.ReadByte();
            bin.ReadBytes(3);
            meta.nbUniqueIdxByInt = bin.ReadInt32();
            meta.nbUniqueIdxByString = bin.ReadInt32();
            bin.ReadBytes(4);
            meta.uniqueIdxByInt = bin.ReadInt64();
            meta.uniqueIdxByString = bin.ReadInt64();
            meta.bool_88 = bin.ReadByte();
            bin.ReadBytes(3);
            meta.column_8C = bin.ReadInt32();
            meta.column_90 = bin.ReadInt32();
            bin.ReadBytes(4);
            meta.sortFunctionOffs = bin.ReadInt64();
            meta.table_name = bin.ReadInt64();

            return meta;
        }

        public static string GetPattern()
        {
            var pattern__pointer = "? ? ? ? 01 00 00 00 ";
            var pattern__optional_pointer = "? ? ? ? ? 00 00 00 ";
            var pattern__boolean = "? 00 00 00 ";
            var pattern__uint8 = pattern__boolean;
            var pattern__field_reference = "? ? 00 00 ";
            var pattern__field_reference_or_none = "? ? ? ? ";
            var pattern__hash = "? ? ? ? ";
            var pattern__4_byte_padding = "00 00 00 00 ";
            var pattern__record_size = "? ? 00 00 ";

            return pattern__pointer
            + pattern__field_reference
            + pattern__record_size
            + pattern__field_reference
            + pattern__field_reference_or_none
            + pattern__boolean
            + pattern__4_byte_padding
            + pattern__pointer
            + pattern__pointer
            + pattern__pointer
            + pattern__pointer
            + pattern__pointer
            + pattern__pointer
            + pattern__pointer
            + pattern__uint8
            + pattern__hash
            + pattern__4_byte_padding
            + pattern__hash
            + pattern__uint8
            + pattern__field_reference
            + pattern__field_reference
            + pattern__4_byte_padding
            + pattern__optional_pointer
            + pattern__optional_pointer
            + pattern__boolean
            + pattern__field_reference_or_none
            + pattern__field_reference_or_none
            + pattern__4_byte_padding
            + pattern__optional_pointer
            + pattern__boolean
            + pattern__4_byte_padding;
        }
    }
}
