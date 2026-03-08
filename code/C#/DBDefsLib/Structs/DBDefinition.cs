using System.Collections.Generic;

namespace DBDefsLib.Structs
{
    public struct DBDefinition
    {
        public Dictionary<string, ColumnDefinition> columnDefinitions;
        public VersionDefinitions[] versionDefinitions;
    }
}
