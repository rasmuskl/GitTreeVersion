using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using FluentAssertions;
using GitTreeVersion.BuildEnvironments;
using GitTreeVersion.Context;
using GitTreeVersion.Paths;
using LibGit2Sharp;
using NUnit.Framework;
using Semver;

namespace GitTreeVersion.Tests
{
    public class Scenarios
    {
        [Test]
        public void NonRepository()
        {
            var repositoryPath = CreateEmptyDirectory();

            Action action = () => { new VersionCalculator().GetVersion(ContextResolver.GetFileGraph(repositoryPath)); };

            action.Should().Throw<InvalidOperationException>();
        }

        [Test]
        public void EmptyRepository()
        {
            var repositoryPath = CreateGitRepository();

            var version = new VersionCalculator().GetVersion(ContextResolver.GetFileGraph(repositoryPath));

            version.Should().Be(new SemVersion(0));
        }

        [Test]
        public void SingleCommit()
        {
            var repositoryPath = CreateGitRepository();

            CommitNewFile(repositoryPath);

            var version = new VersionCalculator().GetVersion(ContextResolver.GetFileGraph(repositoryPath));

            version.Should().Be(new SemVersion(0, 0, 1));
        }

        [Test]
        public void TwoCommits()
        {
            var repositoryPath = CreateGitRepository();

            CommitNewFile(repositoryPath);
            CommitNewFile(repositoryPath);

            var version = new VersionCalculator().GetVersion(ContextResolver.GetFileGraph(repositoryPath));

            version.Should().Be(new SemVersion(0, 0, 2));
        }

        [Test]
        public void SingleMergeCommitNoFastForward()
        {
            var repositoryPath = CreateGitRepository();

            CommitNewFile(repositoryPath);

            var branchName = CreateBranch(repositoryPath);

            CommitNewFile(repositoryPath);

            MergeBranchToMaster(repositoryPath, branchName);

            var version = new VersionCalculator().GetVersion(ContextResolver.GetFileGraph(repositoryPath));

            version.Should().Be(new SemVersion(0, 0, 2));
        }

        [Test]
        public void SingleMergeCommitFastForward()
        {
            var repositoryPath = CreateGitRepository();

            CommitNewFile(repositoryPath);

            var branchName = CreateBranch(repositoryPath);

            CommitNewFile(repositoryPath);

            MergeBranchToMaster(repositoryPath, branchName, true);

            var version = new VersionCalculator().GetVersion(ContextResolver.GetFileGraph(repositoryPath));

            version.Should().Be(new SemVersion(0, 0, 2));
        }

        [Test]
        public void MinorVersion()
        {
            var repositoryPath = CreateGitRepository();

            var bumpFile = new Bumper().Bump(repositoryPath, VersionType.Minor);
            CommitFile(repositoryPath, bumpFile);

            var version = new VersionCalculator().GetVersion(ContextResolver.GetFileGraph(repositoryPath));

            version.Should().Be(new SemVersion(0, 1));
        }

        [Test]
        public void MajorVersion()
        {
            var repositoryPath = CreateGitRepository();

            var bumpFile = new Bumper().Bump(repositoryPath, VersionType.Major);
            CommitFile(repositoryPath, bumpFile);

            var version = new VersionCalculator().GetVersion(ContextResolver.GetFileGraph(repositoryPath));

            version.Should().Be(new SemVersion(1));
        }

        [Test]
        public void MinorThenMajorVersion()
        {
            var repositoryPath = CreateGitRepository();

            var minorBumpFile = new Bumper().Bump(repositoryPath, VersionType.Minor);
            CommitFile(repositoryPath, minorBumpFile);

            var majorBumpFile = new Bumper().Bump(repositoryPath, VersionType.Major);
            CommitFile(repositoryPath, majorBumpFile);

            var version = new VersionCalculator().GetVersion(ContextResolver.GetFileGraph(repositoryPath));

            version.Should().Be(new SemVersion(1));
        }

        [Test]
        public void MajorThenMinorVersion()
        {
            var repositoryPath = CreateGitRepository();

            var majorBumpFile = new Bumper().Bump(repositoryPath, VersionType.Major);
            CommitFile(repositoryPath, majorBumpFile);

            var minorBumpFile = new Bumper().Bump(repositoryPath, VersionType.Minor);
            CommitFile(repositoryPath, minorBumpFile);

            var version = new VersionCalculator().GetVersion(ContextResolver.GetFileGraph(repositoryPath));

            version.Should().Be(new SemVersion(1, 1));
        }

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

        private static IEnvironmentAccessor SimulateAzureDevOpsPullRequest(AbsoluteDirectoryPath repositoryPath, string branchName, int pullRequestId)
        {
            var environmentVariables = new Dictionary<string, string?>();

            var currentCommit = Git.RunGit(repositoryPath, "rev-parse", branchName).Trim();

            var pullRef = $"pull/{pullRequestId}/pull";
            var mergeRef = $"pull/{pullRequestId}/merge";
            environmentVariables.Add("TF_BUILD", "True");
            environmentVariables.Add("SYSTEM_PULLREQUEST_PULLREQUESTID", pullRequestId.ToString());

            // Source: https://stackoverflow.com/a/42634501
            Git.RunGit(repositoryPath, "update-ref", pullRef, currentCommit);
            Git.RunGit(repositoryPath, "checkout", "master");
            Git.RunGit(repositoryPath, "merge", "--no-ff", pullRef);
            Git.RunGit(repositoryPath, "update-ref", mergeRef, "HEAD");
            Git.RunGit(repositoryPath, "checkout", mergeRef);
            
            // git status at this point gives: 
            //
            // (local)
            // HEAD detached at pull/42/merge
            // nothing to commit, working tree clean
            //
            // (Azure Pipelines)
            // HEAD detached at pull/42/merge
            // nothing to commit, working tree clean

            return new DictionaryEnvironmentAccessor(environmentVariables);
        }

        [Test]
        public void SemanticVersion_SingleCommit()
        {
            var repositoryPath = CreateGitRepository();

            CommitVersionConfig(repositoryPath, new VersionConfig {Mode = VersionMode.SemanticVersion});

            var version = new VersionCalculator().GetVersion(ContextResolver.GetFileGraph(repositoryPath));

            version.Should().Be(new SemVersion(0, 0, 1));
        }

        [Test]
        public void CalendarVersioning_SingleCommit()
        {
            var repositoryPath = CreateGitRepository();

            var commitTime = new DateTimeOffset(2021, 1, 1, 10, 42, 5, TimeSpan.Zero);
            var filePath = WriteVersionConfig(repositoryPath, new VersionConfig {Mode = VersionMode.CalendarVersion});
            CommitFile(repositoryPath, filePath, commitTime);

            var version = new VersionCalculator().GetVersion(ContextResolver.GetFileGraph(repositoryPath));

            version.Should().Be(new SemVersion(2021, 101));
        }

        [Test]
        public void CalendarVersioning_TwoCommit()
        {
            var repositoryPath = CreateGitRepository();

            var commitTime = new DateTimeOffset(2021, 1, 1, 10, 42, 5, TimeSpan.Zero);
            var filePath = WriteVersionConfig(repositoryPath, new VersionConfig {Mode = VersionMode.CalendarVersion});
            CommitFile(repositoryPath, filePath, commitTime);

            CommitNewFile(repositoryPath, commitTime.Add(TimeSpan.FromSeconds(5)));

            var version = new VersionCalculator().GetVersion(ContextResolver.GetFileGraph(repositoryPath));

            version.Should().Be(new SemVersion(2021, 101, 1));
        }
        
        private void CommitVersionConfig(AbsoluteDirectoryPath repositoryPath, VersionConfig versionConfig)
        {
            WriteVersionConfig(repositoryPath, versionConfig);
            Git.RunGit(repositoryPath, "add", "--all");
            Git.RunGit(repositoryPath, "commit", "--all", "--message", Guid.NewGuid().ToString());
        }

        private AbsoluteFilePath WriteVersionConfig(AbsoluteDirectoryPath repositoryPath, VersionConfig versionConfig)
        {
            var filePath = Path.Combine(repositoryPath.ToString(), ContextResolver.VersionConfigFileName);
            File.WriteAllText(filePath, JsonSerializer.Serialize(versionConfig, JsonOptions.DefaultOptions));

            return new AbsoluteFilePath(filePath);
        }

        private static void MergeBranchToMaster(AbsoluteDirectoryPath repositoryPath, string branchName, bool fastForward = false)
        {
            Git.RunGit(repositoryPath, "checkout", "master");
            Git.RunGit(repositoryPath, "merge", fastForward ? "--ff" : "--no-ff", branchName);
        }

        private static string CreateBranch(AbsoluteDirectoryPath repositoryPath)
        {
            var branchName = Guid.NewGuid().ToString();
            Git.RunGit(repositoryPath, "checkout", "-b", branchName);
            return branchName;
        }

        private static void CommitNewFile(AbsoluteDirectoryPath repositoryPath, DateTimeOffset? commitTime = null)
        {
            var fileName = Path.Combine(repositoryPath.ToString(), Guid.NewGuid().ToString());
            File.WriteAllText(fileName, Guid.NewGuid().ToString());

            using var repository = new Repository(repositoryPath.ToString());

            repository.Index.Add(Path.GetRelativePath(repositoryPath.ToString(), fileName));
            repository.Index.Write();

            var signature = new Signature(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), commitTime.GetValueOrDefault(DateTimeOffset.UtcNow));
            repository.Commit(Guid.NewGuid().ToString(), signature, signature);
        }

        private static void CommitFile(AbsoluteDirectoryPath repositoryPath, AbsoluteFilePath filePath, DateTimeOffset? commitTime = null)
        {
            using var repository = new Repository(repositoryPath.ToString());

            repository.Index.Add(Path.GetRelativePath(repositoryPath.ToString(), filePath.ToString()));
            repository.Index.Write();

            var signature = new Signature(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), commitTime.GetValueOrDefault(DateTimeOffset.UtcNow));
            repository.Commit(Guid.NewGuid().ToString(), signature, signature);
        }

        private static AbsoluteDirectoryPath CreateGitRepository()
        {
            var repositoryPath = CreateEmptyDirectory();
            Repository.Init(repositoryPath.ToString());
            return repositoryPath;
        }

        private static AbsoluteDirectoryPath CreateEmptyDirectory()
        {
            var repositoryPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(repositoryPath);
            return new AbsoluteDirectoryPath(repositoryPath);
        }
    }
}