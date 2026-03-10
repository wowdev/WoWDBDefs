namespace DBDefsLib.Structs
{
    public record struct EnumEntry
    {
        public long value;
        public string name;
        public Build[] builds;
        public BuildRange[] buildRanges;
        public string comment;
    }
}
