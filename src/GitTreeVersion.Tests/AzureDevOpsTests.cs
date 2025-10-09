using System.Linq;
using FluentAssertions;
using GitTreeVersion.BuildEnvironments;
using GitTreeVersion.Context;
using GitTreeVersion.Git;
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

            var versionGraph = ContextResolver.GetVersionGraph(repositoryPath, buildEnvironmentDetector);
            var version = new VersionCalculator().GetVersion(versionGraph, versionGraph.PrimaryVersionRootPath);

            version.Should().Be(new SemVersion(0, 0, 2, "PullRequest.42.1"));
        }

        [Test]
        public void SymbolicRemoteHeadShouldNotCauseMultipleMainBranches()
        {
            var originPath = CreateEmptyDirectory();
            var originGit = new GitDirectory(originPath);
            originGit.RunGit("init", "--bare", "--initial-branch=master");

            var repositoryPath = CreateEmptyDirectory();
            var gitDirectory = new GitDirectory(repositoryPath);
            gitDirectory.RunGit("init", "--initial-branch=master");
            gitDirectory.RunGit("remote", "add", "origin", originPath.ToString());

            CommitNewFile(repositoryPath);
            gitDirectory.RunGit("push", "-u", "origin", "master");
            gitDirectory.RunGit("fetch");

            var commitSha = gitDirectory.RunGit("rev-parse", "HEAD").Trim();
            gitDirectory.RunGit("checkout", commitSha);
            gitDirectory.RunGit("branch", "-D", "master");

            var branchOutput = gitDirectory.RunGit("branch", "--all");
            branchOutput.Should().NotContain("* master");
            branchOutput.Should().Contain("remotes/origin/HEAD");
            branchOutput.Should().Contain("remotes/origin/master");

            var (currentBranch, mainBranch) = gitDirectory.GitCurrentBranch();

            currentBranch.Should().NotBeNull();
            currentBranch!.IsDetached.Should().BeTrue();
            mainBranch.Should().NotBeNull();
            mainBranch!.Name.Should().Be("remotes/origin/master");
        }
    }
}