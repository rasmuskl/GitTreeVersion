using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace GitTreeVersion.Paths
{
    public class AbsoluteDirectoryPath
    {
        private static readonly char[] PathSeparators = { '/', '\\' };

        public AbsoluteDirectoryPath(string path)
        {
            Debug.Assert(Path.IsPathRooted(path));
            FullName = Path.TrimEndingDirectorySeparator(path);
        }

        public AbsoluteDirectoryPath Parent
        {
            get
            {
                var parentDirectoryPath = Path.GetDirectoryName(FullName);

                if (parentDirectoryPath == null)
                {
                    return this;
                }

                return new AbsoluteDirectoryPath(parentDirectoryPath);
            }
        }

        public bool IsAtRoot => Path.GetDirectoryName(FullName) == null;
        public int PathLength => FullName.Length;
        public string Name => Path.GetFileName(FullName);
        public bool Exists => Directory.Exists(FullName);
        public string FullName { get; }

        public AbsoluteFilePath CombineToFile(params string[] relativePaths)
        {
            var paths = new[] { FullName }
                .Concat(relativePaths
                    .SelectMany(p => p.Split(PathSeparators, StringSplitOptions.RemoveEmptyEntries)))
                .ToArray();

            var filePath = Path.GetFullPath(Path.Combine(paths));
            return new AbsoluteFilePath(filePath);
        }

        public AbsoluteDirectoryPath CombineToDirectory(params string[] relativePaths)
        {
            var paths = new[] { FullName }
                .Concat(relativePaths
                    .SelectMany(p => p.Split(PathSeparators, StringSplitOptions.RemoveEmptyEntries)))
                .ToArray();

            var filePath = Path.GetFullPath(Path.Combine(paths));
            return new AbsoluteDirectoryPath(filePath);
        }

        public override string ToString()
        {
            return FullName;
        }

        public bool Equals(AbsoluteDirectoryPath other)
        {
            return FullName == other.FullName;
        }

        public override bool Equals(object? obj)
        {
            return obj is AbsoluteDirectoryPath other && Equals(other);
        }

        public override int GetHashCode()
        {
            return FullName.GetHashCode();
        }

        public static bool operator ==(AbsoluteDirectoryPath left, AbsoluteDirectoryPath right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(AbsoluteDirectoryPath left, AbsoluteDirectoryPath right)
        {
            return !left.Equals(right);
        }

        public bool IsInSubPathOf(AbsoluteDirectoryPath parentPath)
        {
            var directoryPath = Path.TrimEndingDirectorySeparator(new DirectoryInfo(FullName).FullName) + Path.DirectorySeparatorChar;
            var potentialParentDirectoryPath = Path.TrimEndingDirectorySeparator(new DirectoryInfo(parentPath.ToString()).FullName) + Path.DirectorySeparatorChar;
            return directoryPath.StartsWith(potentialParentDirectoryPath);
        }

        public static AbsoluteFilePath? FindFileAbove(AbsoluteDirectoryPath directory, string fileName)
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

        public static AbsoluteDirectoryPath? FindDirectoryAboveContaining(AbsoluteDirectoryPath directory, string directoryName)
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