using System.Linq;
using GitTreeVersion.Git;

namespace GitTreeVersion.VersionStrategies
{
    public class CommitCountVersionStrategy : IVersionStrategy
    {
        public VersionComponent GetVersionComponent(VersionComponentContext context, string? range)
        {
            var gitDirectory = new GitDirectory(context.VersionRootPath);
            var commits = gitDirectory.GitCommits(range, context.RelevantPaths.Select(p => p.ToString()).ToArray());

            if (context.CurrentBranch is null || context.MainBranch is null || context.CurrentBranch == context.MainBranch)
            {
                return new VersionComponent(commits.Length, null);
            }

            string[] otherCommits = gitDirectory.GitCommits($"{context.MainBranch.Name}..{context.CurrentBranch.Name}", context.RelevantPaths.Select(p => p.FullName).ToArray());

            if (!otherCommits.Any())
            {
                return new VersionComponent(commits.Length, null);
            }

            var mainBranchCommitCount = commits.Except(otherCommits).Count();
            return new VersionComponent(mainBranchCommitCount + 1, null);
        }
    }
}