using System;
using System.Linq;
using GitTreeVersion.Git;
using GitTreeVersion.Paths;

namespace GitTreeVersion.VersionStrategies
{
    public class FullDateVersionStrategy : IVersionStrategy
    {
        public VersionComponent GetVersionComponent(AbsoluteDirectoryPath versionRootPath, AbsoluteDirectoryPath[] relevantPaths, string? range)
        {
            var gitDirectory = new GitDirectory(versionRootPath);
            var pathSpecs = relevantPaths.Select(p => p.ToString()).ToArray();
            var newestCommit = gitDirectory.GitNewestCommitUnixTimeSeconds(null, pathSpecs);

            if (newestCommit is null)
            {
                throw new InvalidOperationException($"No newest commit found for paths: {string.Join(", ", relevantPaths.Select(p => p.FullName))}");
            }

            var newestCommitTimestamp = DateTimeOffset.FromUnixTimeSeconds(newestCommit.Value);

            Log.Debug($"{newestCommit} - {newestCommitTimestamp}");

            var newestCommitDate = new DateTimeOffset(newestCommitTimestamp.Date, TimeSpan.Zero);

            Log.Debug($"Since date: {newestCommitDate}");

            var gitCommits = gitDirectory.GitCommits(null, pathSpecs, newestCommitDate, newestCommitTimestamp);
            var firstOnDate = gitCommits.Last();
            Log.Debug($"First on date: {firstOnDate}");
            range = $"{firstOnDate}..";

            return new VersionComponent(int.Parse(newestCommitTimestamp.ToString("yyyyMMdd")), range);
        }
    }
}