using System;
using System.Collections.Generic;
using System.Text;
using static DBDefsLib.Structs;

namespace DBDefsLib
{
    public class Utils
    {
        public static Build ParseBuild(string build)
        {
            var split = build.Split('.');

            return new Build()
            {
                expansion = short.Parse(split[0]),
                major = short.Parse(split[1]),
                minor = short.Parse(split[2]),
                build = uint.Parse(split[3])
            };
        }
    }
}

