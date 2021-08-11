using System.Linq;
using GitTreeVersion.Git;
using GitTreeVersion.Paths;

namespace GitTreeVersion.VersionStrategies
{
    public class CommitCountVersionStrategy : IVersionStrategy
    {
        public VersionComponent GetVersionComponent(AbsoluteDirectoryPath versionRootPath, AbsoluteDirectoryPath[] relevantPaths, string? range)
        {
            var gitDirectory = new GitDirectory(versionRootPath);
            var commits = gitDirectory.GitCommits(range, relevantPaths.Select(p => p.ToString()).ToArray());
            var patch = commits.Length;

            return new VersionComponent(patch, null);
        }
    }
}