using System.IO;
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

        public void ApplyVersion(SemVersion version, ApplyOptions applyOptions)
        {
            var document = XDocument.Load(FilePath.FullName, LoadOptions.PreserveWhitespace);

            var writerSettings = new XmlWriterSettings { OmitXmlDeclaration = true };

            var versionElement = document.XPathSelectElements("//PropertyGroup/Version").FirstOrDefault();

            if (versionElement is not null)
            {
                versionElement.SetValue(version.ToString());
                SaveChanges(document, writerSettings, applyOptions);
                return;
            }

            var propertyGroupElement = document.XPathSelectElement("//PropertyGroup");

            if (propertyGroupElement is not null)
            {
                propertyGroupElement.Add(new XElement("Version", version.ToString()));
                SaveChanges(document, writerSettings, applyOptions);
                return;
            }

            var projectElement = document.XPathSelectElement("//Project");

            if (projectElement is not null)
            {
                projectElement.Add(new XElement("PropertyGroup", new XElement("Version", version.ToString())));
                SaveChanges(document, writerSettings, applyOptions);
                return;
            }

            Log.Warning($"Unable to apply version to: {FilePath.FullName}");
        }

        private void SaveChanges(XDocument document, XmlWriterSettings writerSettings, ApplyOptions applyOptions)
        {
            if (applyOptions.BackupChangedFiles)
            {
                File.Copy(FilePath.FullName, $"{FilePath.FullName}.bak", true);
            }

            using var xmlWriter = XmlWriter.Create(FilePath.FullName, writerSettings);
            document.Save(xmlWriter);
        }
    }
}