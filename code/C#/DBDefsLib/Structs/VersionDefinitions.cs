namespace DBDefsLib.Structs
{
    public struct VersionDefinitions
    {
        public Build[] builds;
        public BuildRange[] buildRanges;
        public string[] layoutHashes;
        public string comment;
        public Definition[] definitions;
    }
}
