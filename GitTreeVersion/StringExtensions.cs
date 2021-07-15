using System.Linq;

namespace GitTreeVersion
{
    public static class StringExtensions
    {
        public static string[] SplitOutput(this string output)
        {
            return output
                .Trim()
                .Split('\n')
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToArray();
        }
    }
}