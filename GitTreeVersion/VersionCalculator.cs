using System;
using System.Linq;
using GitTreeVersion.Context;
using GitTreeVersion.Paths;

namespace GitTreeVersion
{
    public class VersionCalculator
    {
        public Version GetVersion(FileGraph graph)
        {
            return GetVersion(graph, graph.VersionRootPath);
        }
        
        public Version GetVersion(FileGraph graph, AbsoluteDirectoryPath versionRootPath)
        {
            var relevantPaths = graph.GetRelevantPathsForVersionRoot(versionRootPath);
            
            string? range = null;
            var merges = Git.GitMerges(versionRootPath, range, relevantPaths);

            // Console.WriteLine($"Merges: {merges.Length}");
            // Console.WriteLine($"Last merge: {merges.FirstOrDefault()}");

            string? sinceMergeRange = null;

            if (merges.Any())
            {
                sinceMergeRange = $"{merges.First()}..";
            }

            var nonMerges = Git.GitNonMerges(versionRootPath, sinceMergeRange, relevantPaths);

            // Console.WriteLine($"Non-merges: {nonMerges.Length}");
            // Console.WriteLine($"Version: 0.{merges.Length}.{nonMerges.Length}");

            return new Version(0, merges.Length, nonMerges.Length);
        }
        
        public Version GetCalendarVersion(FileGraph graph)
        {
            var relevantPaths = graph.GetRelevantPathsForVersionRoot(graph.VersionRootPath);

            var newestCommit = Git.GitNewestCommitUnixTimeSeconds(graph.VersionRootPath, null, relevantPaths);

            if (newestCommit is null)
            {
                throw new InvalidOperationException("No newest commit found for paths: " + string.Join(", ", relevantPaths));
            }
            
            var newestCommitTimestamp = DateTimeOffset.FromUnixTimeSeconds(long.Parse(newestCommit));

            // Console.WriteLine($"{newestCommit} - {newestCommitTimestamp}");

            var newestCommitDate = new DateTimeOffset(newestCommitTimestamp.Date, TimeSpan.Zero);
            
            // Console.WriteLine($"Since date: {newestCommitDate}");

            var gitCommits = Git.GitCommits(graph.VersionRootPath, null, relevantPaths, newestCommitDate, newestCommitTimestamp);
            var firstOnDate = gitCommits.Last();
            
            // Console.WriteLine($"First on date: {firstOnDate}");

            var range = $"{firstOnDate}..";
            var commits = Git.GitCommits(graph.VersionRootPath, range, relevantPaths);

            // Console.WriteLine($"Commits since: {commits.Length}");

            return new Version(newestCommitTimestamp.Year, int.Parse(newestCommitTimestamp.ToString("Mdd")), commits.Length);
        }
    }
}