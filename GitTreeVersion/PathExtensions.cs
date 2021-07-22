using System.IO;

namespace GitTreeVersion
{
    public static class PathExtensions
    {
        public static bool IsSubPathOf(this string path, string otherPath)
        {
            return !Path.GetRelativePath(otherPath, path).StartsWith("..");
        }
    }
}