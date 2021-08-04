﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using System.Xml.XPath;
using GitTreeVersion.Paths;

namespace GitTreeVersion.Deployables
{
    public class DotnetDeployableProcessor
    {
        public AbsoluteFilePath[] GetSourceReferencedDeployablePaths(AbsoluteFilePath filePath)
        {
            var document = XDocument.Load(filePath.ToString());

            if (filePath.Parent.IsAtRoot)
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

                Console.WriteLine("Adding "+attribute.Value);
                list.Add(filePath.Parent.CombineToFile(attribute.Value));
                Console.WriteLine("Added "+filePath.Parent.CombineToFile(attribute.Value));

            }

            return list.ToArray();
        }
    }
}
