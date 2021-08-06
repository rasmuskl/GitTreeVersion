using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Linq;
using GitTreeVersion.Context;
using GitTreeVersion.Paths;
using GitTreeVersion.VersionAppliers;
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
                var relevantDeployables = fileGraph.DeployableFileVersionRoots.Where(p => p.Value == fileGraph.VersionRootPath).Select(p => p.Key).ToArray();

                foreach (var deployable in relevantDeployables)
                {
                    AnsiConsole.MarkupLine($"Applying version [green]{version}[/] to: {deployable.FullName}");

                    if (deployable.Extension == ".csproj")
                    {
                        new DotnetVersionApplier().ApplyVersion(deployable, version);
                    }
                    else if (deployable.FileName == "package.json")
                    {
                        new NpmVersionApplier().ApplyVersion(deployable, version);
                    }
                    else
                    {
                        Log.Warning($"Unable to apply version to: {deployable.FullName}");
                    }
                }
            }

            Log.Debug($"Elapsed: {stopwatch.ElapsedMilliseconds} ms");
        }
    }
}