using System;
using FluentAssertions;
using NUnit.Framework;
using Semver;

namespace GitTreeVersion.Tests
{
    public class CalendarVersioningScenarios : GitTestBase
    {
        [Test]
        public void CalendarVersioning_SingleCommit()
        {
            var repositoryPath = CreateGitRepository();

            var commitTime = new DateTimeOffset(2021, 1, 1, 10, 42, 5, TimeSpan.Zero);
            var filePath = WriteVersionConfig(repositoryPath, new VersionConfig { Mode = VersionMode.CalendarVersion });
            CommitFile(repositoryPath, filePath, commitTime);

            var version = CalculateVersion(repositoryPath);

            version.Should().Be(new SemVersion(2021, 101));
        }

        [Test]
        public void CalendarVersioning_TwoCommit()
        {
            var repositoryPath = CreateGitRepository();

            var commitTime = new DateTimeOffset(2021, 1, 1, 10, 42, 5, TimeSpan.Zero);
            var filePath = WriteVersionConfig(repositoryPath, new VersionConfig { Mode = VersionMode.CalendarVersion });
            CommitFile(repositoryPath, filePath, commitTime);

            CommitNewFile(repositoryPath, commitTime.Add(TimeSpan.FromSeconds(5)));

            var version = CalculateVersion(repositoryPath);

            version.Should().Be(new SemVersion(2021, 101, 1));
        }
    }
}