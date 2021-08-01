using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using GitTreeVersion.Deployables;
using GitTreeVersion.Paths;

namespace GitTreeVersion.Context
{
    public class FileGraph
    {
        public FileGraph(AbsoluteDirectoryPath repositoryRootPath, AbsoluteDirectoryPath versionRootPath)
        {
            RepositoryRootPath = repositoryRootPath;
            VersionRootPath = versionRootPath;

            var rootStack = new Stack<AbsoluteDirectoryPath>();
            rootStack.Push(versionRootPath);

            var versionRootPaths = new List<AbsoluteDirectoryPath>();
            var versionRootParents = new Dictionary<AbsoluteDirectoryPath, AbsoluteDirectoryPath?>();
            var versionRootConfigs = new Dictionary<AbsoluteDirectoryPath, VersionConfig>();

            versionRootPaths.Add(versionRootPath);
            versionRootParents[versionRootPath] = null;

            var versionDirectoryPaths = Git.GitFindFiles(VersionRootPath, new[] {":(glob)**/version.json"}, true)
                .Select(Path.GetDirectoryName)
                .OrderBy(p => p);

            foreach (var versionDirectoryPath in versionDirectoryPaths)
            {
                if (string.IsNullOrEmpty(versionDirectoryPath))
                {
                    continue;
                }

                var rootPath = new AbsoluteDirectoryPath(Path.Combine(VersionRootPath.ToString(), versionDirectoryPath));

                var potentialParent = rootStack.Peek();

                while (!rootPath.IsInSubPathOf(potentialParent))
                {
                    rootStack.Pop();
                    potentialParent = rootStack.Peek();
                }

                versionRootParents[rootPath] = potentialParent;
                rootStack.Push(rootPath);
                versionRootPaths.Add(rootPath);
            }

            foreach (var rootPath in versionRootPaths)
            {
                var filePath = Path.Combine(rootPath.ToString(), ContextResolver.VersionConfigFileName);
                VersionConfig? versionConfig = null;

                if (File.Exists(filePath))
                {
                    versionConfig = JsonSerializer.Deserialize<VersionConfig>(File.ReadAllText(filePath), JsonOptions.DefaultOptions);
                }

                versionRootConfigs[rootPath] = versionConfig ?? VersionConfig.Default;
            }

            VersionRootPaths = versionRootPaths.ToArray();
            VersionRootParents = versionRootParents;
            VersionRootConfigs = versionRootConfigs;

            var relevantDeployableFiles = Git.GitFindFiles(VersionRootPath, new[] {":(glob)**/*.csproj", ":(glob)**/package.json"}, true);

            var relevantDeployableFilePaths = relevantDeployableFiles
                .Select(f => new AbsoluteFilePath(Path.GetFullPath(Path.Combine(VersionRootPath.ToString(), f))));

            var deployableVersionRoots = new Dictionary<AbsoluteFilePath, AbsoluteDirectoryPath>();
            var deployableDependencies = new Dictionary<AbsoluteFilePath, AbsoluteFilePath[]>();
            var deployableQueue = new Queue<AbsoluteFilePath>(relevantDeployableFilePaths);
            var dotnetDeployableProcessor = new DotnetDeployableProcessor();

            var deployableFilePaths = new HashSet<AbsoluteFilePath>();

            while (deployableQueue.TryDequeue(out var deployableFilePath))
            {
                if (deployableFilePaths.Contains(deployableFilePath))
                {
                    continue;
                }

                if (!deployableFilePath.Exists)
                {
                    Log.Warning($"File not found: {deployableFilePath}");
                    continue;
                }

                var fileName = deployableFilePath.FileName;

                if (fileName == "package.json")
                {
                    deployableDependencies[deployableFilePath] = Array.Empty<AbsoluteFilePath>();
                }
                else if (deployableFilePath.Extension == ".csproj" || deployableFilePath.Extension == ".vbproj")
                {
                    var referencedDeployablePaths = dotnetDeployableProcessor.GetSourceReferencedDeployablePaths(new FileInfo(deployableFilePath.ToString()));
                    deployableDependencies[deployableFilePath] = referencedDeployablePaths;

                    foreach (var path in referencedDeployablePaths)
                    {
                        if (deployableDependencies.ContainsKey(path))
                        {
                            continue;
                        }

                        deployableQueue.Enqueue(path);
                    }
                }
                else
                {
                    Log.Warning($"Unknown deployable: {deployableFilePath}");
                    continue;
                }

                deployableFilePaths.Add(deployableFilePath);
            }

            foreach (var deployableFilePath in deployableFilePaths)
            {
                var deployableVersionRoot = versionRootPaths
                    .Where(p => deployableFilePath.IsInSubPathOf(p))
                    .OrderByDescending(p => p.PathLength)
                    .First();

                deployableVersionRoots[deployableFilePath] = deployableVersionRoot;
            }

            DeployableFilePaths = deployableFilePaths.ToArray();
            DeployableFileDependencies = deployableDependencies;
            DeployableFileVersionRoots = deployableVersionRoots;
        }

        public AbsoluteDirectoryPath RepositoryRootPath { get; }
        public AbsoluteDirectoryPath VersionRootPath { get; }

        public AbsoluteFilePath[] DeployableFilePaths { get; }
        public Dictionary<AbsoluteFilePath, AbsoluteDirectoryPath> DeployableFileVersionRoots { get; }
        public Dictionary<AbsoluteFilePath, AbsoluteFilePath[]> DeployableFileDependencies { get; }

        public AbsoluteDirectoryPath[] VersionRootPaths { get; }
        public Dictionary<AbsoluteDirectoryPath, AbsoluteDirectoryPath?> VersionRootParents { get; }
        public Dictionary<AbsoluteDirectoryPath, VersionConfig> VersionRootConfigs { get; }

        public AbsoluteDirectoryPath[] GetRelevantPathsForVersionRoot(AbsoluteDirectoryPath versionRootPath)
        {
            var nestedVersionRoots = VersionRootPaths
                .Where(p => p.IsInSubPathOf(versionRootPath))
                .ToArray();

            var nestedDeployables = GetReachableDeployables(DeployableFilePaths
                    .Where(fp => nestedVersionRoots.Any(fp.IsInSubPathOf)))
                .ToArray();

            var relevantDirectories = nestedVersionRoots
                .Concat(nestedDeployables.Select(d => d.Parent))
                .OrderBy(x => x.ToString())
                .ToArray();

            var paths = new List<AbsoluteDirectoryPath>();
            AbsoluteDirectoryPath? previous = null;

            foreach (var relevantDirectory in relevantDirectories)
            {
                if (previous is not null && relevantDirectory.IsInSubPathOf(previous.Value))
                {
                    continue;
                }

                previous = relevantDirectory;
                paths.Add(relevantDirectory);
            }

            foreach (var pathOutsideRepository in paths.Where(p => !p.IsInSubPathOf(RepositoryRootPath)))
            {
                Log.Warning($"Relevant path outside repository: {pathOutsideRepository}");
            }

            paths.RemoveAll(p => !p.IsInSubPathOf(RepositoryRootPath));

            return paths.ToArray();
        }

        private IEnumerable<AbsoluteFilePath> GetReachableDeployables(IEnumerable<AbsoluteFilePath> deployablePaths)
        {
            foreach (var deployablePath in deployablePaths)
            {
                yield return deployablePath;

                if (!DeployableFileDependencies.TryGetValue(deployablePath, out var deployableDependencies))
                {
                    continue;
                }

                foreach (var dependencyPath in GetReachableDeployables(deployableDependencies))
                {
                    yield return dependencyPath;
                }
            }
        }
    }
}