using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using GitTreeVersion.Paths;
using Semver;

namespace GitTreeVersion.Deployables.DotNet
{
    public class DotNetSdkStyleProject : IDeployable
    {
        public DotNetSdkStyleProject(AbsoluteFilePath filePath, AbsoluteFilePath[] referencedDeployablePaths)
        {
            FilePath = filePath;
            ReferencedDeployablePaths = referencedDeployablePaths;
        }

        public AbsoluteFilePath FilePath { get; }
        public AbsoluteFilePath[] ReferencedDeployablePaths { get; }

        public void ApplyVersion(SemVersion version)
        {
            var document = XDocument.Load(FilePath.FullName, LoadOptions.PreserveWhitespace);

            var writerSettings = new XmlWriterSettings { OmitXmlDeclaration = true };

            var versionElement = document.XPathSelectElements("//PropertyGroup/Version").FirstOrDefault();

            if (versionElement is not null)
            {
                versionElement.SetValue(version.ToString());
                using var xmlWriter = XmlWriter.Create(FilePath.FullName, writerSettings);
                document.Save(xmlWriter);
                return;
            }

            var propertyGroupElement = document.XPathSelectElement("//PropertyGroup");

            if (propertyGroupElement is not null)
            {
                propertyGroupElement.Add(new XElement("Version", version.ToString()));
                using var xmlWriter = XmlWriter.Create(FilePath.FullName, writerSettings);
                document.Save(xmlWriter);
                return;
            }

            var projectElement = document.XPathSelectElement("//Project");

            if (projectElement is not null)
            {
                projectElement.Add(new XElement("PropertyGroup", new XElement("Version", version.ToString())));
                using var xmlWriter = XmlWriter.Create(FilePath.FullName, writerSettings);
                document.Save(xmlWriter);
                return;
            }

            Log.Warning($"Unable to apply version to: {FilePath.FullName}");
        }
    }
}