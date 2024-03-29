﻿using GitTreeVersion.Paths;

namespace GitTreeVersion.VersionStrategies
{
    public class FixedVersionStrategy : IVersionStrategy
    {
        private readonly int _version;

        public FixedVersionStrategy(int version)
        {
            _version = version;
        }

        public VersionComponent GetVersionComponent(VersionComponentContext context, string? range)
        {
            return new VersionComponent(_version, range);
        }

        public AbsoluteFilePath Bump(AbsoluteDirectoryPath versionRootPath)
        {
            throw new UserException("Version bumping is not supported with fixed version strategy.");
        }
    }
}