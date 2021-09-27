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
        private readonly AbsoluteDirectoryPath _repositoryRootPath;
        private readonly AbsoluteDirectoryPath _versionRootPath;
        private readonly VersionType _versionType;

        public VersionConfigVersionStrategy(AbsoluteDirectoryPath repositoryRootPath, AbsoluteDirectoryPath versionRootPath, VersionConfig versionConfig, VersionType versionType)
        {
            _repositoryRootPath = repositoryRootPath;
            _versionRootPath = versionRootPath;
            _versionConfig = versionConfig;
            _versionType = versionType;
        }

        public VersionComponent GetVersionComponent(VersionComponentContext context, string? range)
        {
            SemVersion version = new(0);

            var versionConfigPath = _versionRootPath.CombineToFile(ContextResolver.VersionConfigFileName);

            if (versionConfigPath.Exists)
            {
                var gitDirectory = new GitDirectory(_repositoryRootPath);

                var historyEntries = gitDirectory.FileCommitHistory(versionConfigPath.FullName);
                var (v, e) = GetCurrentVersionIntroduced(versionConfigPath, historyEntries);
                version = v;

                if (e is not null)
                {
                    range = $"{e.CommitSha}..";
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

        private static (SemVersion, FileCommitContent?) GetCurrentVersionIntroduced(AbsoluteFilePath versionConfigPath, FileCommitContent[] historyEntries)
        {
            var firstEntry = historyEntries.FirstOrDefault();

            if (firstEntry is null)
            {
                return (new SemVersion(0), null);
            }

            var firstConfig = VersionConfig.FromJson(firstEntry.Content);
            FileCommitContent? entry = null;
            var semVersion = new SemVersion(0);

            if (firstConfig?.BaseVersion is not null)
            {
                entry = firstEntry;

                if (!SemVersion.TryParse(firstConfig.BaseVersion, out semVersion))
                {
                    throw new UserException($"Base version is invalid: {firstConfig.BaseVersion} (path: {versionConfigPath.FullName})");
                }
            }

            foreach (var historyEntry in historyEntries.Skip(1))
            {
                var config = VersionConfig.FromJson(historyEntry.Content);

                if (config?.BaseVersion is null)
                {
                    return (semVersion, entry);
                }

                if (!SemVersion.TryParse(config.BaseVersion, out var entryVersion) || entryVersion is null)
                {
                    return (semVersion, entry);
                }

                if (semVersion != entryVersion)
                {
                    return (semVersion, entry);
                }

                semVersion = entryVersion;
                entry = historyEntry;
            }

            return (semVersion, entry);
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