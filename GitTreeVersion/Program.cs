using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Xml.Linq;
using GitTreeVersion.Context;

namespace GitTreeVersion
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var rootCommand = new RootCommand();

            var versionCommand = new Command("version")
            {
                Handler = CommandHandler.Create<bool>(DoVersion),
            };

            versionCommand.AddOption(new Option<bool>("--directory-build-props"));
            
            rootCommand.AddCommand(versionCommand);

            await rootCommand.InvokeAsync(args);
        }

        private static void DoVersion(bool directoryBuildProps)
        {
            var stopwatch = Stopwatch.StartNew();
            var repositoryContext = ContextResolver.GetRepositoryContext(Environment.CurrentDirectory);
            
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
            Console.WriteLine($"Elapsed: {stopwatch.ElapsedMilliseconds} ms");
        }
    }
}