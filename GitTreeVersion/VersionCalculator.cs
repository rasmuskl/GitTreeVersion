using System;
using System.Linq;
using GitTreeVersion.Context;

namespace GitTreeVersion
{
    public class VersionCalculator
    {
        public Version GetVersion(RepositoryContext context)
        {
            string? range = null;
            var merges = Git.GitMerges(context.VersionRootPath, range, ".");

            // Console.WriteLine($"Merges: {merges.Length}");
            //
            // Console.WriteLine($"Last merge: {merges.FirstOrDefault()}");

            string? sinceMergeRange = null;

            if (merges.Any())
            {
                sinceMergeRange = $"{merges.First()}..";
            }

            var nonMerges = Git.GitNonMerges(context.VersionRootPath, sinceMergeRange, ".");

            // Console.WriteLine($"Non-merges: {nonMerges.Length}");
            //
            // Console.WriteLine($"Version: 0.{merges.Length}.{nonMerges.Length}");

            return new Version(int.Parse(context.VersionConfig.Major ?? "0"), merges.Length, nonMerges.Length);
        }
    }
}