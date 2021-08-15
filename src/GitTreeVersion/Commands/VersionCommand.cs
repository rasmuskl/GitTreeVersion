﻿using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.IO;
using System.Linq;
using GitTreeVersion.Context;
using GitTreeVersion.Paths;
using Spectre.Console;

namespace GitTreeVersion.Commands
{
    public class VersionCommand : Command
    {
        public VersionCommand() : base("version", "Versions the thing")
        {
            Handler = CommandHandler.Create<bool, bool, string?>(Execute);

            AddOption(new Option<bool>("--apply"));
            AddArgument(new Argument<string?>("path", () => null));
        }

        private void Execute(bool apply, bool debug, string? path)
        {
            Log.IsDebug = debug;

            if (path is not null)
            {
                path = Path.GetFullPath(path);
            }
            path ??= Environment.CurrentDirectory;

            var stopwatch = Stopwatch.StartNew();
            var fileGraph = ContextResolver.GetFileGraph(new AbsoluteDirectoryPath(path));

            Console.WriteLine($"Repository root: {fileGraph.RepositoryRootPath}");
            Console.WriteLine($"Version root: {fileGraph.VersionRootPath}");

            var versionCalculator = new VersionCalculator();
            var version = versionCalculator.GetVersion(fileGraph);

            AnsiConsole.MarkupLine($"Version: [green]{version}[/]");

            if (apply)
            {
                var relevantDeployables = fileGraph
                    .DeployableFileVersionRoots
                    .Where(p => p.Value == fileGraph.VersionRootPath)
                    .Select(p => p.Key);

                foreach (var deployablePath in relevantDeployables)
                {
                    AnsiConsole.MarkupLine($"Applying version [green]{version}[/] to: {deployablePath.FullName}");

                    if (fileGraph.Deployables.TryGetValue(deployablePath, out var deployable))
                    {
                        deployable.ApplyVersion(version);
                    }
                    else
                    {
                        Log.Warning($"Unable to apply version to: {deployablePath.FullName}");
                    }
                }
            }

            Log.Debug($"Elapsed: {stopwatch.ElapsedMilliseconds} ms");
        }
    }
}