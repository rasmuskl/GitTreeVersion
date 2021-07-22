using System;
using System.IO;

namespace GitTreeVersion
{
    public static class PathExtensions
    {
        public static bool IsInSubPathOf(this DirectoryInfo directory, DirectoryInfo potentialParentDirectory)
        {
            var directoryPath = Path.TrimEndingDirectorySeparator(directory.FullName) + Path.DirectorySeparatorChar;
            var potentialParentDirectoryPath = Path.TrimEndingDirectorySeparator(potentialParentDirectory.FullName) + Path.DirectorySeparatorChar;
            return directoryPath.StartsWith(potentialParentDirectoryPath);
        }

        public static bool IsInSubPathOf(this FileInfo file, DirectoryInfo potentialParentDirectory)
        {
            if (file.Directory == null)
            {
                return false;
            }

            var directoryPath = file.Directory.FullName + Path.DirectorySeparatorChar;
            var potentialParentDirectoryPath = Path.TrimEndingDirectorySeparator(potentialParentDirectory.FullName) + Path.DirectorySeparatorChar;
            return directoryPath.StartsWith(potentialParentDirectoryPath);
        }
    }
}