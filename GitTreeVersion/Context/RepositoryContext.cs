using System.Collections.Generic;
using System.IO;
using System.Linq;
using GitTreeVersion.Commands;

namespace GitTreeVersion.Context
{
    public class RepositoryContext
    {
        public RepositoryContext(string repositoryRootPath, string versionRootPath, VersionConfig versionConfig)
        {
            RepositoryRootPath = repositoryRootPath;
            VersionRootPath = versionRootPath;
            VersionConfig = versionConfig;
        }

        public string RepositoryRootPath { get; }

        public string VersionRootPath { get; }

        public VersionConfig VersionConfig { get; }

        public VersionRoot GetVersionRoots()
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
    }
}