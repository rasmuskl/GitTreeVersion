using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using GitTreeVersion.Deployables;

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

            var processor = new CsprojDeployableProcessor();

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

                var deployablePaths = processor.GetSourceReferencedDeployablePaths(fileInfo);

                foreach (var deployablePath in deployablePaths)
                {
                    projectQueue.Enqueue(deployablePath);
                }

            }
        }
    }
}