using GitTreeVersion.Paths;

namespace GitTreeVersion.VersionStrategies
{
    public class ZeroVersionStrategy : IVersionStrategy
    {
        public VersionComponent GetVersionComponent(AbsoluteDirectoryPath versionRootPath, AbsoluteDirectoryPath[] relevantPaths, string? range)
        {
            return new VersionComponent(0, range);
        }
    }
}