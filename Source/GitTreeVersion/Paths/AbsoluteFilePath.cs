using System;
using System.Diagnostics;
using System.IO;

namespace GitTreeVersion.Paths
{
    public readonly struct AbsoluteFilePath
    {
        public AbsoluteFilePath(string path)
        {
            Debug.Assert(Path.IsPathRooted(path));
            FullName = path;
        }

        public string FileName => Path.GetFileName(FullName);

        public AbsoluteDirectoryPath Parent
        {
            get
            {
                var parentDirectoryPath = Path.GetDirectoryName(FullName);

                if (parentDirectoryPath == null)
                {
                    throw new InvalidOperationException($"No parent directory found for file path: {FullName}");
                }

                return new AbsoluteDirectoryPath(parentDirectoryPath);
            }
        }

        public bool Exists => File.Exists(FullName);
        public string Extension => Path.GetExtension(FullName);
        public string FullName { get; }

        public override string ToString()
        {
            return FullName;
        }

        public bool Equals(AbsoluteFilePath other)
        {
            return FullName == other.FullName;
        }

        public override bool Equals(object? obj)
        {
            return obj is AbsoluteFilePath other && Equals(other);
        }

        public override int GetHashCode()
        {
            return FullName.GetHashCode();
        }

        public static bool operator ==(AbsoluteFilePath left, AbsoluteFilePath right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(AbsoluteFilePath left, AbsoluteFilePath right)
        {
            return !left.Equals(right);
        }

        public bool IsInSubPathOf(AbsoluteDirectoryPath path)
        {
            var directoryPath = new DirectoryInfo(Parent.ToString()).FullName + Path.DirectorySeparatorChar;
            var potentialParentDirectoryPath = Path.TrimEndingDirectorySeparator(new DirectoryInfo(path.ToString()).FullName) + Path.DirectorySeparatorChar;
            return directoryPath.StartsWith(potentialParentDirectoryPath);
        }
    }
}