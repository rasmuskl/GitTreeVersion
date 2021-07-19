using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GitTreeVersion.Deployables;

namespace GitTreeVersion.Context
{
    public class RepositoryContext
    {
        public RepositoryContext(string repositoryRootPath, string versionRootPath, VersionConfig versionConfig)
        {
            RepositoryRootPath = repositoryRootPath;
            VersionRootPath = versionRootPath;
            VersionConfig = versionConfig;
            VersionRoot = GetVersionRoot();
        }

        public RepositoryContext(string repositoryRootPath, VersionRoot versionRoot, VersionConfig versionConfig)
        {
            RepositoryRootPath = repositoryRootPath;
            VersionRootPath = versionRoot.Path;
            VersionConfig = versionConfig;
            VersionRoot = versionRoot;
        }

        public string RepositoryRootPath { get; }

        public string VersionRootPath { get; }

        public VersionConfig VersionConfig { get; }
        
        public VersionRoot VersionRoot { get; }

        private VersionRoot GetVersionRoot()
        {
            var rootVersionRoot = new VersionRoot(VersionRootPath);

            var versionDirectoryPaths = Git.GitFindFiles(VersionRootPath, ":(glob)**/version.json")
                .Select(Path.GetDirectoryName)
                .OrderBy(p => p);

            var rootStack = new Stack<VersionRoot>();
            rootStack.Push(rootVersionRoot);

            foreach (var versionDirectoryPath in versionDirectoryPaths)
            {
                if (string.IsNullOrEmpty(versionDirectoryPath)) continue;

                var versionRoot = new VersionRoot(Path.Combine(VersionRootPath, versionDirectoryPath));

                var potentialParent = rootStack.Peek();

                while (!versionRoot.Path.StartsWith(potentialParent.Path))
                {
                    rootStack.Pop();
                    potentialParent = rootStack.Peek();
                }

                potentialParent.AddVersionRoot(versionRoot);
                rootStack.Push(versionRoot);
            }

            return rootVersionRoot;
        }

        public Deployable[] GetDeployables()
        {
            var versionCache = new Dictionary<string, Version>();

            var csprojFiles = Git.GitFindFiles(VersionRootPath, ":(glob)**/*.csproj");
            var packageJsonFiles = Git.GitFindFiles(VersionRootPath, ":(glob)**/package.json");
            var deployableFiles = csprojFiles.Concat(packageJsonFiles);

            var deployables = new List<Deployable>();

            foreach (var deployableFile in deployableFiles.Select(f => Path.Combine(VersionRootPath, f)))
            {
                var directoryPath = Path.GetDirectoryName(deployableFile);

                if (directoryPath is null)
                {
                    continue;
                }
                
                var versionableContext = ContextResolver.GetRepositoryContext(directoryPath);

                if (!versionCache.TryGetValue(versionableContext.VersionRootPath, out var version))
                {
                    version = new VersionCalculator().GetVersion(versionableContext);
                    versionCache.Add(versionableContext.VersionRootPath, version);
                }

                var versionRoot = VersionRoot.AllVersionRoots().First(r => r.Path == versionableContext.VersionRootPath);
                var versionable = new Deployable(deployableFile, versionRoot, version);
                deployables.Add(versionable);
            }

            return deployables.ToArray();
        }
    }
}