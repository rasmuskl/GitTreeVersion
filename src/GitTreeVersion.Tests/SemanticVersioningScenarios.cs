using System;
using FluentAssertions;
using NUnit.Framework;
using Semver;

namespace GitTreeVersion.Tests
{
    public class SemanticVersioningScenarios : GitTestBase
    {
        [Test]
        public void SemanticVersion_SingleConfigCommit()
        {
            var repositoryPath = CreateGitRepository();

            CommitVersionConfig(repositoryPath, new VersionConfig { Preset = VersionPreset.SemanticVersion });

            var version = CalculateVersion(repositoryPath);

            version.Should().Be(new SemVersion(0, 0, 1));
        }

        [Test]
        public void SemanticVersion_NoBaseVersionSet()
        {
            var repositoryPath = CreateGitRepository();

            CommitNewFile(repositoryPath);
            CommitNewFile(repositoryPath);
            CommitNewFile(repositoryPath);

            CommitVersionConfig(repositoryPath, new VersionConfig
            {
                Preset = VersionPreset.SemanticVersion
            });

            var version = CalculateVersion(repositoryPath);

            version.Should().Be(new SemVersion(0, 0, 4));
        }

        [Test]
        public void SemanticVersion_SingleCommitAfterConfig()
        {
            var repositoryPath = CreateGitRepository();

            CommitVersionConfig(repositoryPath, new VersionConfig
            {
                Preset = VersionPreset.SemanticVersion,
                BaseVersion = "0.0.0"
            });

            CommitNewFile(repositoryPath);

            var version = CalculateVersion(repositoryPath);

            version.Should().Be(new SemVersion(0, 0, 1));
        }

        [Test]
        public void SemanticVersion_CurrentVersionInvalid()
        {
            var repositoryPath = CreateGitRepository();

            CommitVersionConfig(repositoryPath, new VersionConfig
            {
                Preset = VersionPreset.SemanticVersion,
                BaseVersion = "abc"
            });

            CommitNewFile(repositoryPath);

            Action calculateAction = () => CalculateVersion(repositoryPath);

            calculateAction.Should().Throw<UserException>();
        }

        [Test]
        public void SemanticVersion_OldVersionInvalid()
        {
            var repositoryPath = CreateGitRepository();

            CommitVersionConfig(repositoryPath, new VersionConfig
            {
                Preset = VersionPreset.SemanticVersion,
                BaseVersion = "abc"
            });

            CommitVersionConfig(repositoryPath, new VersionConfig
            {
                Preset = VersionPreset.SemanticVersion,
                BaseVersion = "1.0.0"
            });

            var version = CalculateVersion(repositoryPath);
            version.Should().Be(new SemVersion(1));
        }

        [Test]
        public void SemanticVersion_ChangeConfigButNotVersionAfterConfig()
        {
            var repositoryPath = CreateGitRepository();

            CommitVersionConfig(repositoryPath, new VersionConfig
            {
                Preset = VersionPreset.SemanticVersion,
                BaseVersion = "1.0.0"
            });

            CommitVersionConfig(repositoryPath, new VersionConfig
            {
                Preset = VersionPreset.SemanticVersion,
                BaseVersion = "1.0.0",
                ExtraDirectories = new []{ "test" }
            });

            var version = CalculateVersion(repositoryPath);

            version.Should().Be(new SemVersion(1, 0, 1));
        }
    }
}