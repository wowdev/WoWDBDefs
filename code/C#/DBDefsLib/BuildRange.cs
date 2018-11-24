using System;

namespace DBDefsLib
{
    public class BuildRange : IComparable
    {
        public Build minBuild;
        public Build maxBuild;

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
            var split = buildRange.Split('-');
            this.minBuild = new Build(split[0]);
            this.maxBuild = new Build(split[1]);

            if (minBuild.expansion != maxBuild.expansion)
                throw new Exception("Expansion differs across build range. This is not allowed!");
        }

        public override string ToString()
        {
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

        private int CombineHashes(Object obj, int current = 0)
        {
            return current ^ obj.GetHashCode() + -1640531527 + (current << 6) + (current >> 2);
        }

        public override int GetHashCode()
        {
            return CombineHashes(CombineHashes(minBuild), maxBuild.GetHashCode());
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
    }
}
