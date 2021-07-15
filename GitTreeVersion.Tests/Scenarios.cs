using System;
using System.IO;
using System.Text.Json;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace GitTreeVersion.Tests
{
    public class Scenarios
    {
        [Fact]
        public void NonRepository()
        {
            var repositoryPath = CreateEmptyDirectory();

            var version = new VersionCalculator().GetVersion(repositoryPath);

            version.Should().Be(new Version(0, 0, 0));
        }

        [Fact]
        public void EmptyRepository()
        {
            var repositoryPath = CreateGitRepository();

            var version = new VersionCalculator().GetVersion(repositoryPath);

            version.Should().Be(new Version(0, 0, 0));
        }

        [Fact]
        public void SingleCommit()
        {
            var repositoryPath = CreateGitRepository();

            CommitNewFile(repositoryPath);

            var version = new VersionCalculator().GetVersion(repositoryPath);

            version.Should().Be(new Version(0, 0, 1));
        }

        [Fact]
        public void TwoCommits()
        {
            var repositoryPath = CreateGitRepository();

            CommitNewFile(repositoryPath);
            CommitNewFile(repositoryPath);

            var version = new VersionCalculator().GetVersion(repositoryPath);

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

            var version = new VersionCalculator().GetVersion(repositoryPath);

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
            
            var version = new VersionCalculator().GetVersion(repositoryPath);

            version.Should().Be(new Version(0, 0, 2));
        }
        
        [Fact]
        public void MajorVersionConfigured()
        {
            var repositoryPath = CreateGitRepository();

            CommitVersionConfig(repositoryPath, new VersionConfig { Major = "1" });

            var version = new VersionCalculator().GetVersion(repositoryPath);

            version.Should().Be(new Version(1, 0, 1));
        }

        private void CommitVersionConfig(string repositoryPath, VersionConfig versionConfig)
        {
            File.WriteAllText(Path.Combine(repositoryPath, VersionConfigManager.VersionConfigFileName), JsonSerializer.Serialize(versionConfig, new JsonSerializerOptions { WriteIndented = true }));
            Git.RunGit(repositoryPath, "add", "--all");
            Git.RunGit(repositoryPath, "commit", "--all", "--message", Guid.NewGuid().ToString());
        }

        private static void MergeBranchToMaster(string repositoryPath, string branchName, bool fastForward = false)
        {
            Git.RunGit(repositoryPath, "checkout", "master");
            Git.RunGit(repositoryPath, "merge", fastForward ? "--ff" : "--no-ff", branchName);
        }

        private static string CreateBranch(string repositoryPath)
        {
            var branchName = Guid.NewGuid().ToString();
            Git.RunGit(repositoryPath, "checkout", "-b", branchName);
            return branchName;
        }

        private static void CommitNewFile(string repositoryPath)
        {
            File.WriteAllText(Path.Combine(repositoryPath, Guid.NewGuid().ToString()), Guid.NewGuid().ToString());
            Git.RunGit(repositoryPath, "add", "--all");
            Git.RunGit(repositoryPath, "commit", "--all", "--message", Guid.NewGuid().ToString());
        }

        private static string CreateGitRepository()
        {
            var repositoryPath = CreateEmptyDirectory();
            Git.RunGit(repositoryPath, "init");
            return repositoryPath;
        }

        private static string CreateEmptyDirectory()
        {
            var repositoryPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(repositoryPath);
            return repositoryPath;
        }
    }
}