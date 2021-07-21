using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Xml.Linq;
using GitTreeVersion.Context;

namespace GitTreeVersion.Commands
{
    public class VersionCommand : Command
    {
        public VersionCommand() : base("version", "Versions the thing")
        {
            Handler = CommandHandler.Create<bool, bool>(Execute);
            
            AddOption(new Option<bool>("--directory-build-props"));
        }

        private void Execute(bool directoryBuildProps, bool debug)
        {
            Git.Debug = debug;

            var stopwatch = Stopwatch.StartNew();
            var repositoryContext = ContextResolver.GetFileGraph(Environment.CurrentDirectory);

            Console.WriteLine($"Repository root: {repositoryContext.RepositoryRootPath}");
            Console.WriteLine($"Version root: {repositoryContext.VersionRootPath}");

            // GitFindFiles(workingDirectory, ":(top,glob)**/*.csproj");
            // var file = @"Source/PoeNinja/PoeNinja.csproj";
            // var lastCommitHashes = GitLastCommitHashes(workingDirectory, file);
            // Console.WriteLine($"Last commit hash: {string.Join(Environment.NewLine, lastCommitHashes)}");
            // var range = $"{lastCommitHashes.Last()}..";

            var version = new VersionCalculator().GetVersion(repositoryContext);

            if (directoryBuildProps)
            {
                var xDocument = new XDocument(
                    new XElement("Project",
                        new XElement("PropertyGroup",
                            new XElement("Version", version.ToString()))));


                xDocument.Save("Directory.Build.props");
                Console.WriteLine($"Wrote version {version} to Directory.Build.props");
            }

            Console.WriteLine($"Version: {version}");

            if (debug)
            {
                Console.WriteLine($"Elapsed: {stopwatch.ElapsedMilliseconds} ms");
            }
        }
    }
}