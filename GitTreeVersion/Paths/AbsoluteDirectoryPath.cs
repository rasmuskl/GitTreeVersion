using System.Diagnostics;
using System.IO;

namespace GitTreeVersion.Paths
{
    public readonly struct AbsoluteDirectoryPath
    {
        private readonly string _path;

        public AbsoluteDirectoryPath(string path)
        {
            Debug.Assert(Path.IsPathRooted(path));
            _path = path;
        }

        public AbsoluteDirectoryPath Parent
        {
            get
            {
                var parentDirectoryPath = Path.GetDirectoryName(_path);

                if (parentDirectoryPath == null)
                {
                    return this;
                }

                return new AbsoluteDirectoryPath(parentDirectoryPath);
            }
        }

        public bool IsAtRoot => Path.GetDirectoryName(_path) == null;
        public int PathLength => _path.Length;
        public string Name => Path.GetFileName(_path);

        public override string ToString()
        {
            return _path;
        }

        public bool Equals(AbsoluteDirectoryPath other)
        {
            return _path == other._path;
        }

        public override bool Equals(object? obj)
        {
            return obj is AbsoluteDirectoryPath other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _path.GetHashCode();
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
            var directoryPath = Path.TrimEndingDirectorySeparator(new DirectoryInfo(_path).FullName) + Path.DirectorySeparatorChar;
            var potentialParentDirectoryPath = Path.TrimEndingDirectorySeparator(new DirectoryInfo(parentPath.ToString()).FullName) + Path.DirectorySeparatorChar;
            return directoryPath.StartsWith(potentialParentDirectoryPath);
        }
    }
}