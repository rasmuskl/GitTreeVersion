using System;
using GitTreeVersion.BuildEnvironments;
using GitTreeVersion.Paths;

namespace GitTreeVersion.Context
{
    public static class ContextResolver
    {
        public const string VersionConfigFileName = "version.json";

        public static FileGraph GetFileGraph(AbsoluteDirectoryPath workingDirectory, BuildEnvironmentDetector? buildEnvironmentDetector = null)
        {
            var repositoryRoot = FindDirectoryAboveContaining(workingDirectory, ".git");

            if (repositoryRoot is null)
            {
                throw new UserException("Not in a git repository.");
            }

            var configFilePath = FindFileAbove(workingDirectory, VersionConfigFileName);

            if (configFilePath is null)
            {
                return new FileGraph(repositoryRoot, repositoryRoot, buildEnvironmentDetector);
            }

            var versionRootPath = configFilePath.Parent;
            return new FileGraph(repositoryRoot, versionRootPath, buildEnvironmentDetector);
        }

        private static AbsoluteFilePath? FindFileAbove(AbsoluteDirectoryPath directory, string fileName)
        {
            var filePath = directory.CombineToFile(fileName);

            if (filePath.Exists)
            {
                return filePath;
            }

            if (directory.IsAtRoot)
            {
                return null;
            }

            return FindFileAbove(directory.Parent, fileName);
        }

        private static AbsoluteDirectoryPath? FindDirectoryAboveContaining(AbsoluteDirectoryPath directory, string directoryName)
        {
            var directoryPath = directory.CombineToDirectory(directoryName);

            if (directoryPath.Exists)
            {
                return directory;
            }

            if (directory.IsAtRoot)
            {
                return null;
            }

            return FindDirectoryAboveContaining(directory.Parent, directoryName);
        }
    }
}