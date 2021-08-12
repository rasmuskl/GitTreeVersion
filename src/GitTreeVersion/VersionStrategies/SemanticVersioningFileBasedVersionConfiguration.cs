using Semver;

namespace GitTreeVersion.VersionStrategies
{
    public class SemanticVersioningFileBasedVersionConfiguration : IVersionConfiguration
    {
        public SemanticVersioningFileBasedVersionConfiguration(VersionConfig versionConfig)
        {
            var semVersion = SemVersion.Parse(versionConfig.Version);
            Major = new FixedVersionStrategy(semVersion.Major);
            Minor = new FixedVersionStrategy(semVersion.Minor);
        }

        public IVersionStrategy Major { get; }
        public IVersionStrategy Minor { get; }
        public IVersionStrategy Patch => new CommitCountVersionStrategy();
    }
}