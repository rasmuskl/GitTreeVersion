using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Threading.Tasks;
using GitTreeVersion.Commands;

namespace GitTreeVersion
{
    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand();
            rootCommand.AddGlobalOption(new Option<bool>("--debug"));

            rootCommand.AddCommand(new InitCommand());
            rootCommand.AddCommand(new VersionCommand());
            rootCommand.AddCommand(new CheckChangedCommand());
            rootCommand.AddCommand(new AnalyzeCommand());
            rootCommand.AddCommand(new TreeCommand());
            rootCommand.AddCommand(new BumpCommand());

            var commandLineBuilder = new CommandLineBuilder(rootCommand)
                .UseDefaults();

            var parser = commandLineBuilder.Build();
            return await parser.InvokeAsync(args);
        }
    }
}