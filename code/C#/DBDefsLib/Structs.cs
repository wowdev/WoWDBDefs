using System.Collections.Generic;

namespace DBDefsLib
{
    public class Structs
    {
        public struct DBDefinition
        {
            public Dictionary<string, ColumnDefinition> columnDefinitions;
            public VersionDefinitions[] versionDefinitions;
        }

        public struct VersionDefinitions
        {
            public Build[] builds;
            public BuildRange[] buildRanges;
            public string[] layoutHashes;
            public string comment;
            public Definition[] definitions;
        }

        public struct BuildRange
        {
            public Build minBuild;
            public Build maxBuild;
        }

        public struct Build
        {
            public short expansion;
            public short major;
            public short minor;
            public uint build;
        }

        public struct Definition
        {
            public int size;
            public int arrLength;
            public string name;
            public bool isID;
            public bool isRelation;
            public bool isInline;
            public string comment;
            public string typeOverride;
        }

        public struct ColumnDefinition
        {
            public string type;
            public string foreignTable;
            public string foreignColumn;
            public bool verified;
            public string comment;
        }
    }
}
