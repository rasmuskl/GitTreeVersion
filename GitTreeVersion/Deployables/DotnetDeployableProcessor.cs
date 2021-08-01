using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using System.Xml.XPath;
using GitTreeVersion.Paths;

namespace GitTreeVersion.Deployables
{
    public class DotnetDeployableProcessor
    {
        public AbsoluteFilePath[] GetSourceReferencedDeployablePaths(FileInfo fileInfo)
        {
            var document = XDocument.Load(fileInfo.FullName);

            if (fileInfo.DirectoryName == null)
            {
                return Array.Empty<AbsoluteFilePath>();
            }

            var projectReferenceElements = document.Root?.XPathSelectElements("//ProjectReference");

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

                list.Add(new AbsoluteFilePath(Path.GetFullPath(Path.Combine(fileInfo.DirectoryName, attribute.Value))));
            }

            return list.ToArray();
        }
    }
}