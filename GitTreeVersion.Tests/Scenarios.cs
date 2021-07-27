using System;
using System.IO;
using System.Text.Json;
using FluentAssertions;
using GitTreeVersion.Context;
using GitTreeVersion.Paths;
using LibGit2Sharp;
using Xunit;
using JsonSerializer = System.Text.Json.JsonSerializer;
using Version = System.Version;

namespace GitTreeVersion.Tests
{
    public class Scenarios
    {
        [Fact]
        public void NonRepository()
        {
            var repositoryPath = CreateEmptyDirectory();

            Action action = () => { new VersionCalculator().GetVersion(ContextResolver.GetFileGraph(repositoryPath)); };

            action.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void EmptyRepository()
        {
            var repositoryPath = CreateGitRepository();
            
            var version = new VersionCalculator().GetVersion(ContextResolver.GetFileGraph(repositoryPath));

            version.Should().Be(new Version(0, 0, 0));
        }

        [Fact]
        public void SingleCommit()
        {
            var repositoryPath = CreateGitRepository();

            CommitNewFile(repositoryPath);

            var version = new VersionCalculator().GetVersion(ContextResolver.GetFileGraph(repositoryPath));

            version.Should().Be(new Version(0, 0, 1));
        }

        [Fact]
        public void TwoCommits()
        {
            var repositoryPath = CreateGitRepository();

            CommitNewFile(repositoryPath);
            CommitNewFile(repositoryPath);

            var version = new VersionCalculator().GetVersion(ContextResolver.GetFileGraph(repositoryPath));

            version.Should().Be(new Version(0, 0, 2));
        }

        [Fact]
        public void SingleMergeCommitNoFastForward()
        {
            var repositoryPath = CreateGitRepository();

            CommitNewFile(repositoryPath);

            var branchName = CreateBranch(repositoryPath);

            CommitNewFile(repositoryPath);

            MergeBranchToMaster(repositoryPath, branchName);

            var version = new VersionCalculator().GetVersion(ContextResolver.GetFileGraph(repositoryPath));

            version.Should().Be(new Version(0, 1, 0));
        }

        [Fact]
        public void SingleMergeCommitFastForward()
        {
            var repositoryPath = CreateGitRepository();

            CommitNewFile(repositoryPath);

            var branchName = CreateBranch(repositoryPath);

            CommitNewFile(repositoryPath);

            MergeBranchToMaster(repositoryPath, branchName, true);

            var version = new VersionCalculator().GetVersion(ContextResolver.GetFileGraph(repositoryPath));

            version.Should().Be(new Version(0, 0, 2));
        }

        // [Fact]
        // public void MajorVersionConfigured()
        // {
        //     var repositoryPath = CreateGitRepository();
        //
        //     CommitVersionConfig(repositoryPath, new VersionConfig {Major = "1"});
        //
        //     var version = new VersionCalculator().GetVersion(ContextResolver.GetRepositoryContext(repositoryPath));
        //
        //     version.Should().Be(new Version(1, 0, 1));
        // }

        [Fact]
        public void AzureDevOpsDetachedHeadState()
        {
            var repositoryPath = CreateGitRepository();

            CommitNewFile(repositoryPath);

            var branchName = CreateBranch(repositoryPath);

            CommitNewFile(repositoryPath);

            var currentCommit = Git.RunGit(repositoryPath, "rev-parse", branchName).Trim();

            // Source: https://stackoverflow.com/a/42634501
            Git.RunGit(repositoryPath, "update-ref", "refs/pull/42/pull", currentCommit);
            Git.RunGit(repositoryPath, "checkout", "master");
            Git.RunGit(repositoryPath, "merge", "--no-ff", "refs/pull/42/pull");
            Git.RunGit(repositoryPath, "update-ref", "refs/pull/42/merge", "HEAD");
            Git.RunGit(repositoryPath, "checkout", "refs/pull/42/merge");

            // git status at this point gives: 
            //
            // (local)
            // HEAD detached at refs/pull/42/merge
            // nothing to commit, working tree clean
            //
            // (Azure DevOps)
            // HEAD detached at pull/119/merge
            // nothing to commit, working tree clean

            var version = new VersionCalculator().GetVersion(ContextResolver.GetFileGraph(repositoryPath));

            // TODO: Determine correct version here
            version.Should().Be(new Version(0, 1, 0));
        }

        private void CommitVersionConfig(AbsoluteDirectoryPath repositoryPath, VersionConfig versionConfig)
        {
            File.WriteAllText(Path.Combine(repositoryPath.ToString(), ContextResolver.VersionConfigFileName), JsonSerializer.Serialize(versionConfig, new JsonSerializerOptions { WriteIndented = true }));
            Git.RunGit(repositoryPath, "add", "--all");
            Git.RunGit(repositoryPath, "commit", "--all", "--message", Guid.NewGuid().ToString());
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

        private static void CommitNewFile(AbsoluteDirectoryPath repositoryPath)
        {
            var fileName = Path.Combine(repositoryPath.ToString(), Guid.NewGuid().ToString());
            File.WriteAllText(fileName, Guid.NewGuid().ToString());

            using var repository = new Repository(repositoryPath.ToString());
            
            repository.Index.Add(Path.GetRelativePath(repositoryPath.ToString(), fileName));
            repository.Index.Write();
            
            var signature = new Signature(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), DateTimeOffset.UtcNow);
            repository.Commit(Guid.NewGuid().ToString(), signature, signature);

            // Git.RunGit(repositoryPath, "add", "--all");
            // Git.RunGit(repositoryPath, "commit", "--all", "--message", Guid.NewGuid().ToString());
        }

        private static AbsoluteDirectoryPath CreateGitRepository()
        {
            var repositoryPath = CreateEmptyDirectory();
            // Git.RunGit(repositoryPath, "init");
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