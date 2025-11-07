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

        public Build(short expansion, short major, short minor, uint build)
        {
            this.expansion = expansion;
            this.major = major;
            this.minor = minor;
            this.build = build;
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
            return HashCode.Combine(build, minor, major, expansion);
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

        public static bool TryParse(string value, out Build result)
        {
            result = null;

            var split = value.Split('.');
            if (split.Length != 4)
                return false;

            if (!short.TryParse(split[0], out var expansion))
                return false;
            if (!short.TryParse(split[1], out var major))
                return false;
            if (!short.TryParse(split[2], out var minor))
                return false;
            if (!uint.TryParse(split[3], out var build))
                return false;

            result = new Build()
            {
                expansion = expansion,
                major = major,
                minor = minor,
                build = build,
            };

            return true;
        }
    }
}
