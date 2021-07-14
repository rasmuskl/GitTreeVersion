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
            var workingDirectory = @"c:\dev\PoeNinja";

            var stopwatch = Stopwatch.StartNew();

            // GitFindFiles(workingDirectory, ":(top,glob)**/*.csproj");
            
            var file = @"Source/PoeNinja/PoeNinja.csproj";

            var lastCommitHashes = GitLastCommitHashes(workingDirectory, file);
            
            Console.WriteLine($"Last commit hash: {string.Join(Environment.NewLine, lastCommitHashes)}");

            var merges = GitMerges(workingDirectory, $"{lastCommitHashes.Last()}..", file);
            
            Console.WriteLine($"Merges: {merges.Length}");
            
            Console.WriteLine($"Last merge: {merges.First()}");

            var nonMerges = GitNonMerges(workingDirectory, $"{merges.First()}..", ".");
            
            Console.WriteLine($"Non-merges: {nonMerges.Length}");
            
            Console.WriteLine($"Version: 0.{merges.Length}.{nonMerges.Length}");

            Console.WriteLine($"Elapsed {stopwatch.ElapsedMilliseconds} ms");
        }

        private static string[] GitNonMerges(string workingDirectory, string range, string pathSpec)
        {
            var output = RunGit(workingDirectory, new []{ "rev-list", "--no-merges", "--full-history", range, "--", pathSpec});
            return output.Trim().Split('\n').Select(l => l.Trim()).ToArray();
        }

        private static string[] GitLastCommitHashes(string workingDirectory, string pathSpec)
        {
            var output = RunGit(workingDirectory, new []{ "rev-list", "HEAD", "--", pathSpec});
            return output.Trim().Split('\n').Select(l => l.Trim()).ToArray();
        }

        private static string[] GitMerges(string workingDirectory, string range, string pathSpec)
        {
            var output = RunGit(workingDirectory, new []{ "rev-list", "--merges", "--full-history", "--first-parent", range, "--", pathSpec});
            return output.Trim().Split('\n').Select(l => l.Trim()).ToArray();
        }

        private static void GitFindFiles(string workingDirectory, string pathSpec)
        {
            var runGit = RunGit(workingDirectory, new [] { "ls-files", "--", pathSpec });
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

            process.OutputDataReceived += (sender, args) =>
            {
                outputBuilder.AppendLine(args.Data);
            };
            
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
