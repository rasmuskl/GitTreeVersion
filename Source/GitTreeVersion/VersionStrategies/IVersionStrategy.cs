using GitTreeVersion.Paths;

namespace GitTreeVersion
{
    public interface IVersionStrategy
    {
        VersionComponent GetVersionComponent(AbsoluteDirectoryPath versionRootPath, AbsoluteDirectoryPath[] relevantPaths, string? range);
    }
}