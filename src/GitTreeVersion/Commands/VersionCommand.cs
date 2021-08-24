using System;
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
        public VersionCommand() : base("version", "Calculate versions for closest version root")
        {
            Handler = CommandHandler.Create<bool, bool, bool, string?>(Execute);

            AddOption(new Option<bool>("--apply"));
            AddOption(new Option<bool>("--set-build-number"));
            AddArgument(new Argument<string?>("path", () => null));
        }

        private void Execute(bool apply, bool debug, bool setBuildNumber, string? path)
        {
            Log.IsDebug = debug;

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
                var relevantDeployables = versionGraph
                    .DeployableFileVersionRoots
                    .Select(p => p.Key);

                foreach (var deployablePath in relevantDeployables)
                {
                    var deployableVersion = versionCalculator.GetVersion(versionGraph, versionGraph.DeployableFileVersionRoots[deployablePath]);

                    AnsiConsole.MarkupLine($"Applying version [lime]{deployableVersion}[/] to: {deployablePath.FullName}");

                    if (versionGraph.Deployables.TryGetValue(deployablePath, out var deployable))
                    {
                        deployable.ApplyVersion(deployableVersion);
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