using System;
using System.Linq;

namespace GitTreeVersion
{
    public class VersionCalculator
    {
        public Version GetVersion(string workingDirectory)
        {
            string? range = null;
            var merges = Git.GitMerges(workingDirectory, range, ".");

            var versionConfigInstance = new VersionConfigManager().FindConfig(workingDirectory);
            
            // Console.WriteLine($"Merges: {merges.Length}");
            //
            // Console.WriteLine($"Last merge: {merges.FirstOrDefault()}");

            string? sinceMergeRange = null;

            if (merges.Any())
            {
                sinceMergeRange = $"{merges.First()}..";
            }

            var nonMerges = Git.GitNonMerges(workingDirectory, sinceMergeRange, ".");

            // Console.WriteLine($"Non-merges: {nonMerges.Length}");
            //
            // Console.WriteLine($"Version: 0.{merges.Length}.{nonMerges.Length}");

            return new Version(int.Parse(versionConfigInstance.VersionConfig.Major ?? "0"), merges.Length, nonMerges.Length);
        }
    }
}