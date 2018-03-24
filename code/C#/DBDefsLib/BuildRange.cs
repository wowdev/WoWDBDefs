namespace DBDefsLib
{
    public class BuildRange
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
