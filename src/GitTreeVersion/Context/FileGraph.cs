﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using GitTreeVersion.BuildEnvironments;
using GitTreeVersion.Deployables;
using GitTreeVersion.Git;
using GitTreeVersion.Paths;

namespace GitTreeVersion.Context
{
    public class FileGraph
    {
        private readonly DeployableResolver _deployableResolver = new();

        public FileGraph(AbsoluteDirectoryPath repositoryRootPath, AbsoluteDirectoryPath versionRootPath, BuildEnvironmentDetector? buildEnvironmentDetector)
        {
            RepositoryRootPath = repositoryRootPath;
            VersionRootPath = versionRootPath;

            var branchStatus = new GitDirectory(repositoryRootPath).GitCurrentBranch();
            CurrentBranch = branchStatus.currentRef;
            MainBranch = branchStatus.mainBranch;
            BuildEnvironment = (buildEnvironmentDetector ?? new BuildEnvironmentDetector()).GetBuildEnvironment();

            var rootStack = new Stack<AbsoluteDirectoryPath>();
            rootStack.Push(versionRootPath);

            var versionRootPaths = new List<AbsoluteDirectoryPath>();
            var versionRootParents = new Dictionary<AbsoluteDirectoryPath, AbsoluteDirectoryPath?>();
            var versionRootConfigs = new Dictionary<AbsoluteDirectoryPath, VersionConfig>();

            versionRootPaths.Add(versionRootPath);
            versionRootParents[versionRootPath] = null;

            var gitDirectory = new GitDirectory(VersionRootPath);
            var versionDirectoryPaths = gitDirectory.GitFindFiles(new[] { ":(glob)**/version.json" }, true)
                .Select(Path.GetDirectoryName)
                .OrderBy(p => p);

            foreach (var versionDirectoryPath in versionDirectoryPaths)
            {
                if (string.IsNullOrEmpty(versionDirectoryPath))
                {
                    continue;
                }

                var rootPath = VersionRootPath.CombineToDirectory(versionDirectoryPath);

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
                var filePath = rootPath.CombineToFile(ContextResolver.VersionConfigFileName);
                VersionConfig? versionConfig = null;

                if (filePath.Exists)
                {
                    versionConfig = JsonSerializer.Deserialize<VersionConfig>(File.ReadAllText(filePath.ToString()), JsonOptions.DefaultOptions);
                }

                versionRootConfigs[rootPath] = versionConfig ?? VersionConfig.Default;
            }

            VersionRootPaths = versionRootPaths.ToArray();
            VersionRootParents = versionRootParents;
            VersionRootConfigs = versionRootConfigs;

            var relevantDeployableFiles = gitDirectory.GitFindFiles(new[] { ":(glob)**/*.csproj", ":(glob)**/package.json" }, true);

            var relevantDeployableFilePaths = relevantDeployableFiles
                .Select(f => VersionRootPath.CombineToFile(f));

            var deployableVersionRoots = new Dictionary<AbsoluteFilePath, AbsoluteDirectoryPath>();
            var deployableDependencies = new Dictionary<AbsoluteFilePath, AbsoluteFilePath[]>();
            var deployableFilePaths = new HashSet<AbsoluteFilePath>();
            var deployables = new List<IDeployable>();

            var deployableQueue = new Queue<AbsoluteFilePath>(relevantDeployableFilePaths);

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

                var deployable = _deployableResolver.Resolve(deployableFilePath);

                if (deployable is null)
                {
                    Log.Warning($"Unknown deployable: {deployableFilePath}");
                    continue;
                }

                deployables.Add(deployable);

                foreach (var path in deployable.ReferencedDeployablePaths)
                {
                    if (deployableFilePaths.Contains(path))
                    {
                        continue;
                    }

                    deployableQueue.Enqueue(path);
                }

                deployableFilePaths.Add(deployableFilePath);
            }

            foreach (var deployable in deployables)
            {
                var deployableVersionRoot = versionRootPaths
                    .Where(p => deployable.FilePath.IsInSubPathOf(p))
                    .OrderByDescending(p => p.PathLength)
                    .FirstOrDefault();

                if (deployableVersionRoot is null)
                {
                    continue;
                }

                deployableVersionRoots[deployable.FilePath] = deployableVersionRoot;
            }

            DeployableFileDependencies = deployableDependencies;
            DeployableFileVersionRoots = deployableVersionRoots;
            Deployables = deployables.ToDictionary(d => d.FilePath, d => d);
        }

        public GitRef? MainBranch { get; }
        public GitRef? CurrentBranch { get; }
        public IBuildEnvironment? BuildEnvironment { get; }
        public AbsoluteDirectoryPath RepositoryRootPath { get; }
        public AbsoluteDirectoryPath VersionRootPath { get; }
        public Dictionary<AbsoluteFilePath, IDeployable> Deployables { get; }
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

            var nestedDeployables = GetReachableDeployables(Deployables
                    .Values
                    .Select(d => d.FilePath)
                    .Where(d => nestedVersionRoots.Any(d.IsInSubPathOf)))
                .ToArray();

            var relevantDirectories = nestedVersionRoots
                .Concat(nestedDeployables.Select(d => d.Parent))
                .OrderBy(x => x.ToString())
                .ToArray();

            var paths = new List<AbsoluteDirectoryPath>();

            AbsoluteDirectoryPath? previous = null;
            foreach (var relevantDirectory in relevantDirectories)
            {
                if (previous is not null && relevantDirectory.IsInSubPathOf(previous))
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