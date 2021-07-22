using System.IO;
using FluentAssertions;
using Xunit;

namespace GitTreeVersion.Tests
{
    public class PathTests
    {
        [Fact]
        public void IsSubPathOf_True()
        {
            var path1 = new FileInfo("xyz/abc.txt");
            var path2 = new FileInfo("xyz/def/abc.txt");

            path2.Directory.IsSubPathOf(path1.Directory).Should().BeTrue();
        }
    }
}