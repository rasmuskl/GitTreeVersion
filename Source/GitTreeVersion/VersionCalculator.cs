using System;
using System.Linq;
using System.Text.RegularExpressions;
using GitTreeVersion.Context;
using GitTreeVersion.Paths;
using Semver;

namespace GitTreeVersion
{
    public class VersionCalculator
    {
        
        
        public SemVersion GetVersion(FileGraph graph)
        {
            return GetVersion(graph, graph.VersionRootPath);
        }

        public SemVersion GetVersion(FileGraph graph, AbsoluteDirectoryPath versionRootPath)
        {
            var versionConfiguration = new VersionConfiguration();
            
            if (graph.VersionRootConfigs[versionRootPath].Mode == VersionMode.CalendarVersion)
            {
                return GetCalendarVersion(graph, versionRootPath);
            }

            var relevantPaths = graph.GetRelevantPathsForVersionRoot(versionRootPath);
            var prerelease = GetPrerelease(graph, versionRootPath, relevantPaths);

            var majorComponent = versionConfiguration.Major.GetVersionComponent(versionRootPath, relevantPaths, null);
            var minorComponent = versionConfiguration.Minor.GetVersionComponent(versionRootPath, relevantPaths, majorComponent.Range);
            var patchComponent = versionConfiguration.Patch.GetVersionComponent(versionRootPath, relevantPaths, minorComponent.Range);

            return new SemVersion(majorComponent.Version, minorComponent.Version, patchComponent.Version, prerelease);
        }

        private SemVersion GetCalendarVersion(FileGraph graph, AbsoluteDirectoryPath versionRootPath)
        {
            var relevantPaths = graph.GetRelevantPathsForVersionRoot(versionRootPath);
            var prerelease = GetPrerelease(graph, versionRootPath, relevantPaths);
            var pathSpecs = relevantPaths.Select(p => p.ToString()).ToArray();

            var newestCommit = Git.GitNewestCommitUnixTimeSeconds(graph.VersionRootPath, null, pathSpecs);

            if (newestCommit is null)
            {
                throw new InvalidOperationException($"No newest commit found for paths: {string.Join(", ", relevantPaths)}");
            }

            var newestCommitTimestamp = DateTimeOffset.FromUnixTimeSeconds(newestCommit.Value);

            Log.Debug($"{newestCommit} - {newestCommitTimestamp}");

            var newestCommitDate = new DateTimeOffset(newestCommitTimestamp.Date, TimeSpan.Zero);

            Log.Debug($"Since date: {newestCommitDate}");

            var gitCommits = Git.GitCommits(graph.VersionRootPath, null, pathSpecs, newestCommitDate, newestCommitTimestamp);
            var firstOnDate = gitCommits.Last();

            Log.Debug($"First on date: {firstOnDate}");

            var range = $"{firstOnDate}..";
            var commits = Git.GitCommits(graph.VersionRootPath, range, pathSpecs);

            Log.Debug($"Commits since: {commits.Length}");

            return new SemVersion(newestCommitTimestamp.Year, int.Parse(newestCommitTimestamp.ToString("Mdd")), commits.Length, prerelease);
        }

        private static string? GetPrerelease(FileGraph graph, AbsoluteDirectoryPath versionRootPath, AbsoluteDirectoryPath[] relevantPaths)
        {
            var prerelease = graph.BuildEnvironment?.GetPrerelease(versionRootPath, relevantPaths);

            if (!string.IsNullOrWhiteSpace(prerelease))
            {
                return prerelease;
            }

            if (graph.CurrentBranch is { IsDetached: false } branch)
            {
                return branch.Name switch
                {
                    "main" => null,
                    "master" => null,
                    var name => SanitizeBranchName(name),
                };
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
}