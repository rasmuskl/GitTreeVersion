using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GitTreeVersion
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var rootCommand = new RootCommand();
            
            rootCommand.AddCommand(new Command("version")
            {
                Handler = CommandHandler.Create(DoVersion)
            });

            rootCommand.AddCommand(new Command("directory-props")
            {
                Handler = CommandHandler.Create(DoDirectoryProps)
            });
            
            await rootCommand.InvokeAsync(args);
        }

        private static void DoDirectoryProps()
        {
            var workingDirectory = Environment.CurrentDirectory;

            var version = new VersionCalculator().GetVersion(workingDirectory);

            var xDocument = new XDocument(
                new XElement("Project",
                    new XElement("PropertyGroup", 
                        new XElement("Version", version.ToString()))));
            
            
            xDocument.Save("Directory.Build.props");
            
            Console.WriteLine($"Wrote version {version} to Directory.Build.props");
        }

        private static void DoVersion()
        {
            var workingDirectory = Environment.CurrentDirectory;

            var stopwatch = Stopwatch.StartNew();

            // GitFindFiles(workingDirectory, ":(top,glob)**/*.csproj");
            // var file = @"Source/PoeNinja/PoeNinja.csproj";
            // var lastCommitHashes = GitLastCommitHashes(workingDirectory, file);
            // Console.WriteLine($"Last commit hash: {string.Join(Environment.NewLine, lastCommitHashes)}");
            // var range = $"{lastCommitHashes.Last()}..";

            var version = new VersionCalculator().GetVersion(workingDirectory);
            Console.WriteLine($"Version: {version}");

            Console.WriteLine($"Elapsed: {stopwatch.ElapsedMilliseconds} ms");
        }
    }
}