using GitTreeVersion.Paths;

namespace GitTreeVersion.BuildEnvironments
{
    public interface IBuildEnvironment
    {
        string? GetPrerelease(AbsoluteDirectoryPath versionRootPath, AbsoluteDirectoryPath[] relevantPaths);
    }
}