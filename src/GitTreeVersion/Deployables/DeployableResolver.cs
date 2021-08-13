using System.Collections.Generic;
using System.Xml.Linq;
using System.Xml.XPath;
using GitTreeVersion.Deployables.DotNet;
using GitTreeVersion.Deployables.Npm;
using GitTreeVersion.Paths;

namespace GitTreeVersion.Deployables
{
    public class DeployableResolver
    {
        public IDeployable? Resolve(AbsoluteFilePath filePath)
        {
            if (filePath.FileName == "package.json")
            {
                return new NpmProject(filePath);
            }

            if (filePath.Extension == ".csproj")
            {
                var document = XDocument.Load(filePath.FullName);

                if (document.Root is null)
                {
                    return null;
                }

                var projectReferenceElements = document.Root.XPathSelectElements("//*[local-name() = 'ProjectReference']");

                var referencedDependencies = new List<AbsoluteFilePath>();

                foreach (var element in projectReferenceElements)
                {
                    var attribute = element.Attribute("Include");

                    if (attribute is null)
                    {
                        continue;
                    }

                    referencedDependencies.Add(filePath.Parent.CombineToFile(attribute.Value));
                }

                if (document.Root.Attribute("Sdk") is not null)
                {
                    return new DotNetSdkStyleProject(filePath, referencedDependencies.ToArray());
                }

                return new DotNetClassicProject(filePath, referencedDependencies.ToArray());
            }

            return null;
        }
    }
}