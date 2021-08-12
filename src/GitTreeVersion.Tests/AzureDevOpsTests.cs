using FluentAssertions;
using GitTreeVersion.BuildEnvironments;
using GitTreeVersion.Context;
using NUnit.Framework;
using Semver;

namespace GitTreeVersion.Tests
{
    public class AzureDevOpsTests : GitTestBase
    {
        [Test]
        public void AzureDevOpsDetachedHeadState()
        {
            var repositoryPath = CreateGitRepository();

            CommitNewFile(repositoryPath);

            var branchName = CreateBranch(repositoryPath);

            CommitNewFile(repositoryPath);

            var environmentAccessor = SimulateAzureDevOpsPullRequest(repositoryPath, branchName, 42);
            var buildEnvironmentDetector = new BuildEnvironmentDetector(environmentAccessor);

            var version = new VersionCalculator().GetVersion(ContextResolver.GetFileGraph(repositoryPath, buildEnvironmentDetector));

            version.Should().Be(new SemVersion(0, 0, 2, "PullRequest.42.1"));
        }
    }
}