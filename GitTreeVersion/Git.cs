using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace GitTreeVersion
{
    public static class Git
    {
        public static bool Debug { get; set; }
        
        public static string[] GitNonMerges(string workingDirectory, string? range, string[]? pathSpecs)
        {
            var arguments = new List<string>();
            arguments.Add("rev-list");
            arguments.Add("--no-merges");
            arguments.Add("--full-history");

            arguments.Add(range ?? "HEAD");

            if (pathSpecs is not null)
            {
                arguments.Add("--");
                arguments.AddRange(pathSpecs);
            }

            var output = RunGit(workingDirectory, arguments.ToArray());
            return output.SplitOutput();
        }

        public static string[] GitLastCommitHashes(string workingDirectory, string pathSpec)
        {
            var output = RunGit(workingDirectory, "rev-list", "HEAD", "--", pathSpec);
            return output.SplitOutput();
        }

        public static string[] GitMerges(string workingDirectory, string? range, string[]? pathSpecs)
        {
            var arguments = new List<string>();
            arguments.Add("rev-list");
            arguments.Add("--merges");
            arguments.Add("--full-history");

            arguments.Add(range ?? "HEAD");

            if (pathSpecs is not null)
            {
                arguments.Add("--");
                arguments.AddRange(pathSpecs);
            }

            var output = RunGit(workingDirectory, arguments.ToArray());
            return output.SplitOutput();
        }

        public static string[] GitFindFiles(string workingDirectory, string pathSpec)
        {
            var runGit = RunGit(workingDirectory, "ls-files", "--", pathSpec);
            return runGit.SplitOutput();
        }

        public static string RunGit(string workingDirectory, params string[] arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            foreach (var argument in arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            if (Debug)
            {
                Console.WriteLine($"git {string.Join(" ", startInfo.ArgumentList)}");
            }

            var process = Process.Start(startInfo);

            if (process is null)
            {
                throw new InvalidOperationException("No process");
            }
            
            var outputBuilder = new StringBuilder();

            process.OutputDataReceived += (sender, args) => { outputBuilder.AppendLine(args.Data); };

            process.BeginOutputReadLine();

            var error = process.StandardError.ReadToEnd();

            process.WaitForExit();

            if (!string.IsNullOrWhiteSpace(error))
            {
                Console.WriteLine($"ERROR: {error}");
            }

            return outputBuilder.ToString();
        }
    }
}