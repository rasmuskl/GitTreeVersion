using System.IO;
using FluentAssertions;
using Xunit;

namespace GitTreeVersion.Tests
{
    public class PathTests
    {
        [Fact]
        public void IsInSubPathOf_File_True()
        {
            var path1 = new DirectoryInfo("xyz/");
            var path2 = new FileInfo("xyz/def/abc.txt");

            path2.IsInSubPathOf(path1).Should().BeTrue();
        }

        [Fact]
        public void IsInSubPathOf_File_False()
        {
            var path1 = new DirectoryInfo("xyz/");
            var path2 = new FileInfo("xyzd/def/abc.txt");

            path2.IsInSubPathOf(path1).Should().BeFalse();
        }

        [Fact]
        public void IsInSubPathOf_Directory_True()
        {
            var path1 = new DirectoryInfo("xyz");
            var path2 = new DirectoryInfo("xyz/def/");

            path2.IsInSubPathOf(path1).Should().BeTrue();
        }

        [Fact]
        public void IsInSubPathOf_Directory_False()
        {
            var path1 = new DirectoryInfo("xyz");
            var path2 = new DirectoryInfo("def/xyz");

            path2.IsInSubPathOf(path1).Should().BeFalse();
        }
    }
}