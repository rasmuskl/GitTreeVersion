using Semver;

namespace GitTreeVersion.VersionStrategies
{
    public class SemanticVersioningConfigFileVersionConfiguration : IVersionConfiguration
    {
        public SemanticVersioningConfigFileVersionConfiguration(VersionConfig versionConfig)
        {
            SemVersion semVersion = !string.IsNullOrWhiteSpace(versionConfig.BaseVersion)
                ? SemVersion.Parse(versionConfig.BaseVersion)
                : new SemVersion(0);

            Major = new FixedVersionStrategy(semVersion.Major);
            Minor = new FixedVersionStrategy(semVersion.Minor);
        }

        public IVersionStrategy Major { get; }
        public IVersionStrategy Minor { get; }
        public IVersionStrategy Patch => new CommitCountVersionStrategy();
    }
}