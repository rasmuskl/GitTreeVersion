using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using GitTreeVersion.Paths;
using Spectre.Console;

namespace GitTreeVersion
{
    public static class Git
    {
        public static string[] GitNonMerges(AbsoluteDirectoryPath workingDirectory, string? range,
            AbsoluteDirectoryPath[] pathSpecs)
        {
            var arguments = new List<string>();
            arguments.Add("rev-list");
            arguments.Add("--no-merges");
            arguments.Add("--full-history");

            arguments.Add(range ?? "HEAD");

            if (pathSpecs.Any())
            {
                arguments.Add("--");
                arguments.AddRange(pathSpecs.Select(ps => ps.ToString()));
            }

            var output = RunGit(workingDirectory, arguments.ToArray());
            return output.SplitOutput();
        }

        public static long? GitNewestCommitUnixTimeSeconds(AbsoluteDirectoryPath workingDirectory, string? range,
            string[] pathSpecs)
        {
            var arguments = new List<string>();
            arguments.Add("log");
            arguments.Add("--full-history");
            arguments.Add("--author-date-order");
            arguments.Add("--max-count=1");
            arguments.Add("--format=%at");

            arguments.Add(range ?? "HEAD");

            if (pathSpecs.Any())
            {
                arguments.Add("--");
                arguments.AddRange(pathSpecs);
            }

            var output = RunGit(workingDirectory, arguments.ToArray());
            var unixTimeSeconds = output.SplitOutput().SingleOrDefault();

            if (unixTimeSeconds is null)
            {
                return null;
            }

            return long.Parse(unixTimeSeconds);
        }

        public static string[] GitCommits(AbsoluteDirectoryPath workingDirectory, string? range,
            string[] pathSpecs, DateTimeOffset? after = null, DateTimeOffset? before = null)
        {
            var arguments = new List<string>();
            arguments.Add("rev-list");
            arguments.Add("--full-history");

            if (after is not null)
            {
                arguments.Add($"--after={after.Value.ToUnixTimeSeconds()}");
            }

            if (before is not null)
            {
                arguments.Add($"--before={before.Value.ToUnixTimeSeconds()}");
            }

            arguments.Add(range ?? "HEAD");

            if (pathSpecs.Any())
            {
                arguments.Add("--");
                arguments.AddRange(pathSpecs.Select(ps => ps.ToString()));
            }

            var output = RunGit(workingDirectory, arguments.ToArray());
            return output.SplitOutput();
        }

        public static string[] GitLastCommitHashes(AbsoluteDirectoryPath workingDirectory, string pathSpec)
        {
            var output = RunGit(workingDirectory, "rev-list", "HEAD", "--", pathSpec);
            return output.SplitOutput();
        }

        public static string[] GitMerges(AbsoluteDirectoryPath workingDirectory, string? range,
            AbsoluteDirectoryPath[] pathSpecs)
        {
            var arguments = new List<string>();
            arguments.Add("rev-list");
            arguments.Add("--merges");
            arguments.Add("--full-history");

            arguments.Add(range ?? "HEAD");

            if (pathSpecs.Any())
            {
                arguments.Add("--");
                arguments.AddRange(pathSpecs.Select(ps => ps.ToString()));
            }

            var output = RunGit(workingDirectory, arguments.ToArray());
            return output.SplitOutput();
        }

        public static string[] GitFindFiles(AbsoluteDirectoryPath workingDirectory, string pathSpec,
            bool includeUnstaged = false)
        {
            if (includeUnstaged)
            {
                var runGit = RunGit(workingDirectory, "ls-files", "--exclude-standard", "--others", "--cached", "--",
                    pathSpec);
                return runGit.SplitOutput();
            }
            else
            {
                var runGit = RunGit(workingDirectory, "ls-files", "--", pathSpec);
                return runGit.SplitOutput();
            }
        }

        public static string[] GitDiffFileNames(AbsoluteDirectoryPath workingDirectory, string? range, string? pathSpec)
        {
            var arguments = new List<string>();
            arguments.Add("diff");
            arguments.Add("--name-only");

            arguments.Add(range ?? "HEAD");

            if (!string.IsNullOrWhiteSpace(pathSpec))
            {
                arguments.Add("--");
                arguments.Add(pathSpec);
            }

            var output = RunGit(workingDirectory, arguments.ToArray());
            return output.SplitOutput();
        }

        public static string RunGit(AbsoluteDirectoryPath workingDirectory, params string[] arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                WorkingDirectory = workingDirectory.ToString(),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            foreach (var argument in arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            Log.Debug($"[grey35]> git {string.Join(" ", startInfo.ArgumentList)}[/]");

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