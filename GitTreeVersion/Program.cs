using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Threading.Tasks;

namespace GitTreeVersion
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var rootCommand = new RootCommand();
            
            rootCommand.AddCommand(new Command("version") { Handler = CommandHandler.Create(() =>
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

                Console.WriteLine($"Elapsed {stopwatch.ElapsedMilliseconds} ms");
            })});

            await rootCommand.InvokeAsync(args);
        }
    }
}