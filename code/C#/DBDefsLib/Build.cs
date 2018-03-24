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
    }
}
