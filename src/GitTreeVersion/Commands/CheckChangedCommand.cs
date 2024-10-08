﻿using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using GitTreeVersion.Git;
using GitTreeVersion.Paths;

namespace GitTreeVersion.Commands
{
    public class CheckChangedCommand : Command
    {
        public CheckChangedCommand() : base("check-changed", "Checks if relevant files have changed")
        {
            Handler = CommandHandler.Create<bool, string?>(Execute);

            AddArgument(new Argument<string?>("path", () => "."));
        }

        private void Execute(bool debug, string? path)
        {
            Log.IsDebug = debug;

            if (path is not null)
            {
                path = Path.GetFullPath(path);
            }

            path ??= Environment.CurrentDirectory;

            var gitDirectory = new GitDirectory(new AbsoluteDirectoryPath(path));
            var(parent1, parent2) = gitDirectory.GetMergeParentCommitHashes();

            Console.WriteLine($"Found merge commit with parents {parent1} and {parent2}.");

            Console.WriteLine();
            Console.WriteLine("Relevant changed files:");
            Console.WriteLine();

            var fileNames = gitDirectory.GitDiffFileNames(parent1, parent2, null);

            foreach (var fileName in fileNames)
            {
                Console.WriteLine(fileName);
            }
        }
    }
}