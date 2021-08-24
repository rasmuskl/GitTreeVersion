using GitTreeVersion.Paths;
using Semver;

namespace GitTreeVersion.BuildEnvironments
{
    public interface IBuildEnvironment
    {
        string? GetPrerelease(AbsoluteDirectoryPath versionRootPath, AbsoluteDirectoryPath[] relevantPaths);
        void SetBuildNumber(SemVersion version);
    }
}