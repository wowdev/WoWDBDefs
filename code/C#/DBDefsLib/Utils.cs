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

        public static string BuildToString(Build build)
        {
            return build.expansion + "." + build.major + "." + build.minor + "." + build.build;
        }

        public static string NormalizeColumn(string col, bool fixFirst = false)
        {
            var thingsToUpperCase = new List<string> { "ID", "WMO" };

            // ugh
            var filteredOut = new List<string> { "mid" };

            var cleaned = col;

            foreach(var thingToUpperCase in thingsToUpperCase)
            {
                if (filteredOut.Contains(col))
                {
                    continue;
                }

                if (cleaned.StartsWith(thingToUpperCase, StringComparison.CurrentCultureIgnoreCase))
                {
                    cleaned = thingToUpperCase + cleaned.Substring(thingToUpperCase.Length);
                }

                if (cleaned.EndsWith(thingToUpperCase, StringComparison.CurrentCultureIgnoreCase))
                {
                    cleaned = cleaned.Substring(0, cleaned.Length - thingToUpperCase.Length) + thingToUpperCase;
                }
            }

            if (fixFirst)
            {
                var arr = cleaned.ToCharArray();
                arr[0] = char.ToUpper(arr[0]);
                cleaned = new string(arr);
            }

            return cleaned;
        }
    }
}

