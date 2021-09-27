using System.IO;
using FluentAssertions;
using GitTreeVersion.Context;
using GitTreeVersion.Git;
using GitTreeVersion.Paths;
using NUnit.Framework;
using Semver;

namespace GitTreeVersion.Tests
{
    public class FileBasedVersioningScenarios : GitTestBase
    {
        [Test]
        public void MinorVersion()
        {
            var repositoryPath = CreateGitRepository();
            WriteVersionConfig(repositoryPath, new VersionConfig { Preset = VersionPreset.SemanticVersionFileBased });

            var bumpFile = new Bumper().Bump(ContextResolver.GetVersionGraph(repositoryPath), repositoryPath, VersionType.Minor);
            CommitFile(repositoryPath, bumpFile);

            var version = CalculateVersion(repositoryPath);

            version.Should().Be(new SemVersion(0, 1));
        }

        [Test]
        public void MajorVersion()
        {
            var repositoryPath = CreateGitRepository();
            WriteVersionConfig(repositoryPath, new VersionConfig { Preset = VersionPreset.SemanticVersionFileBased });

            var bumpFile = new Bumper().Bump(ContextResolver.GetVersionGraph(repositoryPath), repositoryPath, VersionType.Major);
            CommitFile(repositoryPath, bumpFile);

            var version = CalculateVersion(repositoryPath);

            version.Should().Be(new SemVersion(1));
        }

        [Test]
        public void MinorThenMajorVersion()
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
        public void MajorThenMinorThenChangeMajor()
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
        public void MajorThenMinorThenMoveMajor()
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
        public void MajorThenMinorThenTCommitThenChangeMinor()
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
        public void MajorThenMinorThenTCommitThenMoveMinor()
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
        public void MinorThenCommitThenChangeMinor()
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
        public void MinorThenChangeThenMoveMinor()
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

        [Test]
        public void MultipleBumpsInNestedRoots_TwoBumps()
        {
            var repositoryPath = CreateGitRepository();

            var nestedRootPath = repositoryPath.CombineToDirectory("nestedroot");

            WriteVersionConfig(repositoryPath, new VersionConfig { Preset = VersionPreset.SemanticVersionFileBased });
            WriteVersionConfig(nestedRootPath, new VersionConfig { Preset = VersionPreset.SemanticVersionFileBased });

            var bumper = new Bumper();
            bumper.Bump(ContextResolver.GetVersionGraph(repositoryPath), repositoryPath, VersionType.Minor);
            bumper.Bump(ContextResolver.GetVersionGraph(repositoryPath), nestedRootPath, VersionType.Minor);

            var gitDirectory = new GitDirectory(repositoryPath);
            gitDirectory.RunGit("add", "--all");
            gitDirectory.RunGit("commit", "--all", "--message", "bump");

            var version = CalculateVersion(repositoryPath);

            version.Should().Be(new SemVersion(0, 2, 0));
        }

        [Test]
        public void MultipleBumpsSameNameInNestedRoots_OneBump()
        {
            var repositoryPath = CreateGitRepository();

            var nestedRootPath = repositoryPath.CombineToDirectory("nestedroot");

            WriteVersionConfig(repositoryPath, new VersionConfig { Preset = VersionPreset.SemanticVersionFileBased });
            WriteVersionConfig(nestedRootPath, new VersionConfig { Preset = VersionPreset.SemanticVersionFileBased });

            var bumper = new Bumper();
            var bumpFile1 = bumper.Bump(ContextResolver.GetVersionGraph(repositoryPath), repositoryPath, VersionType.Minor);
            var bumpFile2 = bumper.Bump(ContextResolver.GetVersionGraph(repositoryPath), nestedRootPath, VersionType.Minor);

            File.Copy(bumpFile1.FullName, bumpFile2.Parent.CombineToFile(bumpFile1.FileName).FullName);
            File.Delete(bumpFile2.FullName);

            var gitDirectory = new GitDirectory(repositoryPath);
            gitDirectory.RunGit("add", "--all");
            gitDirectory.RunGit("commit", "--all", "--message", "bump");

            var version = CalculateVersion(repositoryPath);

            version.Should().Be(new SemVersion(0, 1, 0));
        }

    }
}