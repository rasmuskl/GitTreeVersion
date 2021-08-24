using GitTreeVersion.Paths;

namespace GitTreeVersion.Context
{
    public class Bumper
    {
        private readonly VersionCalculator _versionCalculator = new();

        public AbsoluteFilePath Bump(AbsoluteDirectoryPath versionRootPath, VersionType versionType)
        {
            var versionConfiguration = _versionCalculator.GetVersionConfiguration(versionRootPath);

            switch (versionType)
            {
                case VersionType.Major:
                    return versionConfiguration.Major.Bump(versionRootPath);
                case VersionType.Minor:
                    return versionConfiguration.Minor.Bump(versionRootPath);
                case VersionType.Patch:
                    return versionConfiguration.Patch.Bump(versionRootPath);
            }

            throw new UserException($"Unknown version type: {versionType}");
        }
    }

    public enum VersionType
    {
        Major,
        Minor,
        Patch,
    }
}