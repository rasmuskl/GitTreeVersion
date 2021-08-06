using GitTreeVersion.Paths;

namespace GitTreeVersion.VersionStrategies
{
    public class FixedVersionStrategy : IVersionStrategy
    {
        private readonly int _version;

        public FixedVersionStrategy(int version)
        {
            _version = version;
        }

        public VersionComponent GetVersionComponent(AbsoluteDirectoryPath versionRootPath, AbsoluteDirectoryPath[] relevantPaths, string? range)
        {
            return new(_version, range);
        }
    }
}