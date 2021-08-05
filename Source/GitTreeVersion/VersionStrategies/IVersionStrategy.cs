using GitTreeVersion.Paths;

namespace GitTreeVersion.VersionStrategies
{
    public interface IVersionStrategy
    {
        VersionComponent GetVersionComponent(AbsoluteDirectoryPath versionRootPath, AbsoluteDirectoryPath[] relevantPaths, string? range);
    }
}