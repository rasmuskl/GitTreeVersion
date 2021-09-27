using FluentAssertions;
using GitTreeVersion.Context;
using NUnit.Framework;
using Semver;

namespace GitTreeVersion.Tests
{
    public class ProjectReferenceScenarios : GitTestBase
    {
        [Test]
        public void FollowingReferencesForChanges()
        {
            var repositoryPath = CreateGitRepository();

            var project1Path = repositoryPath.CombineToDirectory("project1").CombineToFile("project1.csproj");
            var project2Path = repositoryPath.CombineToDirectory("project2").CombineToFile("project2.csproj");

            CommitVersionConfig(project2Path.Parent, new VersionConfig());
            CommitCsprojFile(repositoryPath, project1Path);
            CommitCsprojFile(repositoryPath, project2Path, new[] { project1Path });

            var versionGraph = ContextResolver.GetVersionGraph(repositoryPath);

            var version = new VersionCalculator().GetVersion(versionGraph, project2Path.Parent);
            version.Should().Be(new SemVersion(0, 0, 3));
        }

        [Test]
        public void FollowingReferencesForChanges2()
        {
            var repositoryPath = CreateGitRepository();

            var project1Path = repositoryPath.CombineToDirectory("projects").CombineToFile("project1.csproj");
            var project2Path = repositoryPath.CombineToDirectory("projects").CombineToFile("project2.csproj");
            var project3Path = repositoryPath.CombineToDirectory("project3").CombineToFile("project3.csproj");

            CommitVersionConfig(project3Path.Parent, new VersionConfig());
            CommitCsprojFile(repositoryPath, project1Path);
            CommitCsprojFile(repositoryPath, project2Path);
            CommitCsprojFile(repositoryPath, project3Path, new[] { project1Path });

            var versionGraph = ContextResolver.GetVersionGraph(repositoryPath);

            var relevantDeployablesForVersionRoot = versionGraph.GetRelevantDeployablesForVersionRoot(project3Path.Parent);
            relevantDeployablesForVersionRoot.Should().BeEquivalentTo(project1Path, project3Path);
        }
    }
}