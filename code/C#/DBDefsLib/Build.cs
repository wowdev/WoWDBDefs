using System;

namespace DBDefsLib
{
    public class Build : IComparable
    {
        public short expansion;
        public short major;
        public short minor;
        public uint build;

        /// <summary>
        /// Serialization requirement.
        /// </summary>
        private Build() { }

        public Build(string buildString)
        {
            var split = buildString.Split('.');

            expansion = short.Parse(split[0]);
            major = short.Parse(split[1]);
            minor = short.Parse(split[2]);
            build = uint.Parse(split[3]);
        }

        public override string ToString()
        {
            return expansion + "." + major + "." + minor + "." + build;
        }

        public override bool Equals(object obj)
        {
            var build = obj as Build;
            if (build == null)
            {
                return false;
            }

            return Equals(build);
        }

        public override int GetHashCode()
        {
            if (build > 0xFFFFF) throw new Exception("Build too large for fake hash code");
            if (minor > 0xF) throw new Exception("Minor too large for fake hash code");
            if (major > 0xF) throw new Exception("Major too large for fake hash code");
            if (expansion > 0xF) throw new Exception("Expansion too large for fake hash code");
            return (int)((uint)expansion << 28 | (uint)major << 24 | (uint)minor << 20 | build);
        }

        public bool Equals(Build build)
        {
            return build != null && build.expansion == expansion && build.major == major && build.minor == minor && build.build == this.build;
        }

        public int CompareTo(object obj)
        {
            if (obj == null) return 1;

            if (obj is Build otherBuild)
            {
                if (expansion != otherBuild.expansion)
                {
                    return expansion.CompareTo(otherBuild.expansion);
                }
                else if (major != otherBuild.major)
                {
                    return major.CompareTo(otherBuild.major);
                }
                else if (minor != otherBuild.minor)
                {
                    return minor.CompareTo(otherBuild.minor);
                }
                else if (build != otherBuild.build)
                {
                    return build.CompareTo(otherBuild.build);
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                throw new ArgumentException("Object is not a valid build!");
            }
        }


        #region Operators

        public static bool operator ==(Build x, Build y)
        {
            if (x is null && y is null)
                return true;
            if (x is null || y is null)
                return false;

            return x.Equals(y);
        }

        public static bool operator !=(Build x, Build y)
        {
            return !(x == y);
        }

        public static bool operator <(Build x, Build y)
        {
            if (x is null || y is null)
                throw new ArgumentNullException();

            return x.CompareTo(y) < 0;
        }

        public static bool operator >(Build x, Build y)
        {
            if (x is null || y is null)
                throw new ArgumentNullException();

            return x.CompareTo(y) > 0;
        }

        public static bool operator <=(Build x, Build y)
        {
            if (x == y)
                return true;

            return x < y;
        }

        public static bool operator >=(Build x, Build y)
        {
            if (x == y)
                return true;

            return x > y;
        }

        #endregion
    }
}

