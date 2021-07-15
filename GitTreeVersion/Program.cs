using System;
using System.Diagnostics;
using System.Linq;

namespace GitTreeVersion
{
    class Program
    {
        static void Main(string[] args)
        {
            // var workingDirectory = @"c:\dev\PoeNinja";
            var workingDirectory = Environment.CurrentDirectory;

            var stopwatch = Stopwatch.StartNew();

            // GitFindFiles(workingDirectory, ":(top,glob)**/*.csproj");

            // var file = @"Source/PoeNinja/PoeNinja.csproj";

            // var lastCommitHashes = GitLastCommitHashes(workingDirectory, file);

            // Console.WriteLine($"Last commit hash: {string.Join(Environment.NewLine, lastCommitHashes)}");

            // var range = $"{lastCommitHashes.Last()}..";

            var version = new Versioner().GetVersion(workingDirectory);
            Console.WriteLine($"Version: {version}");

            Console.WriteLine($"Elapsed {stopwatch.ElapsedMilliseconds} ms");
        }
    }

    public class Versioner
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