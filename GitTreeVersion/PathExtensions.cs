using System;
using System.IO;

namespace GitTreeVersion
{
    public static class PathExtensions
    {
        public static bool IsSubPathOf(this DirectoryInfo directory1, DirectoryInfo directory2)
        {
            return (directory1.FullName + Path.DirectorySeparatorChar).StartsWith(directory2.FullName + Path.DirectorySeparatorChar);
        }
    }
}