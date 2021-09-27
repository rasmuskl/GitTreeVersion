using System.IO;
using FluentAssertions;
using GitTreeVersion.Context;
using GitTreeVersion.Paths;
using NUnit.Framework;
using Semver;

namespace GitTreeVersion.Tests
{
    public class FileBasedVersioningScenarios : GitTestBase
    {
        [Test]
        public void FileBasedVersioning_MinorVersion()
        {
            var repositoryPath = CreateGitRepository();
            WriteVersionConfig(repositoryPath, new VersionConfig { Preset = VersionPreset.SemanticVersionFileBased });

            var bumpFile = new Bumper().Bump(ContextResolver.GetVersionGraph(repositoryPath), repositoryPath, VersionType.Minor);
            CommitFile(repositoryPath, bumpFile);

            var version = CalculateVersion(repositoryPath);

            version.Should().Be(new SemVersion(0, 1));
        }

        [Test]
        public void FileBasedVersioning_MajorVersion()
        {
            var repositoryPath = CreateGitRepository();
            WriteVersionConfig(repositoryPath, new VersionConfig { Preset = VersionPreset.SemanticVersionFileBased });

            var bumpFile = new Bumper().Bump(ContextResolver.GetVersionGraph(repositoryPath), repositoryPath, VersionType.Major);
            CommitFile(repositoryPath, bumpFile);

            var version = CalculateVersion(repositoryPath);

            version.Should().Be(new SemVersion(1));
        }

        [Test]
        public void FileBasedVersioning_MinorThenMajorVersion()
        {
            var repositoryPath = CreateGitRepository();
            WriteVersionConfig(repositoryPath, new VersionConfig { Preset = VersionPreset.SemanticVersionFileBased });

            var minorBumpFile = new Bumper().Bump(ContextResolver.GetVersionGraph(repositoryPath), repositoryPath, VersionType.Minor);
            CommitFile(repositoryPath, minorBumpFile);

            var majorBumpFile = new Bumper().Bump(ContextResolver.GetVersionGraph(repositoryPath), repositoryPath, VersionType.Major);
            CommitFile(repositoryPath, majorBumpFile);

            var version = CalculateVersion(repositoryPath);

            version.Should().Be(new SemVersion(1));
        }

        [Test]
        public void FileBasedVersioning_MajorThenMinorVersion()
        {
            var repositoryPath = CreateGitRepository();
            WriteVersionConfig(repositoryPath, new VersionConfig { Preset = VersionPreset.SemanticVersionFileBased });

            var majorBumpFile = new Bumper().Bump(ContextResolver.GetVersionGraph(repositoryPath), repositoryPath, VersionType.Major);
            CommitFile(repositoryPath, majorBumpFile);

            var minorBumpFile = new Bumper().Bump(ContextResolver.GetVersionGraph(repositoryPath), repositoryPath, VersionType.Minor);
            CommitFile(repositoryPath, minorBumpFile);

            var version = CalculateVersion(repositoryPath);

            version.Should().Be(new SemVersion(1, 1));
        }

        [Test]
        public void FileBasedVersioning_MajorThenMinorThenChangeMajor()
        {
            var repositoryPath = CreateGitRepository();
            WriteVersionConfig(repositoryPath, new VersionConfig { Preset = VersionPreset.SemanticVersionFileBased });

            var majorBumpFile = new Bumper().Bump(ContextResolver.GetVersionGraph(repositoryPath), repositoryPath, VersionType.Major);
            CommitFile(repositoryPath, majorBumpFile);

            var minorBumpFile = new Bumper().Bump(ContextResolver.GetVersionGraph(repositoryPath), repositoryPath, VersionType.Minor);
            CommitFile(repositoryPath, minorBumpFile);

            File.WriteAllText(majorBumpFile.ToString(), "change");
            CommitFile(repositoryPath, majorBumpFile);

            var version = CalculateVersion(repositoryPath);

            version.Should().Be(new SemVersion(1, 1, 1));
        }

        [Test]
        public void FileBasedVersioning_MajorThenMinorThenMoveMajor()
        {
            var repositoryPath = CreateGitRepository();
            WriteVersionConfig(repositoryPath, new VersionConfig { Preset = VersionPreset.SemanticVersionFileBased });

            var majorBumpFile = new Bumper().Bump(ContextResolver.GetVersionGraph(repositoryPath), repositoryPath, VersionType.Major);
            CommitFile(repositoryPath, majorBumpFile);

            var minorBumpFile = new Bumper().Bump(ContextResolver.GetVersionGraph(repositoryPath), repositoryPath, VersionType.Minor);
            CommitFile(repositoryPath, minorBumpFile);

            MoveAndCommitFile(repositoryPath, majorBumpFile, new AbsoluteFilePath(Path.Combine(majorBumpFile.Parent.ToString(), "new-name")));

            var version = CalculateVersion(repositoryPath);

            version.Should().Be(new SemVersion(1, 1, 1));
        }

        [Test]
        public void FileBasedVersioning_MajorThenMinorThenTCommitThenChangeMinor()
        {
            var repositoryPath = CreateGitRepository();
            WriteVersionConfig(repositoryPath, new VersionConfig { Preset = VersionPreset.SemanticVersionFileBased });

            var majorBumpFile = new Bumper().Bump(ContextResolver.GetVersionGraph(repositoryPath), repositoryPath, VersionType.Major);
            CommitFile(repositoryPath, majorBumpFile);

            var minorBumpFile = new Bumper().Bump(ContextResolver.GetVersionGraph(repositoryPath), repositoryPath, VersionType.Minor);
            CommitFile(repositoryPath, minorBumpFile);

            CommitNewFile(repositoryPath);

            File.WriteAllText(minorBumpFile.ToString(), "change");
            CommitFile(repositoryPath, minorBumpFile);

            var version = CalculateVersion(repositoryPath);

            version.Should().Be(new SemVersion(1, 1, 2));
        }

        [Test]
        [Ignore("Should work but does not yet - see comments inside")]
        public void FileBasedVersioning_MajorThenMinorThenTCommitThenMoveMinor()
        {
            var repositoryPath = CreateGitRepository();
            WriteVersionConfig(repositoryPath, new VersionConfig { Preset = VersionPreset.SemanticVersionFileBased });

            var majorBumpFile = new Bumper().Bump(ContextResolver.GetVersionGraph(repositoryPath), repositoryPath, VersionType.Major);
            CommitFile(repositoryPath, majorBumpFile, commitMessage: "bump major version");

            var minorBumpFile = new Bumper().Bump(ContextResolver.GetVersionGraph(repositoryPath), repositoryPath, VersionType.Minor);
            CommitFile(repositoryPath, minorBumpFile, commitMessage: "bump minor version");

            CommitNewFile(repositoryPath);

            MoveAndCommitFile(repositoryPath, minorBumpFile, new AbsoluteFilePath(Path.Combine(minorBumpFile.Parent.ToString(), "new-name")));

            // TODO: diff-filters are not working properly for this scenario
            // Since we get the moved file name and use it as a pathspec, it does not track back to initial bump commit unless --follow is added, but --follow only allows for exactly one pathspec
            // Potential solution - replace diff + log with reconstruction through: git log --full-history --first-parent --name-status --format=format:%H <major-commit>.. -- .version/minor/*

            var version = CalculateVersion(repositoryPath);

            version.Should().Be(new SemVersion(1, 1, 2));
        }

        [Test]
        public void FileBasedVersioning_MinorThenCommitThenChangeMinor()
        {
            var repositoryPath = CreateGitRepository();
            WriteVersionConfig(repositoryPath, new VersionConfig { Preset = VersionPreset.SemanticVersionFileBased });

            var minorBumpFile = new Bumper().Bump(ContextResolver.GetVersionGraph(repositoryPath), repositoryPath, VersionType.Minor);
            CommitFile(repositoryPath, minorBumpFile);

            CommitNewFile(repositoryPath);

            File.WriteAllText(minorBumpFile.ToString(), "change");
            CommitFile(repositoryPath, minorBumpFile);

            var version = CalculateVersion(repositoryPath);

            version.Should().Be(new SemVersion(0, 1, 2));
        }

        [Test]
        public void FileBasedVersioning_MinorThenChangeThenMoveMinor()
        {
            var repositoryPath = CreateGitRepository();
            WriteVersionConfig(repositoryPath, new VersionConfig { Preset = VersionPreset.SemanticVersionFileBased });

            var minorBumpFile = new Bumper().Bump(ContextResolver.GetVersionGraph(repositoryPath), repositoryPath, VersionType.Minor);
            CommitFile(repositoryPath, minorBumpFile);

            CommitNewFile(repositoryPath);

            MoveAndCommitFile(repositoryPath, minorBumpFile, new AbsoluteFilePath(Path.Combine(minorBumpFile.Parent.ToString(), "new-name")));

            var version = CalculateVersion(repositoryPath);

            version.Should().Be(new SemVersion(0, 1, 2));
        }
    }
}