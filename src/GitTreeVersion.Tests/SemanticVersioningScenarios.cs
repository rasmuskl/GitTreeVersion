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

            version.Should().Be(new SemVersion(0));
        }

        [Test]
        public void SemanticVersion_SingleCommitAfterConfig()
        {
            var repositoryPath = CreateGitRepository();

            CommitVersionConfig(repositoryPath, new VersionConfig { Preset = VersionPreset.SemanticVersion });

            CommitNewFile(repositoryPath);

            var version = CalculateVersion(repositoryPath);

            version.Should().Be(new SemVersion(0, 0, 1));
        }
    }
}