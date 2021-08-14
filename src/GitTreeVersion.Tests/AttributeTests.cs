using System.Reflection;
using FluentAssertions;
using GitTreeVersion.Deployables.DotNet;
using NUnit.Framework;

namespace GitTreeVersion.Tests
{
    public class AttributeTests
    {
        private static readonly AttributeAugmenter? Augmenter = new();

        [Test]
        public void MissingAttribute_Add()
        {
            var newContents = Augmenter.EnsureStringAttributes(string.Empty, new[]
            {
                new StringAttribute(typeof(AssemblyVersionAttribute), "1.2.3"),
            });

            newContents.Trim().Should().Be("[assembly: System.Reflection.AssemblyVersionAttribute(\"1.2.3\")]");
        }

        [Test]
        public void ExactMatch_Keep()
        {
            var contents = "[assembly: System.Reflection.AssemblyVersionAttribute(\"1.2.3\")]";

            var newContents = Augmenter.EnsureStringAttributes(contents, new[]
            {
                new StringAttribute(typeof(AssemblyVersionAttribute), "1.2.3"),
            });

            newContents.Trim().Should().Be("[assembly: System.Reflection.AssemblyVersionAttribute(\"1.2.3\")]");
        }

        [Test]
        public void WrongVersion_Replace()
        {
            var contents = "[assembly: System.Reflection.AssemblyVersionAttribute(\"1.0.0\")]";

            var newContents = Augmenter.EnsureStringAttributes(contents, new[]
            {
                new StringAttribute(typeof(AssemblyVersionAttribute), "1.2.3"),
            });

            newContents.Trim().Should().Be("[assembly: System.Reflection.AssemblyVersionAttribute(\"1.2.3\")]");
        }

        [Test]
        public void NoNamespaceMatch_Replace()
        {
            var contents = "[assembly: AssemblyVersionAttribute(\"1.2.3\")]";

            var newContents = Augmenter.EnsureStringAttributes(contents, new[]
            {
                new StringAttribute(typeof(AssemblyVersionAttribute), "1.2.3"),
            });

            newContents.Trim().Should().Be("[assembly: System.Reflection.AssemblyVersionAttribute(\"1.2.3\")]");
        }

        [Test]
        public void NoAttributeSuffix_Replace()
        {
            var contents = "[assembly: AssemblyVersion(\"1.2.3\")]";

            var newContents = Augmenter.EnsureStringAttributes(contents, new[]
            {
                new StringAttribute(typeof(AssemblyVersionAttribute), "1.2.3"),
            });

            newContents.Trim().Should().Be("[assembly: System.Reflection.AssemblyVersionAttribute(\"1.2.3\")]");
        }

        [Test]
        public void SingleLineComment_Add()
        {
            var contents = "// [assembly: AssemblyVersion(\"1.2.3\")]";

            var newContents = Augmenter.EnsureStringAttributes(contents, new[]
            {
                new StringAttribute(typeof(AssemblyVersionAttribute), "1.2.3"),
            });

            newContents.Trim().Should().StartWith("// [assembly: AssemblyVersion(\"1.2.3\")]");
            newContents.Trim().Should().EndWith("[assembly: System.Reflection.AssemblyVersionAttribute(\"1.2.3\")]");
        }
    }
}