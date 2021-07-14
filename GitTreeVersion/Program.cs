using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

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
            string? range = null;
            var merges = GitMerges(workingDirectory, range, ".");

            Console.WriteLine($"Merges: {merges.Length}");

            Console.WriteLine($"Last merge: {merges.FirstOrDefault()}");

            string? sinceMergeRange = null;

            if (merges.Any())
            {
                sinceMergeRange = $"{merges.First()}..";
            }

            var nonMerges = GitNonMerges(workingDirectory, sinceMergeRange, ".");

            Console.WriteLine($"Non-merges: {nonMerges.Length}");

            Console.WriteLine($"Version: 0.{merges.Length}.{nonMerges.Length}");

            Console.WriteLine($"Elapsed {stopwatch.ElapsedMilliseconds} ms");
        }

        private static string[] GitNonMerges(string workingDirectory, string? range, string? pathSpec)
        {
            var arguments = new List<string>();
            arguments.Add("rev-list");
            arguments.Add("--no-merges");
            arguments.Add("--full-history");

            arguments.Add(range ?? "HEAD");

            if (pathSpec is not null)
            {
                arguments.Add("--");
                arguments.Add(pathSpec);
            }

            var output = RunGit(workingDirectory, arguments);
            return output.Trim().Split('\n').Select(l => l.Trim()).ToArray();
        }

        private static string[] GitLastCommitHashes(string workingDirectory, string pathSpec)
        {
            var output = RunGit(workingDirectory, new[] {"rev-list", "HEAD", "--", pathSpec});
            return output.Trim().Split('\n').Select(l => l.Trim()).ToArray();
        }

        private static string[] GitMerges(string workingDirectory, string? range, string? pathSpec)
        {
            var arguments = new List<string>();
            arguments.Add("rev-list");
            arguments.Add("--merges");
            arguments.Add("--full-history");

            arguments.Add(range ?? "HEAD");

            if (pathSpec is not null)
            {
                arguments.Add("--");
                arguments.Add(pathSpec);
            }

            var output = RunGit(workingDirectory, arguments);
            return output.Trim().Split('\n').Select(l => l.Trim()).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
        }

        private static void GitFindFiles(string workingDirectory, string pathSpec)
        {
            var runGit = RunGit(workingDirectory, new[] {"ls-files", "--", pathSpec});
            Console.WriteLine(runGit);
        }

        private static string RunGit(string workingDirectory, IEnumerable<string> arguments = null)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            foreach (var argument in arguments ?? Array.Empty<string>())
            {
                startInfo.ArgumentList.Add(argument);
            }

            Console.WriteLine($"git {string.Join(" ", startInfo.ArgumentList)}");

            var process = Process.Start(startInfo);
            var outputBuilder = new StringBuilder();

            process.OutputDataReceived += (sender, args) => { outputBuilder.AppendLine(args.Data); };

            process.BeginOutputReadLine();

            var error = process.StandardError.ReadToEnd();

            process.WaitForExit();

            if (!string.IsNullOrWhiteSpace(error))
            {
                Console.WriteLine($"ERROR: {error}");
            }

            var output = outputBuilder.ToString();

            return output;
        }
    }
}