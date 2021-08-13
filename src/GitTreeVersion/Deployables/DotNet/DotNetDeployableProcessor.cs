using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Xml.XPath;
using GitTreeVersion.Paths;

namespace GitTreeVersion.Deployables.DotNet
{
    public class DotNetDeployableProcessor
    {
        public AbsoluteFilePath[] GetSourceReferencedDeployablePaths(AbsoluteFilePath filePath)
        {
            var document = XDocument.Load(filePath.ToString());

            if (filePath.Parent.IsAtRoot)
            {
                return Array.Empty<AbsoluteFilePath>();
            }

            if (document.Root is null)
            {
                return Array.Empty<AbsoluteFilePath>();
            }

            var rootName = document.Root.Name;

            var projectReferenceElements = document.Root.XPathSelectElements("//*[local-name() = 'ProjectReference']");

            if (projectReferenceElements is null)
            {
                return Array.Empty<AbsoluteFilePath>();
            }

            var list = new List<AbsoluteFilePath>();

            foreach (var element in projectReferenceElements)
            {
                var attribute = element.Attribute("Include");

                if (attribute is null)
                {
                    continue;
                }

                list.Add(filePath.Parent.CombineToFile(attribute.Value));
            }

            return list.ToArray();
        }
    }
}