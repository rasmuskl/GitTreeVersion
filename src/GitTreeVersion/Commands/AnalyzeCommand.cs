using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using GitTreeVersion.Context;
using GitTreeVersion.Paths;
using Spectre.Console;

namespace GitTreeVersion.Commands
{
    public class AnalyzeCommand : Command
    {
        public AnalyzeCommand() : base("analyze", "Analyzes dependencies between project")
        {
            Handler = CommandHandler.Create<string, bool>(Execute);

            AddArgument(new Argument("project"));
        }

        private void Execute(string project, bool debug)
        {
            Log.IsDebug = debug;
            var projectPath = new AbsoluteFilePath(Path.GetFullPath(project));
            var projectDirectoryPath = projectPath.Parent;
            var tree = new Tree(projectPath.FileName);

            if (!projectPath.Exists)
            {
                throw new Exception($"File not found: {project}");
            }

            AnsiConsole
                .Status()
                .Start("Analyzing project...", ctx =>
                {
                    var graph = ContextResolver.GetFileGraph(projectDirectoryPath!);
                    AddDependencies(tree, graph, projectPath);
                });

            AnsiConsole.Render(tree);
        }

        private void AddDependencies(IHasTreeNodes tree, FileGraph graph, AbsoluteFilePath projectPath)
        {
            if (!graph.DeployableFileDependencies.TryGetValue(projectPath, out var dependencies))
            {
                return;
            }

            foreach (var dependency in dependencies)
            {
                var node = tree.AddNode(dependency.FileName);
                AddDependencies(node, graph, dependency);
            }
        }
    }
}