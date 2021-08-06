using System.Linq;
using GitTreeVersion.Paths;

namespace GitTreeVersion.BuildEnvironments
{
    public class AzureDevOpsBuildEnvironment : IBuildEnvironment
    {
        private readonly IEnvironmentAccessor _environmentAccessor;

        public AzureDevOpsBuildEnvironment(IEnvironmentAccessor environmentAccessor)
        {
            _environmentAccessor = environmentAccessor;
        }

        public string? GetPrerelease(AbsoluteDirectoryPath versionRootPath, AbsoluteDirectoryPath[] relevantPaths)
        {
            var pullRequestId = _environmentAccessor.GetEnvironmentVariable("SYSTEM_PULLREQUEST_PULLREQUESTID");

            if (string.IsNullOrWhiteSpace(pullRequestId))
            {
                return null;
            }

            var branchCommits = Git.GitCommits(versionRootPath, "HEAD^1..HEAD^2", relevantPaths.Select(p => p.FullName).ToArray());
            return $"PullRequest.{pullRequestId}.{branchCommits.Length}";
        }
    }
}