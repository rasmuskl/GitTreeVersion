using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.IO;
using GitTreeVersion.Context;
using GitTreeVersion.Deployables;
using GitTreeVersion.Paths;
using Spectre.Console;

namespace GitTreeVersion.Commands
{
    public class VersionCommand : Command
    {
        public VersionCommand() : base("version", "Calculate versions for closest version root")
        {
            Handler = CommandHandler.Create<bool, bool, bool, bool, bool, string?>(Execute);

            AddOption(new Option<bool>("--apply", "Apply calculated version to project files"));
            AddOption(new Option<bool>("--skip-backups", "Skip backups of projects when applying versions"));
            AddOption(new Option<bool>("--skip-solution-info", "Skip SolutionInfo.cs references in .NET projects when applying versions"));
            AddOption(new Option<bool>("--set-build-number", "Set version in CI environment"));
            AddArgument(new Argument<string?>("path", () => "."));
        }

        private void Execute(bool apply, bool debug, bool setBuildNumber, bool skipBackups, bool skipSolutionInfo, string? path)
        {
            Log.IsDebug = debug;
            var applyBackupChangedFiles = !skipBackups;
            var skipSolutionInfoFiles = skipSolutionInfo;

            if (path is not null)
            {
                path = Path.GetFullPath(path);
            }

            path ??= Environment.CurrentDirectory;

            var stopwatch = Stopwatch.StartNew();
            var versionGraph = ContextResolver.GetVersionGraph(new AbsoluteDirectoryPath(path));

            Console.WriteLine($"Repository root: {versionGraph.RepositoryRootPath}");
            Console.WriteLine($"Primary version root: {versionGraph.PrimaryVersionRootPath}");

            var versionCalculator = new VersionCalculator();
            var primaryVersionRootVersion = versionCalculator.GetVersion(versionGraph, versionGraph.PrimaryVersionRootPath);

            AnsiConsole.MarkupLine($"Version: [lime]{primaryVersionRootVersion}[/]");

            if (setBuildNumber)
            {
                if (versionGraph.BuildEnvironment is null)
                {
                    throw new UserException("No build environment detected to set build number.");
                }

                versionGraph.BuildEnvironment.SetBuildNumber(primaryVersionRootVersion);
            }

            if (apply)
            {
                var relevantDeployables = versionGraph.GetRelevantDeployablesForVersionRoot(versionGraph.PrimaryVersionRootPath);

                foreach (var deployablePath in relevantDeployables)
                {
                    var deployableVersion = versionCalculator.GetVersion(versionGraph, versionGraph.DeployableFileVersionRoots[deployablePath]);

                    AnsiConsole.MarkupLine($"Applying version [lime]{deployableVersion}[/] to: {deployablePath.FullName}");

                    if (versionGraph.Deployables.TryGetValue(deployablePath, out var deployable))
                    {
                        deployable.ApplyVersion(deployableVersion, new ApplyOptions(applyBackupChangedFiles, skipSolutionInfoFiles));
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