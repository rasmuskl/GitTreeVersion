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
            var repositoryContext = ContextResolver.GetRepositoryContext(Environment.CurrentDirectory);

            var currentRoot = repositoryContext.VersionRoot;
            var deployables = repositoryContext.GetDeployables();

            var root = new Tree($"{Path.GetFileName(currentRoot.Path)} [grey30][[[/][lime]{versionCalculator.GetVersion(repositoryContext)}[/][grey30]]][/]");
            var treeStack = new Stack<(VersionRoot versionRoot, IHasTreeNodes treeCursor)>();

            foreach (var deployable in deployables.Where(v => v.VersionRoot == currentRoot))
            {
                root.AddNode($"{Path.GetFileName(deployable.Path)} [grey30][[[/][grey54]{deployable.Version}[/][grey30]]][/]");
            }
            
            foreach (var childRoot in currentRoot.VersionRoots.Reverse())
            {
                treeStack.Push((childRoot, root as IHasTreeNodes));
            }

            while (treeStack.TryPop(out var tuple))
            {
                var (versionRoot, treeCursor) = tuple;
                var treeNode = treeCursor.AddNode($"{Path.GetFileName(versionRoot.Path)} [grey30][[[/][lime]{versionCalculator.GetVersion(ContextResolver.GetRepositoryContext(repositoryContext, versionRoot))}[/][grey30]]][/]");

                foreach (var deployable in deployables.Where(v => v.VersionRoot == versionRoot))
                {
                    treeNode.AddNode($"{Path.GetFileName(deployable.Path)} [grey30][[[/][grey54]{deployable.Version}[/][grey30]]][/]");
                }

                foreach (var childRoot in versionRoot.VersionRoots.Reverse())
                {
                    treeStack.Push((childRoot, treeNode));
                }
            }

            AnsiConsole.Render(root);
        }
    }
}