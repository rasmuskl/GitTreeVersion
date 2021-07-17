using System.CommandLine;
using System.Threading.Tasks;
using GitTreeVersion.Commands;

namespace GitTreeVersion
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var rootCommand = new RootCommand();
            rootCommand.AddGlobalOption(new Option<bool>("--debug"));

            rootCommand.AddCommand(new InitCommand());
            rootCommand.AddCommand(new VersionCommand());
            rootCommand.AddCommand(new CheckChangedCommand());
            rootCommand.AddCommand(new AnalyzeCommand());

            await rootCommand.InvokeAsync(args);
        }
    }
}