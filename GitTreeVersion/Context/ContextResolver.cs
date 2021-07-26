using System;
using System.IO;
using GitTreeVersion.Paths;

namespace GitTreeVersion.Context
{
    public static class ContextResolver
    {
        public const string VersionConfigFileName = "version.json";

        public static FileGraph GetFileGraph(AbsoluteDirectoryPath workingDirectory)
        {
            var repositoryRoot = FindDirectoryAbove(workingDirectory, ".git");

            if (repositoryRoot is null)
            {
                throw new InvalidOperationException("Not in a git repository");
            }
            
            var configFilePath = FindFileAbove(workingDirectory, VersionConfigFileName);

            if (configFilePath == null)
            {
                return new FileGraph(repositoryRoot.Value, repositoryRoot.Value);
            }
            
            var versionRootPath = configFilePath.Value.Parent;
            return new FileGraph(repositoryRoot.Value, versionRootPath);
        }

        private static AbsoluteFilePath? FindFileAbove(AbsoluteDirectoryPath directory, string fileName)
        {
            var filePath = Path.Combine(directory.ToString(), fileName);
            
            if (File.Exists(filePath))
            {
                return new AbsoluteFilePath(filePath);
            }
            
            if (directory.IsAtRoot)
            {
                return null;
            }
            
            return FindFileAbove(directory.Parent, fileName);
        }

        private static AbsoluteDirectoryPath? FindDirectoryAbove(AbsoluteDirectoryPath directory, string directoryName)
        {
            var directoryPath = Path.Combine(directory.ToString(), directoryName);
            
            if (Directory.Exists(directoryPath))
            {
                return directory;
            }

            if (directory.IsAtRoot)
            {
                return null;
            }

            return FindDirectoryAbove(directory.Parent, directoryName);
        }
    }
}