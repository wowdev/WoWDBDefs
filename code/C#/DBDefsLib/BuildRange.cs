using System;

namespace DBDefsLib
{
    public class BuildRange : IComparable
    {
        public Build minBuild;
        public Build maxBuild;

        public BuildRange(Build minBuild, Build maxBuild)
        {
            this.minBuild = minBuild;
            this.maxBuild = maxBuild;
        }

        public override string ToString()
        {
            return minBuild.ToString() + "-" + maxBuild.ToString();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as BuildRange);
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
                build.minor >= minBuild.minor && build.major <= maxBuild.minor && 
                build.build >= minBuild.build && build.build <= maxBuild.build;
        }
    }
}
