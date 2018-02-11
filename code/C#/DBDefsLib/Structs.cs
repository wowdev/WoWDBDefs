using System;
using System.Collections.Generic;
using System.Text;

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
            public int[] builds;
            public string[] layoutHashes;
            public Definition[] definitions;
        }

        public struct Definition
        {
            public int length;
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
