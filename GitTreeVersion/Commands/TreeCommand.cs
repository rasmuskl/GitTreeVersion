using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
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

            var currentRoot = repositoryContext.GetVersionRoots();
            var version = versionCalculator.GetVersion(repositoryContext);

            var versionCache = new Dictionary<string, Version>();
            versionCache.Add(repositoryContext.VersionRootPath, version);

            var csprojFiles = Git.GitFindFiles(repositoryContext.VersionRootPath, ":(glob)**/*.csproj");
            var packageJsonFiles = Git.GitFindFiles(repositoryContext.VersionRootPath, ":(glob)**/package.json");
            var versionableFiles = csprojFiles.Concat(packageJsonFiles);

            var versionables = new List<Deployable>();

            foreach (var versionableFile in versionableFiles.Select(f => Path.Combine(repositoryContext.VersionRootPath, f)))
            {
                var directoryPath = Path.GetDirectoryName(versionableFile);

                if (directoryPath is null)
                {
                    continue;
                }

                var versionableContext = ContextResolver.GetRepositoryContext(directoryPath);

                if (!versionCache.TryGetValue(versionableContext.VersionRootPath, out var fileVersion))
                {
                    fileVersion = versionCalculator.GetVersion(versionableContext);
                    versionCache.Add(versionableContext.VersionRootPath, fileVersion);
                }

                var versionRoot = currentRoot.AllVersionRoots().First(r => r.Path == versionableContext.VersionRootPath);

                var versionable = new Deployable(versionableFile, versionRoot, fileVersion);
                versionables.Add(versionable);
            }

            var root = new Tree($"{Path.GetFileName(currentRoot.Path)} [grey30][[[/][lime]{versionCalculator.GetVersion(ContextResolver.GetRepositoryContext(currentRoot.Path))}[/][grey30]]][/]");
            var treeStack = new Stack<(VersionRoot versionRoot, IHasTreeNodes treeCursor)>();

            foreach (var versionable in versionables.Where(v => v.VersionRoot == currentRoot))
            {
                root.AddNode($"{Path.GetFileName(versionable.Path)} [grey30][[[/][grey54]{versionable.Version}[/][grey30]]][/]");
            }
            
            foreach (var childRoot in currentRoot.VersionRoots.Reverse())
            {
                treeStack.Push((childRoot, root as IHasTreeNodes));
            }

            while (treeStack.TryPop(out var tuple))
            {
                var (versionRoot, treeCursor) = tuple;
                var treeNode = treeCursor.AddNode($"{Path.GetFileName(versionRoot.Path)} [grey30][[[/][lime]{versionCalculator.GetVersion(ContextResolver.GetRepositoryContext(versionRoot.Path))}[/][grey30]]][/]");

                foreach (var versionable in versionables.Where(v => v.VersionRoot == versionRoot))
                {
                    treeNode.AddNode($"{Path.GetFileName(versionable.Path)} [grey30][[[/][grey54]{versionable.Version}[/][grey30]]][/]");
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