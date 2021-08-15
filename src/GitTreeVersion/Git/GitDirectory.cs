using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using GitTreeVersion.Paths;

namespace GitTreeVersion.Git
{
    public class GitDirectory
    {
        private static readonly HashSet<string> MainBranchNames = new(new[] { "master", "main" });
        private readonly AbsoluteDirectoryPath _workingDirectory;

        public GitDirectory(AbsoluteDirectoryPath workingDirectory)
        {
            _workingDirectory = workingDirectory;
        }

        public string[] GitNonMerges(string? range, AbsoluteDirectoryPath[] pathSpecs)
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

            var output = RunGit(arguments.ToArray());
            return output.SplitOutput();
        }

        public long? GitNewestCommitUnixTimeSeconds(string? range, string[] pathSpecs)
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

            var output = RunGit(arguments.ToArray());
            var unixTimeSeconds = output.SplitOutput().SingleOrDefault();

            if (unixTimeSeconds is null)
            {
                return null;
            }

            return long.Parse(unixTimeSeconds);
        }

        public string[] GitCommits(string? range, string[] pathSpecs, DateTimeOffset? after = null, DateTimeOffset? before = null, string? diffFilter = null)
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
                var output = RunGit(arguments.ToArray());
                return output.SplitOutput();
            }
            catch (GitFailedException e) when (e.ErrorOutput is "fatal: bad revision 'HEAD'")
            {
                // Empty repository
                return Array.Empty<string>();
            }
        }

        public string[] GitLastCommitHashes(AbsoluteDirectoryPath workingDirectory, string pathSpec)
        {
            var output = RunGit("rev-list", "HEAD", "--", pathSpec);
            return output.SplitOutput();
        }

        public (GitRef? currentRef, GitRef? mainBranch) GitCurrentBranch()
        {
            var output = RunGit("branch");
            var lines = output.SplitOutput();
            var refs = new List<GitRef>();
            GitRef? currentRef = null;

            if (!lines.Any())
            {
                return (null, null);
            }

            foreach (var line in lines)
            {
                var current = line.StartsWith("*");

                GitRef? lineRef;
                var detachedMatch = Regex.Match(line, "(HEAD detached at (?<ref>[^)]*))");

                if (detachedMatch.Success)
                {
                    lineRef = new GitRef(detachedMatch.Groups["ref"].Value, true);
                }
                else
                {
                    lineRef = new GitRef(line.TrimStart(' ', '*'), false);
                }

                if (current)
                {
                    currentRef = lineRef;
                }

                refs.Add(lineRef);
            }

            if (currentRef == null)
            {
                throw new Exception("Current branch not found.");
            }

            var mainBranches = refs.Where(r => MainBranchNames.Contains(r.Name)).ToArray();

            if (mainBranches.Length != 1)
            {
                if (mainBranches.Length == 0)
                {
                    throw new UserException($"Main branch not found. Expected a branch with name: {string.Join(", ", MainBranchNames)}");
                }

                throw new UserException($"Multiple main branches found. Expected 1 but found: {string.Join(", ", mainBranches.Select(b => b.Name))}");
            }

            var mainBranch = mainBranches.Single();

            return (currentRef, mainBranch);
        }

        public string[] GitMerges(AbsoluteDirectoryPath workingDirectory, string? range, AbsoluteDirectoryPath[] pathSpecs)
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

            var output = RunGit(arguments.ToArray());
            return output.SplitOutput();
        }

        public string[] GitFindFiles(string[] pathSpecs, bool includeUnstaged = false)
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

            var output = RunGit(arguments.ToArray());
            return output.SplitOutput();
        }

        public string[] GitDiffFileNames(string? range, string? pathSpec)
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

            var output = RunGit(arguments.ToArray());
            return output.SplitOutput();
        }

        public FileCommitContent[] FileCommitHistory(string filePath)
        {
            var commits = GitCommits(null, new[] { filePath });

            var args = new List<string>();

            var marker = Guid.NewGuid().ToString();

            args.Add("show");
            args.Add("--no-patch");
            args.Add($"--format=format:{marker}%H{marker}");

            foreach (var commit in commits)
            {
                args.Add(commit);
                args.Add($"{commit}:{Path.GetRelativePath(_workingDirectory.FullName, filePath)}");
            }

            var output = RunGit(args.ToArray());
            var splitOutput = output.Split(marker, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var list = new List<FileCommitContent>();

            for (var i = 0; i < splitOutput.Length; i += 2)
            {
                list.Add(new FileCommitContent(splitOutput[i], splitOutput[i + 1]));
            }

            return list.ToArray();
        }

        public bool IsShallowCheckout()
        {
            var output = RunGit("rev-parse", "--is-shallow-repository");
            return string.Equals(output.Trim(), "true", StringComparison.InvariantCultureIgnoreCase);
        }

        public void Fetch()
        {
            RunGit("fetch");
        }

        public string RunGit(params string[] arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                WorkingDirectory = _workingDirectory.ToString(),
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