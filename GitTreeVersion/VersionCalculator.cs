using System;
using System.Linq;
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
            if (graph.VersionRootConfigs[versionRootPath].Mode == VersionMode.CalendarVersion)
            {
                return GetCalendarVersion(graph, versionRootPath);
            }

            var relevantPaths = graph.GetRelevantPathsForVersionRoot(versionRootPath);
            var prerelease = graph.BuildEnvironment?.GetPrerelease(versionRootPath, relevantPaths);
            
            var majorVersionFiles = Git.GitFindFiles(versionRootPath, new[] {":(glob).version/major/*"});

            foreach (var file in majorVersionFiles)
            {
                Log.Debug($"Major version file: {file}");
            }

            string? range = null;
            var major = majorVersionFiles.Length;
            var minor = 0;

            string[] majorVersionCommits;

            if (majorVersionFiles.Any())
            {
                majorVersionCommits = Git.GitCommits(versionRootPath, null, new[] {":(glob).version/major/*"});

                foreach (var majorVersionCommit in majorVersionCommits)
                {
                    Log.Debug($"Major version commit: {majorVersionCommit}");
                }
            }
            else
            {
                majorVersionCommits = Array.Empty<string>();
            }

            if (majorVersionCommits.Any())
            {
                range = $"{majorVersionCommits.First()}..";
                var changedMinorFiles = Git.GitDiffFileNames(versionRootPath, range, ":(glob).version/minor/*");

                foreach (var file in changedMinorFiles)
                {
                    Log.Debug($"Minor version file: {file}");
                }

                minor = changedMinorFiles.Length;

                var minorVersionCommits = Git.GitCommits(versionRootPath, range, changedMinorFiles.ToArray());

                foreach (var commit in minorVersionCommits)
                {
                    Log.Debug($"Minor version commit: {commit}");
                }

                if (minorVersionCommits.Any())
                {
                    range = $"{minorVersionCommits.First()}..";
                }
            }
            else
            {
                var minorVersionFiles = Git.GitFindFiles(versionRootPath, new[] {":(glob).version/minor/*"});

                foreach (var file in minorVersionFiles)
                {
                    Log.Debug($"Minor version file: {file}");
                }

                if (minorVersionFiles.Any())
                {
                    minor = minorVersionFiles.Length;
                    var minorVersionCommits = Git.GitCommits(versionRootPath, null, new[] {":(glob).version/minor/*"});

                    foreach (var commit in minorVersionCommits)
                    {
                        Log.Debug($"Minor version commit: {commit}");
                    }

                    if (minorVersionCommits.Any())
                    {
                        range = $"{minorVersionCommits.First()}..";
                    }
                }
            }

            // var merges = Git.GitMerges(versionRootPath, range, relevantPaths);

            // Log.Debug($"Merges: {merges.Length}");
            // Log.Debug($"Last merge: {merges.FirstOrDefault()}");

            // if (merges.Any())
            // {
            //     range = $"{merges.First()}..";
            // }

            var commits = Git.GitCommits(versionRootPath, range, relevantPaths.Select(p => p.ToString()).ToArray());

            // Log.Debug($"Non-merges: {nonMerges.Length}");
            // Log.Debug($"Version: 0.{merges.Length}.{nonMerges.Length}");

            var patch = commits.Length;

            return new SemVersion(major, minor, patch, prerelease);
        }

        private SemVersion GetCalendarVersion(FileGraph graph, AbsoluteDirectoryPath versionRootPath)
        {
            var relevantPaths = graph.GetRelevantPathsForVersionRoot(versionRootPath);
            var pathSpecs = relevantPaths.Select(p => p.ToString()).ToArray();

            var newestCommit = Git.GitNewestCommitUnixTimeSeconds(graph.VersionRootPath, null, pathSpecs);

            if (newestCommit is null)
            {
                throw new InvalidOperationException("No newest commit found for paths: " + string.Join(", ", relevantPaths));
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

            return new SemVersion(newestCommitTimestamp.Year, int.Parse(newestCommitTimestamp.ToString("Mdd")), commits.Length);
        }
    }
}