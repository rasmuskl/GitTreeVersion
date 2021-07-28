using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using GitTreeVersion.Context;
using GitTreeVersion.Paths;
using Spectre.Console;

namespace GitTreeVersion.Commands
{
    public class TreeCommand : Command
    {
        public TreeCommand() : base("tree", "Render repository tree")
        {
            Handler = CommandHandler.Create<bool>(Execute);
        }

        private void Execute(bool debug)
        {
            Log.IsDebug = debug;

            var graph = ContextResolver.GetFileGraph(new AbsoluteDirectoryPath(Environment.CurrentDirectory));

            var rootVersionPath = graph.VersionRootPath;

            var version = new VersionCalculator().GetVersion(graph, rootVersionPath);
            var tree = new Tree($"{rootVersionPath.Name} [grey30][[[/][lime]{version}[/][grey30]]][/]");

            AddVersionRootChildren(tree, graph, rootVersionPath, version);

            AnsiConsole.Render(tree);
        }

        private void AddVersionRootChildren(IHasTreeNodes tree, FileGraph graph, AbsoluteDirectoryPath versionRootPath, Version version)
        {
            var childDeployables = graph.DeployableFileVersionRoots
                .Where(p => p.Value == versionRootPath)
                .Select(x => x.Key);
            
            foreach (var childDeployable in childDeployables)
            {
                tree.AddNode($"{Path.GetRelativePath(versionRootPath.ToString(), childDeployable.ToString())}  [grey30][[[/][grey54]{version}[/][grey30]]][/]");
            }

            foreach (var versionRootParent in graph.VersionRootParents)
            {
                Console.WriteLine($"{versionRootParent.Key}: {versionRootParent.Value}");
            }

            var childVersionRoots = graph.VersionRootParents
                .Where(p => p.Value.HasValue && p.Value.Value == versionRootPath)
                .Select(x => x.Key);
            
            foreach (var childVersionRoot in childVersionRoots)
            {
                var childVersion = new VersionCalculator().GetVersion(graph, childVersionRoot);
                var treeNode = tree.AddNode($"{Path.GetRelativePath(versionRootPath.ToString(), childVersionRoot.ToString())} [grey30][[[/][lime]{childVersion}[/][grey30]]][/]");
                AddVersionRootChildren(treeNode, graph, childVersionRoot, childVersion);
            }
        }
    }
}