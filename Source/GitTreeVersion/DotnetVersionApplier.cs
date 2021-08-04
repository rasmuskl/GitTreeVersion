using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using GitTreeVersion.Paths;
using Semver;

namespace GitTreeVersion
{
    public class DotnetVersionApplier
    {
        public void ApplyVersion(AbsoluteFilePath filePath, SemVersion version)
        {
            var document = XDocument.Load(filePath.FullName, LoadOptions.PreserveWhitespace);

            var writerSettings = new XmlWriterSettings() { OmitXmlDeclaration = true };

            var versionElement = document.XPathSelectElements("//PropertyGroup/Version").FirstOrDefault();

            if (versionElement is not null)
            {
                versionElement.SetValue(version.ToString());
                using var xmlWriter = XmlWriter.Create(filePath.FullName, writerSettings);
                document.Save(xmlWriter);
                return;
            }

            var propertyGroupElement = document.XPathSelectElement("//PropertyGroup");

            if (propertyGroupElement is not null)
            {
                propertyGroupElement.Add(new XElement("Version", version.ToString()));
                using var xmlWriter = XmlWriter.Create(filePath.FullName, writerSettings);
                document.Save(xmlWriter);
                return;
            }

            var projectElement = document.XPathSelectElement("//Project");

            if (projectElement is not null)
            {
                projectElement.Add(new XElement("PropertyGroup", new XElement("Version", version.ToString())));
                using var xmlWriter = XmlWriter.Create(filePath.FullName, writerSettings);
                document.Save(xmlWriter);
                return;
            }

            Log.Warning($"Unable to apply version to: {filePath.FullName}");
        }
    }
}
