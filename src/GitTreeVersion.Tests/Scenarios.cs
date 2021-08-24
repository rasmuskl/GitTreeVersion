using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using GitTreeVersion.Context;
using GitTreeVersion.Deployables;
using GitTreeVersion.Deployables.DotNet;
using GitTreeVersion.Deployables.Helm;
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

            Action action = () => { CalculateVersion(repositoryPath); };

            action.Should().Throw<UserException>();
        }

        [Test]
        public void EmptyRepository()
        {
            var repositoryPath = CreateGitRepository();

            var version = CalculateVersion(repositoryPath);

            version.Should().Be(new SemVersion(0));
        }

        [Test]
        public void SingleCommit()
        {
            var repositoryPath = CreateGitRepository();

            CommitNewFile(repositoryPath);

            var version = CalculateVersion(repositoryPath);

            version.Should().Be(new SemVersion(0, 0, 1));
        }

        [Test]
        public void TwoCommits()
        {
            var repositoryPath = CreateGitRepository();

            CommitNewFile(repositoryPath);
            CommitNewFile(repositoryPath);

            var version = CalculateVersion(repositoryPath);

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

            var version = CalculateVersion(repositoryPath);

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

            var version = CalculateVersion(repositoryPath);

            version.Should().Be(new SemVersion(0, 0, 2));
        }

        [Test]
        public void NonDefaultBranch()
        {
            var repositoryPath = CreateGitRepository();

            CommitNewFile(repositoryPath);

            var branchName = CreateBranch(repositoryPath);

            CommitNewFile(repositoryPath);

            var version = CalculateVersion(repositoryPath);

            version.Should().Be(new SemVersion(0, 0, 2, $"{branchName}.1"));
        }

        [Test]
        public void NonDefaultSlashBranch()
        {
            var repositoryPath = CreateGitRepository();

            CommitNewFile(repositoryPath);

            CreateBranch(repositoryPath, "feature/12345");

            CommitNewFile(repositoryPath);

            var version = CalculateVersion(repositoryPath);

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

            var branchVersion = CalculateVersion(repositoryPath);

            MergeBranchToMaster(repositoryPath, branchName);

            var mergedMasterVersion = CalculateVersion(repositoryPath);

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
                new VersionConfig { Preset = VersionPreset.SemanticVersion, BaseVersion = "1.0.0" },
                new VersionConfig { Preset = VersionPreset.SemanticVersion, BaseVersion = "1.1.0" },
                new VersionConfig { Preset = VersionPreset.SemanticVersion, BaseVersion = "1.2.0" },
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
                serializedVersionConfig.Preset.Should().Be(deserializedVersionConfig!.Preset);
                serializedVersionConfig.BaseVersion.Should().Be(deserializedVersionConfig.BaseVersion);
            }
        }

        [Test]
        public void OldCsprojProjectReferences()
        {
            var projectPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.csproj");
            File.WriteAllText(projectPath, ResourceReader.CatapultCsproj);

            var deployable = new DeployableResolver().Resolve(new AbsoluteFilePath(projectPath));

            deployable.Should().NotBeNull();
            deployable.Should().BeOfType<DotNetClassicProject>();
            deployable!.ReferencedDeployablePaths.Length.Should().Be(1);
        }

        [Test]
        public void NewCsprojProjectReferences()
        {
            var projectPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.csproj");
            File.WriteAllText(projectPath, ResourceReader.GitTreeVersionTestsCsproj);

            var deployable = new DeployableResolver().Resolve(new AbsoluteFilePath(projectPath));

            deployable.Should().NotBeNull();
            deployable.Should().BeOfType<DotNetSdkStyleProject>();
            deployable!.ReferencedDeployablePaths.Length.Should().Be(1);
        }

        [Test]
        public void HelmChartApply()
        {
            var chartPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "Chart.yaml");
            Directory.CreateDirectory(Path.GetDirectoryName(chartPath)!);
            File.WriteAllText(chartPath, ResourceReader.BasicChartYaml);

            var deployable = new DeployableResolver().Resolve(new AbsoluteFilePath(chartPath));

            deployable.Should().NotBeNull();
            deployable.Should().BeOfType<HelmChart>();

            deployable!.ApplyVersion(new SemVersion(1));
            var result = File.ReadAllText(chartPath);

            result.Should().Contain("version: 1.0.0");
        }
    }
}