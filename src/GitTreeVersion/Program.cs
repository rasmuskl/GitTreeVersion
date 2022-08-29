using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Reflection;
using System.Threading.Tasks;
using GitTreeVersion.Commands;
using Spectre.Console;

namespace GitTreeVersion
{
    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand();
            rootCommand.AddGlobalOption(new Option<bool>("--debug", "Output debug information"));

            rootCommand.AddCommand(new InitCommand());
            rootCommand.AddCommand(new VersionCommand());
            rootCommand.AddCommand(new CheckChangedCommand());
            rootCommand.AddCommand(new AnalyzeCommand());
            rootCommand.AddCommand(new TreeCommand());
            rootCommand.AddCommand(new BumpCommand());

            var commandLineBuilder = new CommandLineBuilder(rootCommand)
                .UseVersionOption()
                .UseHelp()
                .UseEnvironmentVariableDirective()
                .UseParseDirective()
                .UseDebugDirective()
                .UseSuggestDirective()
                .RegisterWithDotnetSuggest()
                .UseTypoCorrections()
                .UseParseErrorReporting()
                .UseExceptionHandler(OnException)
                .CancelOnProcessTermination();

            var parser = commandLineBuilder.Build();
            return await parser.InvokeAsync(args);
        }

        private static void OnException(Exception exception, InvocationContext context)
        {
            context.ExitCode = 1;

            while (exception is TargetInvocationException && exception.InnerException is not null)
            {
                exception = exception.InnerException;
            }

            if (exception is OperationCanceledException)
            {
                return;
            }

            if (exception is UserException)
            {
                Log.Error(exception.Message);
                return;
            }

            Log.Error("Unhandled exception:");
            AnsiConsole.WriteException(exception, ExceptionFormats.ShortenEverything);
        }
    }
}