using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading;
using GitTreeVersion.Context;
using Spectre.Console;

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
            var projectPath = Path.GetFullPath(project);
            var projectDirectoryPath = Path.GetDirectoryName(projectPath);
            var tree = new Tree(Path.GetFileName(projectPath));

            if (!File.Exists(projectPath))
            {
                throw new Exception($"File not found: {project}");
            }

            AnsiConsole
                .Status()
                .Spinner(Spinner.Known.Aesthetic)
                .Start("Analyzing project...", ctx =>
            {
                var graph = ContextResolver.GetFileGraph(projectDirectoryPath);
                AddDependencies(tree, graph, projectPath);
            });
            
            AnsiConsole.Render(tree);
        }

        private void AddDependencies(IHasTreeNodes tree, FileGraph graph, string projectPath)
        {
            if (!graph.DeployableFileDependencies.TryGetValue(projectPath, out var dependencies))
            {
                return;
            }

            foreach (var dependency in dependencies)
            {
                var node = tree.AddNode(Path.GetFileName(dependency));
                AddDependencies(node, graph, dependency);
            }
        }
    }
}