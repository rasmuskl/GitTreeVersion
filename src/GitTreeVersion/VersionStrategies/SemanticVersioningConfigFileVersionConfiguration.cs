using GitTreeVersion.Context;
using GitTreeVersion.Paths;

namespace GitTreeVersion.VersionStrategies
{
    public class SemanticVersioningConfigFileVersionConfiguration : IVersionConfiguration
    {
        public SemanticVersioningConfigFileVersionConfiguration(AbsoluteDirectoryPath repositoryDirectoryPath, AbsoluteDirectoryPath versionRootPath, VersionConfig versionConfig)
        {
            Major = new VersionConfigVersionStrategy(repositoryDirectoryPath, versionRootPath, versionConfig, VersionType.Major);
            Minor = new VersionConfigVersionStrategy(repositoryDirectoryPath, versionRootPath, versionConfig, VersionType.Minor);
        }

        public IVersionStrategy Major { get; }
        public IVersionStrategy Minor { get; }
        public IVersionStrategy Patch => new CommitCountVersionStrategy();
    }
}