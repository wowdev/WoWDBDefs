using System;

namespace DBDefsLib
{
    public class Build
    {
        public short expansion;
        public short major;
        public short minor;
        public uint build;

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
            return Equals(obj as Build);
        }

        public override int GetHashCode()
        {
            if (build       > 0xFFFFF) throw new Exception("Build too large for fake hash code");
            if (minor       > 0xF) throw new Exception("Minor too large for fake hash code");
            if (major       > 0xF) throw new Exception("Major too large for fake hash code");
            if (expansion   > 0xF) throw new Exception("Expansion too large for fake hash code");
            return (int)((uint)expansion << 28 | (uint)major << 24 | (uint)minor << 20 | build);
        }

        public bool Equals(Build build)
        {
            return build != null && build.expansion == expansion && build.major == major && build.minor == minor && build.build == this.build;
        }
    }
}

