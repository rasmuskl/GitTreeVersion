using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using GitTreeVersion.Paths;

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
            arguments.Add("--first-parent");

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
            arguments.Add("--first-parent");
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

        public static string[] GitCommits(AbsoluteDirectoryPath workingDirectory, string? range, string[] pathSpecs, DateTimeOffset? after = null, DateTimeOffset? before = null, string? diffFilter = null)
        {
            var arguments = new List<string>();
            arguments.Add("log");
            arguments.Add("--full-history");
            arguments.Add("--first-parent");
            arguments.Add("--format=format:%H");

            if (after is not null)
            {
                arguments.Add($"--after={after.Value.ToUnixTimeSeconds()}");
            }

            if (before is not null)
            {
                arguments.Add($"--before={before.Value.ToUnixTimeSeconds()}");
            }

            arguments.Add(range ?? "HEAD");

            if (!string.IsNullOrEmpty(diffFilter))
            {
                arguments.Add($"--diff-filter={diffFilter}");
            }

            if (pathSpecs.Any())
            {
                arguments.Add("--");
                arguments.AddRange(pathSpecs.Select(ps => ps.ToString()));
            }

            try
            {
                var output = RunGit(workingDirectory, arguments.ToArray());
                return output.SplitOutput();
            }
            catch (GitFailedException e) when (e.ErrorOutput is "fatal: bad revision 'HEAD'")
            {
                // Empty repository
                return Array.Empty<string>();
            }
        }

        public static string[] GitLastCommitHashes(AbsoluteDirectoryPath workingDirectory, string pathSpec)
        {
            var output = RunGit(workingDirectory, "rev-list", "HEAD", "--", pathSpec);
            return output.SplitOutput();
        }

        public static GitRef? GitCurrentBranch(AbsoluteDirectoryPath workingDirectory)
        {
            var output = RunGit(workingDirectory, "branch");
            var lines = output.SplitOutput();

            var branch = lines.FirstOrDefault(l => l.StartsWith("*"));

            if (branch is null)
            {
                return null;
            }
            
            var match = Regex.Match(branch, "(HEAD detached at (?<ref>[^)]*))");

            if (match.Success)
            {
                return new GitRef(match.Groups["ref"].Value, true);
            }
            
            return new GitRef(branch.TrimStart(' ', '*'), false);
        }

        public static string[] GitMerges(AbsoluteDirectoryPath workingDirectory, string? range, AbsoluteDirectoryPath[] pathSpecs)
        {
            var arguments = new List<string>();
            arguments.Add("rev-list");
            arguments.Add("--merges");
            arguments.Add("--full-history");
            arguments.Add("--first-parent");

            arguments.Add(range ?? "HEAD");

            if (pathSpecs.Any())
            {
                arguments.Add("--");
                arguments.AddRange(pathSpecs.Select(ps => ps.ToString()));
            }

            var output = RunGit(workingDirectory, arguments.ToArray());
            return output.SplitOutput();
        }

        public static string[] GitFindFiles(AbsoluteDirectoryPath workingDirectory, string[] pathSpecs, bool includeUnstaged = false)
        {
            var arguments = new List<string>();
            arguments.Add("ls-files");

            if (includeUnstaged)
            {
                arguments.Add("--exclude-standard");
                arguments.Add("--others");
                arguments.Add("--cached");
            }

            if (pathSpecs.Any())
            {
                arguments.Add("--");
                arguments.AddRange(pathSpecs);
            }

            var output = RunGit(workingDirectory, arguments.ToArray());
            return output.SplitOutput();
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
                RedirectStandardError = true
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
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (_, args) => { outputBuilder.AppendLine(args.Data); };
            process.ErrorDataReceived += (_, args) => { errorBuilder.AppendLine(args.Data); };

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            process.WaitForExit();

            var error = errorBuilder.ToString();
            if (process.ExitCode != 0)
            {
                throw new GitFailedException(arguments, error, process.ExitCode);
            }

            return outputBuilder.ToString();
        }
    }
}