using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using GitTreeVersion.Context;

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

            var csprojFiles = Git.GitFindFiles(repositoryContext.RepositoryRootPath, ":(glob)**/*.csproj");
            var packageJsonFiles = Git.GitFindFiles(repositoryContext.RepositoryRootPath, ":(glob)**/package.json");
            var versionFiles = Git.GitFindFiles(repositoryContext.RepositoryRootPath, ":(glob)**/version.json");

            var versionRoots = new List<string>();
            versionRoots.Add(repositoryContext.VersionRootPath);

            var versionRootNodes = new Dictionary<string, TreeNode>();

            foreach (var versionFile in versionFiles.OrderBy(v => v.Length))
            {
                var versionRootPath = Path.GetDirectoryName(Path.Combine(repositoryContext.RepositoryRootPath, versionFile));

                if (versionRootPath is null)
                {
                    continue;
                }
                
                versionRoots.Add(versionRootPath);
            }

            foreach (var versionRoot in versionRoots)
            {
                var versionRootNode = new TreeNode($"*{Path.GetFileName(versionRoot)} [{versionCalculator.GetVersion(ContextResolver.GetRepositoryContext(versionRoot))}]");
                versionRootNodes[versionRoot] = versionRootNode;
                // Console.WriteLine($"Version root: {versionRoot}");
                
                var parentRootNodeKey = versionRootNodes.Keys
                    .OrderByDescending(k => k.Length)
                    .FirstOrDefault(k => versionRoot != k && versionRoot.StartsWith(k));

                if (parentRootNodeKey is null)
                {
                    continue;
                }

                var parentRootNode = versionRootNodes[parentRootNodeKey];
                parentRootNode.AddChild(versionRootNode);
            }

            var version = versionCalculator.GetVersion(repositoryContext);

            var versionCache = new Dictionary<string, Version>();
            versionCache.Add(repositoryContext.VersionRootPath, version);

            var versionableFiles = csprojFiles.Concat(packageJsonFiles);
            foreach (var versionableFile in versionableFiles.Select(f => Path.Combine(repositoryContext.RepositoryRootPath, f)))
            {
                var directoryPath = Path.GetDirectoryName(versionableFile);
                var directoryName = Path.GetFileName(directoryPath);

                if (directoryName is null || directoryPath is null)
                {
                    continue;
                }

                var fileContext = ContextResolver.GetRepositoryContext(directoryPath);

                if (!versionCache.TryGetValue(fileContext.VersionRootPath, out var fileVersion))
                {
                    fileVersion = versionCalculator.GetVersion(fileContext);
                    versionCache.Add(fileContext.VersionRootPath, fileVersion);
                }
                
                versionRootNodes[fileContext.VersionRootPath].AddChild(new TreeNode($"{Path.GetFileName(versionableFile)} [{fileVersion}]"));
            }
            
            new TreeRenderer().WriteTree(versionRootNodes[versionRoots[0]]);
        }
    }
}