using System;
using System.IO;

namespace GitTreeVersion.Context
{
    public static class ContextResolver
    {
        public const string VersionConfigFileName = "version.json";

        public static FileGraph GetFileGraph(string workingDirectory)
        {
            var repositoryRoot = FindDirectoryAbove(workingDirectory, ".git");

            if (repositoryRoot is null)
            {
                throw new InvalidOperationException("Not in a git repository");
            }
            
            var configFilePath = FindFileAbove(workingDirectory, VersionConfigFileName);

            if (configFilePath == null)
            {
                return new FileGraph(repositoryRoot, repositoryRoot);
            }
            
            var versionRootPath = Path.GetDirectoryName(configFilePath);
            return new FileGraph(repositoryRoot, versionRootPath!);
        }

        private static string? FindFileAbove(string directory, string fileName)
        {
            var filePath = Path.Combine(directory, fileName);
            
            if (File.Exists(filePath))
            {
                return filePath;
            }
            
            var parentDirectory = Path.GetDirectoryName(directory);

            if (parentDirectory == null)
            {
                return null;
            }
            
            return FindFileAbove(parentDirectory, fileName);
        }

        private static string? FindDirectoryAbove(string directory, string directoryName)
        {
            var directoryPath = Path.Combine(directory, directoryName);
            
            if (Directory.Exists(directoryPath))
            {
                return directory;
            }
            
            var parentDirectory = Path.GetDirectoryName(directory);

            if (parentDirectory == null)
            {
                return null;
            }
            
            return FindDirectoryAbove(parentDirectory, directoryName);
        }
    }
}