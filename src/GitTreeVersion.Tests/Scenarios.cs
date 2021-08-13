using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using GitTreeVersion.Context;
using GitTreeVersion.Deployables.DotNet;
using GitTreeVersion.Git;
using GitTreeVersion.Paths;
using NUnit.Framework;
using Semver;

namespace GitTreeVersion.Tests
{
    public class Scenarios : GitTestBase
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
        public void MajorThenMinorThenChangeMajor()
        {
            var repositoryPath = CreateGitRepository();

            var majorBumpFile = new Bumper().Bump(repositoryPath, VersionType.Major);
            CommitFile(repositoryPath, majorBumpFile);

            var minorBumpFile = new Bumper().Bump(repositoryPath, VersionType.Minor);
            CommitFile(repositoryPath, minorBumpFile);

            File.WriteAllText(majorBumpFile.ToString(), "change");
            CommitFile(repositoryPath, majorBumpFile);

            var version = new VersionCalculator().GetVersion(ContextResolver.GetFileGraph(repositoryPath));

            version.Should().Be(new SemVersion(1, 1, 1));
        }

        [Test]
        public void MajorThenMinorThenMoveMajor()
        {
            var repositoryPath = CreateGitRepository();

            var majorBumpFile = new Bumper().Bump(repositoryPath, VersionType.Major);
            CommitFile(repositoryPath, majorBumpFile);

            var minorBumpFile = new Bumper().Bump(repositoryPath, VersionType.Minor);
            CommitFile(repositoryPath, minorBumpFile);

            MoveAndCommitFile(repositoryPath, majorBumpFile, new AbsoluteFilePath(Path.Combine(majorBumpFile.Parent.ToString(), "new-name")));

            var version = new VersionCalculator().GetVersion(ContextResolver.GetFileGraph(repositoryPath));

            version.Should().Be(new SemVersion(1, 1, 1));
        }

        [Test]
        public void MajorThenMinorThenTCommitThenChangeMinor()
        {
            var repositoryPath = CreateGitRepository();

            var majorBumpFile = new Bumper().Bump(repositoryPath, VersionType.Major);
            CommitFile(repositoryPath, majorBumpFile);

            var minorBumpFile = new Bumper().Bump(repositoryPath, VersionType.Minor);
            CommitFile(repositoryPath, minorBumpFile);

            CommitNewFile(repositoryPath);

            File.WriteAllText(minorBumpFile.ToString(), "change");
            CommitFile(repositoryPath, minorBumpFile);

            var version = new VersionCalculator().GetVersion(ContextResolver.GetFileGraph(repositoryPath));

            version.Should().Be(new SemVersion(1, 1, 2));
        }

        [Test]
        [Ignore("Should work but does not yet - see comments inside")]
        public void MajorThenMinorThenTCommitThenMoveMinor()
        {
            var repositoryPath = CreateGitRepository();

            var majorBumpFile = new Bumper().Bump(repositoryPath, VersionType.Major);
            CommitFile(repositoryPath, majorBumpFile, commitMessage: "bump major version");

            var minorBumpFile = new Bumper().Bump(repositoryPath, VersionType.Minor);
            CommitFile(repositoryPath, minorBumpFile, commitMessage: "bump minor version");

            CommitNewFile(repositoryPath);

            MoveAndCommitFile(repositoryPath, minorBumpFile, new AbsoluteFilePath(Path.Combine(minorBumpFile.Parent.ToString(), "new-name")));

            // TODO: diff-filters are not working properly for this scenario
            // Since we get the moved file name and use it as a pathspec, it does not track back to initial bump commit unless --follow is added, but --follow only allows for exactly one pathspec
            // Potential solution - replace diff + log with reconstruction through: git log --full-history --first-parent --name-status --format=format:%H <major-commit>.. -- .version/minor/*

            var version = new VersionCalculator().GetVersion(ContextResolver.GetFileGraph(repositoryPath));

            version.Should().Be(new SemVersion(1, 1, 2));
        }

        [Test]
        public void MinorThenCommitThenChangeMinor()
        {
            var repositoryPath = CreateGitRepository();

            var minorBumpFile = new Bumper().Bump(repositoryPath, VersionType.Minor);
            CommitFile(repositoryPath, minorBumpFile);

            CommitNewFile(repositoryPath);

            File.WriteAllText(minorBumpFile.ToString(), "change");
            CommitFile(repositoryPath, minorBumpFile);

            var version = new VersionCalculator().GetVersion(ContextResolver.GetFileGraph(repositoryPath));

            version.Should().Be(new SemVersion(0, 1, 2));
        }

        [Test]
        public void MinorThenChangeThenMoveMinor()
        {
            var repositoryPath = CreateGitRepository();

            var minorBumpFile = new Bumper().Bump(repositoryPath, VersionType.Minor);
            CommitFile(repositoryPath, minorBumpFile);

            CommitNewFile(repositoryPath);

            MoveAndCommitFile(repositoryPath, minorBumpFile, new AbsoluteFilePath(Path.Combine(minorBumpFile.Parent.ToString(), "new-name")));

            var version = new VersionCalculator().GetVersion(ContextResolver.GetFileGraph(repositoryPath));

            version.Should().Be(new SemVersion(0, 1, 2));
        }

        [Test]
        public void NonDefaultBranch()
        {
            var repositoryPath = CreateGitRepository();

            CommitNewFile(repositoryPath);

            var branchName = CreateBranch(repositoryPath);

            CommitNewFile(repositoryPath);

            var version = new VersionCalculator().GetVersion(ContextResolver.GetFileGraph(repositoryPath));

            version.Should().Be(new SemVersion(0, 0, 2, $"{branchName}.1"));
        }

        [Test]
        public void NonDefaultSlashBranch()
        {
            var repositoryPath = CreateGitRepository();

            CommitNewFile(repositoryPath);

            CreateBranch(repositoryPath, "feature/12345");

            CommitNewFile(repositoryPath);

            var version = new VersionCalculator().GetVersion(ContextResolver.GetFileGraph(repositoryPath));

            version.Should().Be(new SemVersion(0, 0, 2, "feature-12345.1"));
        }

        [Test]
        public void MonotonicBranch()
        {
            var repositoryPath = CreateGitRepository();

            CommitNewFile(repositoryPath);

            var branchName = CreateBranch(repositoryPath);

            CommitNewFile(repositoryPath);
            CommitNewFile(repositoryPath);

            var branchVersion = new VersionCalculator().GetVersion(ContextResolver.GetFileGraph(repositoryPath));

            MergeBranchToMaster(repositoryPath, branchName);

            var mergedMasterVersion = new VersionCalculator().GetVersion(ContextResolver.GetFileGraph(repositoryPath));

            Console.WriteLine($"Branch: {branchVersion}");
            Console.WriteLine($"Merged: {mergedMasterVersion}");

            mergedMasterVersion.Should().BeGreaterOrEqualTo(branchVersion);
        }

        [Test]
        public void GitFileHistory()
        {
            var repositoryPath = CreateGitRepository();

            var versionConfigs = new[]
            {
                new VersionConfig { Mode = VersionMode.SemanticVersion, Version = "1.0.0" },
                new VersionConfig { Mode = VersionMode.SemanticVersion, Version = "1.1.0" },
                new VersionConfig { Mode = VersionMode.SemanticVersion, Version = "1.2.0" },
            };

            foreach (var versionConfig in versionConfigs)
            {
                CommitVersionConfig(repositoryPath, versionConfig);
            }

            var filePath = Path.Combine(repositoryPath.ToString(), ContextResolver.VersionConfigFileName);

            var gitDirectory = new GitDirectory(repositoryPath);
            var commitContents = gitDirectory.FileCommitHistory(filePath);

            commitContents.Length.Should().Be(versionConfigs.Length);

            var reversedVersionConfigs = versionConfigs.Reverse().ToArray();

            for (var i = 0; i < commitContents.Length; i++)
            {
                var serializedVersionConfig = reversedVersionConfigs[i];
                var deserializedVersionConfig = JsonSerializer.Deserialize<VersionConfig>(commitContents[i].Content, JsonOptions.DefaultOptions);

                deserializedVersionConfig.Should().NotBeNull();
                serializedVersionConfig.Mode.Should().Be(deserializedVersionConfig!.Mode);
                serializedVersionConfig.Version.Should().Be(deserializedVersionConfig.Version);
            }
        }

        [Test]
        public void SemanticVersion_SingleCommit()
        {
            var repositoryPath = CreateGitRepository();

            CommitVersionConfig(repositoryPath, new VersionConfig { Mode = VersionMode.SemanticVersion });

            var version = new VersionCalculator().GetVersion(ContextResolver.GetFileGraph(repositoryPath));

            version.Should().Be(new SemVersion(0, 0, 1));
        }

        [Test]
        public void CalendarVersioning_SingleCommit()
        {
            var repositoryPath = CreateGitRepository();

            var commitTime = new DateTimeOffset(2021, 1, 1, 10, 42, 5, TimeSpan.Zero);
            var filePath = WriteVersionConfig(repositoryPath, new VersionConfig { Mode = VersionMode.CalendarVersion });
            CommitFile(repositoryPath, filePath, commitTime);

            var version = new VersionCalculator().GetVersion(ContextResolver.GetFileGraph(repositoryPath));

            version.Should().Be(new SemVersion(20210101));
        }

        [Test]
        public void CalendarVersioning_TwoCommit()
        {
            var repositoryPath = CreateGitRepository();

            var commitTime = new DateTimeOffset(2021, 1, 1, 10, 42, 5, TimeSpan.Zero);
            var filePath = WriteVersionConfig(repositoryPath, new VersionConfig { Mode = VersionMode.CalendarVersion });
            CommitFile(repositoryPath, filePath, commitTime);

            CommitNewFile(repositoryPath, commitTime.Add(TimeSpan.FromSeconds(5)));

            var version = new VersionCalculator().GetVersion(ContextResolver.GetFileGraph(repositoryPath));

            version.Should().Be(new SemVersion(20210101, 1));
        }

        [Test]
        public void OldCsprojProjectReferences()
        {
            var projectPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.csproj");
            File.WriteAllText(projectPath, ResourceReader.CatapultCsproj);

            var deployableProcessor = new DotNetDeployableProcessor();
            var deployablePaths = deployableProcessor.GetSourceReferencedDeployablePaths(new AbsoluteFilePath(projectPath));

            deployablePaths.Length.Should().Be(1);
        }

        [Test]
        public void NewCsprojProjectReferences()
        {
            var projectPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.csproj");
            File.WriteAllText(projectPath, ResourceReader.GitTreeVersionTestsCsproj);

            var deployableProcessor = new DotNetDeployableProcessor();
            var deployablePaths = deployableProcessor.GetSourceReferencedDeployablePaths(new AbsoluteFilePath(projectPath));

            deployablePaths.Length.Should().Be(1);
        }
    }
}