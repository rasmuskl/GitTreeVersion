using System.Collections.Concurrent;
using System.Linq;
using System.Text.RegularExpressions;
using GitTreeVersion.Context;
using GitTreeVersion.Git;
using GitTreeVersion.Paths;
using GitTreeVersion.VersionStrategies;
using Semver;

namespace GitTreeVersion
{
    public class VersionCalculator
    {
        private readonly ConcurrentDictionary<AbsoluteDirectoryPath, SemVersion> _cache = new();

        public SemVersion GetVersion(VersionGraph graph, AbsoluteDirectoryPath versionRootPath)
        {
            if (_cache.TryGetValue(versionRootPath, out var cachedVersion))
            {
                return cachedVersion;
            }

            IVersionConfiguration versionConfiguration = new SemanticVersioningConfigFileVersionConfiguration(versionRootPath, graph.VersionRootConfigs[versionRootPath]);

            if (graph.VersionRootConfigs[versionRootPath].Preset == VersionPreset.CalendarVersion)
            {
                versionConfiguration = new CalendarVersioningVersionConfiguration();
            }

            if (graph.VersionRootConfigs[versionRootPath].Preset == VersionPreset.SemanticVersionFileBased)
            {
                versionConfiguration = new SemanticVersioningFileBasedVersionConfiguration();
            }

            var relevantPaths = graph.GetRelevantPathsForVersionRoot(versionRootPath);
            var prerelease = GetPrerelease(graph, versionRootPath, relevantPaths);

            var versionComponentContext = new VersionComponentContext(versionRootPath, relevantPaths, prerelease, graph.CurrentBranch, graph.MainBranch);
            var majorComponent = versionConfiguration.Major.GetVersionComponent(versionComponentContext, null);
            var minorComponent = versionConfiguration.Minor.GetVersionComponent(versionComponentContext, majorComponent.Range);
            var patchComponent = versionConfiguration.Patch.GetVersionComponent(versionComponentContext, minorComponent.Range);

            var version = new SemVersion(majorComponent.Version, minorComponent.Version, patchComponent.Version, prerelease);
            _cache[versionRootPath] = version;
            return version;
        }

        private static string? GetPrerelease(VersionGraph graph, AbsoluteDirectoryPath versionRootPath, AbsoluteDirectoryPath[] relevantPaths)
        {
            var prerelease = graph.BuildEnvironment?.GetPrerelease(versionRootPath, relevantPaths);

            if (!string.IsNullOrWhiteSpace(prerelease))
            {
                return prerelease;
            }

            if (graph.CurrentBranch is { IsDetached: false } branch && graph.MainBranch is not null)
            {
                if (graph.CurrentBranch == graph.MainBranch)
                {
                    return null;
                }

                var branchName = SanitizeBranchName(branch.Name);

                if (string.IsNullOrEmpty(branchName))
                {
                    return null;
                }

                var gitDirectory = new GitDirectory(versionRootPath);
                var branchCommits = gitDirectory.GitCommits($"{graph.MainBranch.Name}..{graph.CurrentBranch.Name}", relevantPaths.Select(p => p.FullName).ToArray());
                return $"{branchName}.{branchCommits.Length}";
            }

            return null;
        }

        private static string SanitizeBranchName(string branchName)
        {
            branchName = Regex.Replace(branchName, @"[^a-zA-Z0-9.-]", "-");
            while (branchName.Contains("--"))
            {
                branchName = branchName.Replace("--", "-");
            }

            return branchName.Trim();
        }
    }

    public record VersionComponentContext(AbsoluteDirectoryPath VersionRootPath, AbsoluteDirectoryPath[] RelevantPaths, string? Prerelease, GitRef? CurrentBranch, GitRef? MainBranch);
}