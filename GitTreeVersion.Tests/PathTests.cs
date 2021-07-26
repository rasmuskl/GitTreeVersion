using System;
using System.IO;
using FluentAssertions;
using GitTreeVersion.Paths;
using Xunit;

namespace GitTreeVersion.Tests
{
    public class PathTests
    {
        [Fact]
        public void IsInSubPathOf_File_True()
        {
            var directoryPath = new AbsoluteDirectoryPath(Path.Combine(Environment.CurrentDirectory, "xyz/"));
            var filePath = new AbsoluteFilePath(Path.Combine(Environment.CurrentDirectory, "xyz/def/abc.txt"));

            filePath.IsInSubPathOf(directoryPath).Should().BeTrue();
        }

        [Fact]
        public void IsInSubPathOf_File_False()
        {
            var parentPath = new AbsoluteDirectoryPath(Path.Combine(Environment.CurrentDirectory, "xyz/"));
            var filePath = new AbsoluteFilePath(Path.Combine(Environment.CurrentDirectory, "xyzd/def/abc.txt"));

            filePath.IsInSubPathOf(parentPath).Should().BeFalse();
        }

        [Fact]
        public void IsInSubPathOf_Directory_True()
        {
            var parentPath = new AbsoluteDirectoryPath(Path.Combine(Environment.CurrentDirectory, "xyz"));
            var childPath = new AbsoluteDirectoryPath(Path.Combine(Environment.CurrentDirectory, "xyz/def/"));

            childPath.IsInSubPathOf(parentPath).Should().BeTrue();
        }

        [Fact]
        public void IsInSubPathOf_Directory_False()
        {
            var path1 = new AbsoluteDirectoryPath(Path.Combine(Environment.CurrentDirectory, "xyz"));
            var path2 = new AbsoluteDirectoryPath(Path.Combine(Environment.CurrentDirectory, "def/xyz"));

            path2.IsInSubPathOf(path1).Should().BeFalse();
        }
    }
}