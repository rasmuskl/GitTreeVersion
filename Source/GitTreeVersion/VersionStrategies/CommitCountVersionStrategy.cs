using System.Linq;
using GitTreeVersion.Paths;

namespace GitTreeVersion
{
    public class CommitCountVersionStrategy : IVersionStrategy
    {
        public VersionComponent GetVersionComponent(AbsoluteDirectoryPath versionRootPath, AbsoluteDirectoryPath[] relevantPaths, string? range)
        {
            var commits = Git.GitCommits(versionRootPath, range, relevantPaths.Select(p => p.ToString()).ToArray());
            var patch = commits.Length;

            return new VersionComponent(patch, null);
        }
    }
}