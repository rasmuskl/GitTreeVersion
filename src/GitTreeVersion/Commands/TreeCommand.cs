using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.IO;
using System.Linq;
using GitTreeVersion.Context;
using GitTreeVersion.Paths;
using Semver;
using Spectre.Console;

namespace GitTreeVersion.Commands
{
    public class TreeCommand : Command
    {
        private readonly VersionCalculator _versionCalculator = new();

        public TreeCommand() : base("tree", "Render repository tree")
        {
            Handler = CommandHandler.Create<bool, string?>(Execute);

            AddArgument(new Argument<string?>("path", () => null));
        }

        private void Execute(bool debug, string? path)
        {
            Log.IsDebug = debug;

            if (path is not null)
            {
                path = Path.GetFullPath(path);
            }

            path ??= Environment.CurrentDirectory;

            var stopwatch = Stopwatch.StartNew();

            var versionGraph = ContextResolver.GetVersionGraph(new AbsoluteDirectoryPath(path));

            var version = _versionCalculator.GetVersion(versionGraph, versionGraph.PrimaryVersionRootPath);
            var tree = new Tree($"{versionGraph.PrimaryVersionRootPath.Name} [grey30][[[/][lime]{version}[/][grey30]]][/]");

            AddVersionRootChildren(tree, versionGraph, versionGraph.PrimaryVersionRootPath, version);

            AnsiConsole.Render(tree);
            Log.Debug($"Elapsed: {stopwatch.ElapsedMilliseconds} ms");
        }

        private void AddVersionRootChildren(IHasTreeNodes tree, VersionGraph graph, AbsoluteDirectoryPath versionRootPath, SemVersion version)
        {
            var childDeployables = graph.DeployableFileVersionRoots
                .Where(p => p.Value == versionRootPath)
                .Select(x => x.Key);

            foreach (var childDeployable in childDeployables)
            {
                tree.AddNode($"{Path.GetRelativePath(versionRootPath.ToString(), childDeployable.ToString())}  [grey30][[[/][grey54]{version}[/][grey30]]][/]");
            }

            var childVersionRoots = graph.VersionRootParents
                .Where(p => p.Value is not null && p.Value == versionRootPath)
                .Select(x => x.Key);

            foreach (var childVersionRoot in childVersionRoots)
            {
                var childVersion = _versionCalculator.GetVersion(graph, childVersionRoot);
                var treeNode = tree.AddNode($"{Path.GetRelativePath(versionRootPath.ToString(), childVersionRoot.ToString())} [grey30][[[/][lime]{childVersion}[/][grey30]]][/]");
                AddVersionRootChildren(treeNode, graph, childVersionRoot, childVersion);
            }
        }
    }
}