using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DBDefsLib
{
    public class BuildRange : IComparable
    {
        private static readonly ReadOnlyDictionary<string, (Build minBuild, Build maxBuild)> presetRanges = new(new Dictionary<string, (Build minBuild, Build maxBuild)>
        {
            { "Pre", (new Build(0, 3, 4, 2953), new Build(0, 12, 0, 3988))  },
            { "Vanilla", (new Build(1, 0, 0, 3980), new Build(1, 12, 3, 6141)) },
            { "TBC", (new Build(2, 0, 0, 5610), new Build(2, 4, 3, 8606)) },
            { "WotLK", (new Build(3, 0, 0, 8120), new Build(3, 3, 5, 12340)) },
            { "Cata", (new Build(4, 0, 0, 11927), new Build(4, 3, 4, 15595)) },
            { "MoP", (new Build(5, 0, 1, 15464), new Build(5, 4, 8, 18414)) },
            { "WoD", (new Build(6, 0, 1, 18125), new Build(6, 2, 4, 21742)) },
            { "Legion", (new Build(7, 0, 1, 20740), new Build(7, 3, 5, 26972)) },
            { "BfA", (new Build(8, 0, 1, 25703), new Build(8, 3, 7, 35662)) },
            { "SL", (new Build(9, 0, 1, 33411), new Build(9, 2, 7, 45745)) },
            { "DF", (new Build(10, 0, 0, 43342), new Build(10, 2, 7, 55664)) },
            { "TWW", (new Build(11, 0, 0, 54311), new Build(11, 2, 7, 65299)) },
            // Midnight
            // TLT
        });

        public Build minBuild;
        public Build maxBuild;
        private bool InitializedAsPreset = false;

        /// <summary>
        /// Serialization requirement.
        /// </summary>
        private BuildRange() { }

        public BuildRange(Build minBuild, Build maxBuild)
        {
            this.minBuild = minBuild;
            this.maxBuild = maxBuild;

            if (minBuild.expansion != maxBuild.expansion)
                throw new Exception("Expansion differs across build range. This is not allowed!");
        }

        public BuildRange(string buildRange)
        {
            if (presetRanges.TryGetValue(buildRange, out var presetRange))
            {
                this.minBuild = presetRange.minBuild;
                this.maxBuild = presetRange.maxBuild;
                InitializedAsPreset = true;
                return;
            }

            var split = buildRange.Split('-');
            this.minBuild = new Build(split[0]);
            this.maxBuild = new Build(split[1]);

            if (minBuild.expansion != maxBuild.expansion)
                throw new Exception("Expansion differs across build range. This is not allowed!");
        }

        public override string ToString()
        {
            if (InitializedAsPreset)
            {
                foreach(var preset in presetRanges)
                    if(preset.Value.minBuild == minBuild && preset.Value.maxBuild == maxBuild)
                        return preset.Key;
            }

            return minBuild.ToString() + "-" + maxBuild.ToString();
        }

        public override bool Equals(object obj)
        {
            var buildRange = obj as BuildRange;
            if (buildRange == null)
            {
                return false;
            }

            return Equals(buildRange);
        }

        public bool Equals(BuildRange buildRange)
        {
            return minBuild.Equals(buildRange.minBuild) && maxBuild.Equals(buildRange.maxBuild);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(minBuild, maxBuild);
        }

        public int CompareTo(object obj)
        {
            if (obj == null) return 1;

            if (obj is BuildRange otherBuildRange)
            {
                if (minBuild != otherBuildRange.minBuild)
                {
                    return minBuild.CompareTo(otherBuildRange.minBuild);
                }
                else if (maxBuild != otherBuildRange.maxBuild)
                {
                    return maxBuild.CompareTo(otherBuildRange.maxBuild);
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                throw new ArgumentException("Object is not a valid build range!");
            }
        }

        public bool Contains(Build build)
        {
            return
                build.expansion >= minBuild.expansion && build.expansion <= maxBuild.expansion &&
                build.major >= minBuild.major && build.major <= maxBuild.major &&
                build.build >= minBuild.build && build.build <= maxBuild.build;
        }

        public bool Union(BuildRange buildRange, out BuildRange unionedRange)
        {
            unionedRange = null;

            if (buildRange.Contains(minBuild) ||
                buildRange.Contains(maxBuild) ||
                Contains(buildRange.minBuild) ||
                Contains(buildRange.maxBuild))
            {
                Build min = minBuild, max = maxBuild;

                if (buildRange.minBuild < min)
                    min = buildRange.minBuild;
                if (buildRange.maxBuild > max)
                    max = buildRange.maxBuild;

                unionedRange = new BuildRange(min, max);
                return true;
            }
            else
            {
                return false;
            }
        }

        #region Operators

        public static bool operator ==(BuildRange x, BuildRange y)
        {
            if (x is null && y is null)
                return true;
            if (x is null || y is null)
                return false;

            return x.Equals(y);
        }

        public static bool operator !=(BuildRange x, BuildRange y)
        {
            return !(x == y);
        }

        public static bool operator <(BuildRange x, BuildRange y)
        {
            if (x is null || y is null)
                throw new ArgumentNullException();

            return x.CompareTo(y) < 0;
        }

        public static bool operator >(BuildRange x, BuildRange y)
        {
            if (x is null || y is null)
                throw new ArgumentNullException();

            return x.CompareTo(y) > 0;
        }

        public static bool operator <=(BuildRange x, BuildRange y)
        {
            if (x == y)
                return true;

            return x < y;
        }

        public static bool operator >=(BuildRange x, BuildRange y)
        {
            if (x == y)
                return true;

            return x > y;
        }

        #endregion

        public static bool TryParse(string value, out BuildRange result)
        {
            result = null;

            if (presetRanges.TryGetValue(value, out var presetRange))
            {
                result = new BuildRange()
                {
                    minBuild = presetRange.minBuild,
                    maxBuild = presetRange.maxBuild,
                    InitializedAsPreset = true
                };

                return true;
            }

            var parts = value.Split('-');
            if (parts.Length != 2)
                return false;

            if (!Build.TryParse(parts[0], out var minBuild))
                return false;
            if (!Build.TryParse(parts[1], out var maxBuild))
                return false;

            if (minBuild.expansion != maxBuild.expansion)
                throw new Exception("Expansion differs across build range. This is not allowed!");

            result = new BuildRange()
            {
                minBuild = minBuild,
                maxBuild = maxBuild
            };

            return true;
        }
    }
}
