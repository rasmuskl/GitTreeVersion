using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.IO;
using System.Linq;
using GitTreeVersion.Context;
using Spectre.Console;

namespace GitTreeVersion.Commands
{
    public class TreeCommand : Command
    {
        public TreeCommand() : base("tree", "Render repository tree")
        {
            Handler = CommandHandler.Create(Execute);
        }

        private void Execute()
        {
            var versionCalculator = new VersionCalculator();
            var graph = ContextResolver.GetFileGraph(Environment.CurrentDirectory);

            var rootVersionPath = graph.VersionRootPath;

            var version = versionCalculator.GetVersion(graph, rootVersionPath);
            var tree = new Tree($"{Path.GetFileName(rootVersionPath)} [grey30][[[/][lime]{version}[/][grey30]]][/]");

            AddVersionRootChildren(tree, graph, rootVersionPath, version);

            AnsiConsole.Render(tree);
        }

        private void AddVersionRootChildren(IHasTreeNodes tree, FileGraph graph, string versionRootPath, Version version)
        {
            var childDeployables = graph.DeployableFileVersionRoots
                .Where(p => p.Value == versionRootPath)
                .Select(x => x.Key);
            
            foreach (var childDeployable in childDeployables)
            {
                tree.AddNode($"{Path.GetRelativePath(versionRootPath, childDeployable)}  [grey30][[[/][grey54]{version}[/][grey30]]][/]");
            }

            var childVersionRoots = graph.VersionRootParents
                .Where(p => p.Value == versionRootPath)
                .Select(x => x.Key);
            
            foreach (var childVersionRoot in childVersionRoots)
            {
                var childVersion = new VersionCalculator().GetVersion(graph, childVersionRoot);
                var treeNode = tree.AddNode($"{Path.GetRelativePath(versionRootPath, childVersionRoot)} [grey30][[[/][lime]{childVersion}[/][grey30]]][/]");
                AddVersionRootChildren(treeNode, graph, childVersionRoot, childVersion);
            }
        }
    }
}