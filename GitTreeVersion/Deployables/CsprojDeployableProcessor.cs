using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using System.Xml.XPath;

namespace GitTreeVersion.Deployables
{
    public class CsprojDeployableProcessor
    {
        public string[] GetSourceReferencedDeployablePaths(FileInfo fileInfo)
        {
            var document = XDocument.Load(fileInfo.FullName);

            if (fileInfo.DirectoryName == null)
            {
                return Array.Empty<string>();
            }

            var projectReferenceElements = document.Root?.XPathSelectElements("//ProjectReference");

            if (projectReferenceElements is null)
            {
                return Array.Empty<string>();
            }

            var list = new List<string>();

            foreach (var element in projectReferenceElements)
            {
                var attribute = element.Attribute("Include");

                if (attribute is null)
                {
                    continue;
                }
                
                list.Add(Path.GetFullPath(Path.Combine(fileInfo.DirectoryName, attribute.Value)));
            }

            return list.ToArray();
        }
    }
}