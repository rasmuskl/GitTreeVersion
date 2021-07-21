using System;
using System.IO;
using System.Linq;
using GitTreeVersion.Context;

namespace GitTreeVersion
{
    public class VersionCalculator
    {
        public Version GetVersion(FileGraph graph)
        {
            return GetVersion(graph, graph.VersionRootPath);
        }
        
        public Version GetVersion(FileGraph graph, string versionRootPath)
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

            var nonMerges = Git.GitNonMerges( versionRootPath, sinceMergeRange, relevantPaths);

            // Console.WriteLine($"Non-merges: {nonMerges.Length}");
            // Console.WriteLine($"Version: 0.{merges.Length}.{nonMerges.Length}");

            return new Version(0, merges.Length, nonMerges.Length);
        }
    }
}