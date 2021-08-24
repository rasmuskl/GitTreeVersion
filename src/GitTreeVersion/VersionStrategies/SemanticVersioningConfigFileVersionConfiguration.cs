using GitTreeVersion.Context;
using GitTreeVersion.Paths;

namespace GitTreeVersion.VersionStrategies
{
    public class SemanticVersioningConfigFileVersionConfiguration : IVersionConfiguration
    {
        public SemanticVersioningConfigFileVersionConfiguration(AbsoluteDirectoryPath versionRootPath, VersionConfig versionConfig)
        {
            Major = new VersionConfigVersionStrategy(versionRootPath, versionConfig, VersionType.Major);
            Minor = new VersionConfigVersionStrategy(versionRootPath, versionConfig, VersionType.Minor);
        }

        public IVersionStrategy Major { get; }
        public IVersionStrategy Minor { get; }
        public IVersionStrategy Patch => new CommitCountVersionStrategy();
    }
}