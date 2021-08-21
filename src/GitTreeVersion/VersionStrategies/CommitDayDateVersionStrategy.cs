using System;
using System.Linq;
using GitTreeVersion.Git;

namespace GitTreeVersion.VersionStrategies
{
    public class CommitDayDateVersionStrategy : IVersionStrategy
    {
        private readonly string _format;

        public CommitDayDateVersionStrategy(string format)
        {
            _format = format;
        }

        public VersionComponent GetVersionComponent(VersionComponentContext context, string? range)
        {
            var gitDirectory = new GitDirectory(context.VersionRootPath);
            var pathSpecs = context.RelevantPaths.Select(p => p.ToString()).ToArray();
            var newestCommit = gitDirectory.GitNewestCommitUnixTimeSeconds(null, pathSpecs);

            if (newestCommit is null)
            {
                throw new InvalidOperationException($"No newest commit found for paths: {string.Join(", ", context.RelevantPaths.Select(p => p.FullName))}");
            }

            var newestCommitTimestamp = DateTimeOffset.FromUnixTimeSeconds(newestCommit.Value);

            Log.Debug($"{newestCommit} - {newestCommitTimestamp}");

            var newestCommitDate = new DateTimeOffset(newestCommitTimestamp.Date, TimeSpan.Zero);

            Log.Debug($"Since date: {newestCommitDate}");

            var gitCommits = gitDirectory.GitCommits(null, pathSpecs, newestCommitDate, newestCommitTimestamp);
            var firstOnDate = gitCommits.Last();
            Log.Debug($"First on date: {firstOnDate}");
            range = $"{firstOnDate}..";

            return new VersionComponent(int.Parse(newestCommitTimestamp.ToString(_format)), range);
        }
    }
}