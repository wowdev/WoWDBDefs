namespace DBDefsLib.Structs
{
    public record struct EnumEntry
    {
        public ulong value;
        public string name;
        public Build[] builds;
        public BuildRange[] buildRanges;
        public string comment;
    }
}
