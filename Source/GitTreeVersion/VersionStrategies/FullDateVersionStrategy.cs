using System;
using System.Linq;
using GitTreeVersion.Paths;

namespace GitTreeVersion.VersionStrategies
{
    public class FullDateVersionStrategy : IVersionStrategy
    {
        public VersionComponent GetVersionComponent(AbsoluteDirectoryPath versionRootPath, AbsoluteDirectoryPath[] relevantPaths, string? range)
        {
            var pathSpecs = relevantPaths.Select(p => p.ToString()).ToArray();
            var newestCommit = Git.GitNewestCommitUnixTimeSeconds(versionRootPath, null, pathSpecs);

            if (newestCommit is null)
            {
                throw new InvalidOperationException($"No newest commit found for paths: {string.Join(", ", relevantPaths.Select(p => p.FullName))}");
            }

            var newestCommitTimestamp = DateTimeOffset.FromUnixTimeSeconds(newestCommit.Value);

            Log.Debug($"{newestCommit} - {newestCommitTimestamp}");

            var newestCommitDate = new DateTimeOffset(newestCommitTimestamp.Date, TimeSpan.Zero);

            Log.Debug($"Since date: {newestCommitDate}");

            var gitCommits = Git.GitCommits(versionRootPath, null, pathSpecs, newestCommitDate, newestCommitTimestamp);
            var firstOnDate = gitCommits.Last();
            Log.Debug($"First on date: {firstOnDate}");
            range = $"{firstOnDate}..";

            return new VersionComponent(int.Parse(newestCommitTimestamp.ToString("yyyyMMdd")), range);
        }
    }
}