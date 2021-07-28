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

            var majorVersionFiles = Git.GitFindFiles(versionRootPath, ":(glob).version/major/*", false);
            var majorVersionCommits = Git.GitCommits(versionRootPath, null, new [] { ":(glob).version/major/*" });

            foreach (var majorVersionCommit in majorVersionCommits)
            {
                Console.WriteLine($"Major commit: {majorVersionCommit}");
            }

            string? range = null;
            var major = majorVersionFiles.Length;
            var minor = 0;

            if (majorVersionCommits.Any())
            {
                range = $"{majorVersionCommits.First()}..";
                var changedMinorFiles = Git.GitDiffFileNames(versionRootPath, range, ":(glob).version/minor/*");

                foreach (var file in changedMinorFiles)
                {
                    Console.WriteLine($"Minor file: {file}");
                }

                minor = changedMinorFiles.Length;

                var minorVersionCommits = Git.GitCommits(versionRootPath, range, changedMinorFiles.ToArray());

                foreach (var commit in minorVersionCommits)
                {
                    Console.WriteLine($"Minor commit: {commit}");
                }

                if (minorVersionCommits.Any())
                {
                    range = $"{minorVersionCommits.First()}..";
                }
            }
            else
            {
                var minorVersionFiles = Git.GitFindFiles(versionRootPath, ":(glob).version/minor/*", false);

                foreach (var file in minorVersionFiles)
                {
                    Console.WriteLine($"Minor file: {file}");
                }
                
                minor = minorVersionFiles.Length;
                var minorVersionCommits = Git.GitCommits(versionRootPath, null, new [] { ":(glob).version/minor/*" });

                foreach (var commit in minorVersionCommits)
                {
                    Console.WriteLine($"Minor commit: {commit}");
                }
                
                if (minorVersionCommits.Any())
                {
                    range = $"{minorVersionCommits.First()}..";
                }
            }
            
            // var merges = Git.GitMerges(versionRootPath, range, relevantPaths);

            // Console.WriteLine($"Merges: {merges.Length}");
            // Console.WriteLine($"Last merge: {merges.FirstOrDefault()}");

            // if (merges.Any())
            // {
            //     range = $"{merges.First()}..";
            // }

            var commits = Git.GitCommits(versionRootPath, range, relevantPaths.Select(p => p.ToString()).ToArray());

            // Console.WriteLine($"Non-merges: {nonMerges.Length}");
            // Console.WriteLine($"Version: 0.{merges.Length}.{nonMerges.Length}");

            var patch = commits.Length;
            
            return new Version(major, minor, patch);
        }
        
        public Version GetCalendarVersion(FileGraph graph)
        {
            var relevantPaths = graph.GetRelevantPathsForVersionRoot(graph.VersionRootPath);
            var pathSpecs = relevantPaths.Select(p => p.ToString()).ToArray();
            
            var newestCommit = Git.GitNewestCommitUnixTimeSeconds(graph.VersionRootPath, null, pathSpecs);

            if (newestCommit is null)
            {
                throw new InvalidOperationException("No newest commit found for paths: " + string.Join(", ", relevantPaths));
            }
            
            var newestCommitTimestamp = DateTimeOffset.FromUnixTimeSeconds(newestCommit.Value);

            // Console.WriteLine($"{newestCommit} - {newestCommitTimestamp}");

            var newestCommitDate = new DateTimeOffset(newestCommitTimestamp.Date, TimeSpan.Zero);
            
            // Console.WriteLine($"Since date: {newestCommitDate}");

            var gitCommits = Git.GitCommits(graph.VersionRootPath, null, pathSpecs, newestCommitDate, newestCommitTimestamp);
            var firstOnDate = gitCommits.Last();
            
            // Console.WriteLine($"First on date: {firstOnDate}");

            var range = $"{firstOnDate}..";
            var commits = Git.GitCommits(graph.VersionRootPath, range, pathSpecs);

            // Console.WriteLine($"Commits since: {commits.Length}");

            return new Version(newestCommitTimestamp.Year, int.Parse(newestCommitTimestamp.ToString("Mdd")), commits.Length);
        }
    }
}