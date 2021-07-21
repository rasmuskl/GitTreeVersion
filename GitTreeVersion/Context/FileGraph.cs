using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GitTreeVersion.Deployables;

namespace GitTreeVersion.Context
{
    public class FileGraph
    {
        public string RepositoryRootPath { get; }
        public string VersionRootPath { get; }

        public string[] DeployableFilePaths { get; }
        public Dictionary<string, string> DeployableFileVersionRoots { get; }
        public Dictionary<string, string[]> DeployableFileDependencies { get; }

        public string[] VersionRootPaths { get; }
        public Dictionary<string, string?> VersionRootParents { get; }

        public FileGraph(string repositoryRootPath, string versionRootPath)
        {
            RepositoryRootPath = repositoryRootPath;
            VersionRootPath = versionRootPath;

            var rootStack = new Stack<string>();
            rootStack.Push(versionRootPath);

            var versionRootPaths = new List<string>();
            var versionRootParents = new Dictionary<string, string?>();

            versionRootPaths.Add(versionRootPath);
            versionRootParents[versionRootPath] = null;

            var versionDirectoryPaths = Git.GitFindFiles(VersionRootPath, ":(glob)**/version.json")
                .Select(Path.GetDirectoryName)
                .OrderBy(p => p);

            foreach (var versionDirectoryPath in versionDirectoryPaths)
            {
                if (string.IsNullOrEmpty(versionDirectoryPath))
                {
                    continue;
                }

                var rootPath = Path.Combine(VersionRootPath, versionDirectoryPath);

                var potentialParent = rootStack.Peek();

                while (!rootPath.StartsWith(potentialParent))
                {
                    rootStack.Pop();
                    potentialParent = rootStack.Peek();
                }

                versionRootParents[rootPath] = potentialParent;
                rootStack.Push(rootPath);
                versionRootPaths.Add(rootPath);
            }

            VersionRootPaths = versionRootPaths.ToArray();
            VersionRootParents = versionRootParents;

            var csprojFiles = Git.GitFindFiles(VersionRootPath, ":(glob)**/*.csproj");
            var packageJsonFiles = Git.GitFindFiles(VersionRootPath, ":(glob)**/package.json");

            var deployableFilePaths = csprojFiles
                .Concat(packageJsonFiles)
                .Select(f => Path.GetFullPath(Path.Combine(VersionRootPath, f)))
                .ToHashSet();

            var deployableVersionRoots = new Dictionary<string, string>();

            foreach (var deployableFilePath in deployableFilePaths)
            {
                var deployableVersionRoot = versionRootPaths
                    .Where(p => deployableFilePath.StartsWith(p))
                    .OrderByDescending(p => p.Length)
                    .First();

                deployableVersionRoots[deployableFilePath] = deployableVersionRoot;
            }

            var deployableDependencies = new Dictionary<string, string[]>();
            var deployableQueue = new Queue<string>(deployableFilePaths);
            var csprojDeployableProcessor = new CsprojDeployableProcessor();

            while (deployableQueue.TryDequeue(out var deployableFilePath))
            {
                if (!File.Exists(deployableFilePath))
                {
                    Console.WriteLine($"File not found: {deployableFilePath}");
                    continue;
                }

                var fileName = Path.GetFileName(deployableFilePath);

                if (fileName == "package.json")
                {
                    deployableDependencies[fileName] = Array.Empty<string>();
                }
                else if (Path.GetExtension(deployableFilePath) == ".csproj")
                {
                    var referencedDeployablePaths =
                        csprojDeployableProcessor.GetSourceReferencedDeployablePaths(new FileInfo(deployableFilePath));
                    deployableDependencies[deployableFilePath] = referencedDeployablePaths;

                    foreach (var path in referencedDeployablePaths)
                    {
                        if (deployableDependencies.ContainsKey(path))
                        {
                            continue;
                        }

                        deployableQueue.Enqueue(path);
                        deployableFilePaths.Add(path);
                    }
                }
                else
                {
                    throw new InvalidOperationException($"Unknown deployable: {deployableFilePath}");
                }
            }

            DeployableFilePaths = deployableFilePaths.ToArray();
            DeployableFileDependencies = deployableDependencies;
            DeployableFileVersionRoots = deployableVersionRoots;
        }

        public string[] GetRelevantPathsForVersionRoot(string versionRootPath)
        {
            var nestedVersionRoots = VersionRootPaths
                .Where(p => p.StartsWith(versionRootPath))
                .ToArray();
            
            var nestedDeployables = GetReachableDeployables(DeployableFilePaths
                    .Where(p => nestedVersionRoots.Any(p.StartsWith)))
                    .ToArray();
            
            var relevantDirectories = nestedVersionRoots
                .Concat(nestedDeployables.Select(Path.GetDirectoryName))
                .OrderBy(x => x)
                .ToArray();

            var paths = new List<string>();
            string? previous = null;
            
            foreach (var relevantDirectory in relevantDirectories)
            {
                if (relevantDirectory is null)
                {
                    continue;
                }
                
                if (previous is not null && relevantDirectory.StartsWith(previous))
                {
                    continue;
                }

                previous = relevantDirectory;
                paths.Add(relevantDirectory);
            }

            foreach (var pathOutsideRepository in paths.Where(p => !p.StartsWith(RepositoryRootPath)))
            {
                Console.WriteLine($"Relevant path outside repository: {pathOutsideRepository}");
            }
            
            paths.RemoveAll(p => !p.StartsWith(RepositoryRootPath));

            return paths.ToArray();
        }

        private IEnumerable<string> GetReachableDeployables(IEnumerable<string> deployablePaths)
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