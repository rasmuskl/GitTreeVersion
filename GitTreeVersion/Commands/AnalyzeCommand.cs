using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Xml.Linq;
using System.Xml.XPath;

namespace GitTreeVersion.Commands
{
    public class AnalyzeCommand : Command
    {
        public AnalyzeCommand() : base("analyze", "Analyzes dependencies between project")
        {
            Handler = CommandHandler.Create<string>(Execute);
            
            AddArgument(new Argument("project"));
        }

        private void Execute(string project)
        {
            var analyzedProjects = new HashSet<string>();
            var projectQueue = new Queue<string>();
            projectQueue.Enqueue(project);

            while (projectQueue.TryDequeue(out var nextProject))
            {
                var fileInfo = new FileInfo(nextProject);

                if (!fileInfo.Exists)
                {
                    throw new Exception($"Unable to find project: {project}");
                }

                if (analyzedProjects.Contains(fileInfo.FullName))
                {
                    continue;
                }
                
                Console.WriteLine($"Analyzing: {fileInfo.FullName}");
                
                analyzedProjects.Add(fileInfo.FullName);

                var document = XDocument.Load(fileInfo.FullName);

                foreach (var element in document.Root?.XPathSelectElements("//ProjectReference") ?? Array.Empty<XElement>())
                {
                    var attribute = element.Attribute("Include");

                    if (fileInfo.DirectoryName is null || attribute is null)
                    {
                        continue;
                    }
                    
                    projectQueue.Enqueue(Path.Combine(fileInfo.DirectoryName, attribute.Value));
                }
            }
        }
    }
}