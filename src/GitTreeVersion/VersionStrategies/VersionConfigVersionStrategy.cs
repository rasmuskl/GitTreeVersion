using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using GitTreeVersion.Context;
using GitTreeVersion.Git;
using GitTreeVersion.Paths;
using Semver;

namespace GitTreeVersion.VersionStrategies
{
    public class VersionConfigVersionStrategy : IVersionStrategy
    {
        private readonly VersionConfig _versionConfig;
        private readonly AbsoluteDirectoryPath _versionRootPath;
        private readonly VersionType _versionType;

        public VersionConfigVersionStrategy(AbsoluteDirectoryPath versionRootPath, VersionConfig versionConfig, VersionType versionType)
        {
            _versionRootPath = versionRootPath;
            _versionConfig = versionConfig;
            _versionType = versionType;
        }

        public VersionComponent GetVersionComponent(VersionComponentContext context, string? range)
        {
            SemVersion version = !string.IsNullOrWhiteSpace(_versionConfig.BaseVersion)
                ? SemVersion.Parse(_versionConfig.BaseVersion)
                : new SemVersion(0);

            var versionConfigPath = _versionRootPath.CombineToFile(ContextResolver.VersionConfigFileName);

            if (versionConfigPath.Exists)
            {
                var gitDirectory = new GitDirectory(_versionRootPath);
                var configFileCommits = gitDirectory.GitCommits(range, new[] { ":(glob)version.json" });

                if (configFileCommits.Any())
                {
                    range = $"{configFileCommits.First()}..";
                }
                else
                {
                    range = "HEAD..";
                }
            }

            if (_versionType == VersionType.Major)
            {
                return new VersionComponent(version.Major, range);
            }

            if (_versionType == VersionType.Minor)
            {
                return new VersionComponent(version.Minor, range);
            }

            if (_versionType == VersionType.Patch)
            {
                return new VersionComponent(version.Patch, range);
            }

            throw new Exception($"Unknown version type: {_versionType}");
        }

        public AbsoluteFilePath Bump(AbsoluteDirectoryPath versionRootPath)
        {
            var versionConfigPath = versionRootPath.CombineToFile(ContextResolver.VersionConfigFileName);

            if (!versionConfigPath.Exists)
            {
                throw new UserException("Version config file does not exist.");
            }

            SemVersion version = !string.IsNullOrWhiteSpace(_versionConfig.BaseVersion)
                ? SemVersion.Parse(_versionConfig.BaseVersion)
                : new SemVersion(0);

            switch (_versionType)
            {
                case VersionType.Major:
                    version = version.Change(version.Major + 1);
                    break;
                case VersionType.Minor:
                    version = version.Change(minor: version.Minor + 1);
                    break;
                case VersionType.Patch:
                    version = version.Change(patch: version.Patch + 1);
                    break;
            }

            _versionConfig.BaseVersion = version.ToString();
            File.WriteAllText(versionConfigPath.FullName, JsonSerializer.Serialize(_versionConfig, JsonOptions.DefaultOptions));
            return versionConfigPath;
        }
    }
}