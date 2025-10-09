using System.Linq;
using FluentAssertions;
using GitTreeVersion.BuildEnvironments;
using GitTreeVersion.Context;
using GitTreeVersion.Git;
using NUnit.Framework;

namespace GitTreeVersion.Tests
{
    public class GitBranchTests : GitTestBase
    {
        [Test]
        public void CanOverrideMainBranchViaEnvironmentVariable()
        {
            var upstreamPath = CreateEmptyDirectory();
            var upstreamGit = new GitDirectory(upstreamPath);
            upstreamGit.RunGit("init", "--bare", "--initial-branch=main");

            var repositoryPath = CreateEmptyDirectory();
            var gitDirectory = new GitDirectory(repositoryPath);
            gitDirectory.RunGit("init", "--initial-branch=main");
            gitDirectory.RunGit("remote", "add", "upstream", upstreamPath.ToString());

            CommitNewFile(repositoryPath);
            gitDirectory.RunGit("push", "-u", "upstream", "main");
            gitDirectory.RunGit("fetch");

            var environmentVariables = new System.Collections.Generic.Dictionary<string, string?>
            {
                { "GTV_MAIN_BRANCH", "remotes/upstream/main" }
            };
            var environmentAccessor = new DictionaryEnvironmentAccessor(environmentVariables);
            var buildEnvironmentDetector = new BuildEnvironmentDetector(environmentAccessor);

            var versionGraph = ContextResolver.GetVersionGraph(repositoryPath, buildEnvironmentDetector);

            versionGraph.MainBranch.Should().NotBeNull();
            versionGraph.MainBranch!.Name.Should().Be("remotes/upstream/main");
        }

        [Test]
        public void ThrowsErrorWhenOverriddenMainBranchDoesNotExist()
        {
            var repositoryPath = CreateGitRepository();
            CommitNewFile(repositoryPath);

            var environmentVariables = new System.Collections.Generic.Dictionary<string, string?>
            {
                { "GTV_MAIN_BRANCH", "remotes/nonexistent/main" }
            };
            var environmentAccessor = new DictionaryEnvironmentAccessor(environmentVariables);
            var buildEnvironmentDetector = new BuildEnvironmentDetector(environmentAccessor);

            var exception = Assert.Throws<UserException>(() =>
                ContextResolver.GetVersionGraph(repositoryPath, buildEnvironmentDetector));

            exception.Should().NotBeNull();
            exception!.Message.Should().Contain("remotes/nonexistent/main");
            exception.Message.Should().Contain("not found");
        }
    }
}
