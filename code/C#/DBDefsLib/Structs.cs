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
            public string[] builds;
            public string[] layoutHashes;
            public Definition[] definitions;
        }

        public struct Definition
        {
            public int size;
            public int arrLength;
            public string name;
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
