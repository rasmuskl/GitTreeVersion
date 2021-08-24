using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Text.Json;
using GitTreeVersion.Context;
using GitTreeVersion.Paths;
using Semver;
using Spectre.Console;

namespace GitTreeVersion.Commands
{
    public class InitCommand : Command
    {
        public InitCommand() : base("init", "Creates version root")
        {
            Handler = CommandHandler.Create<bool, string?>(Execute);

            AddArgument(new Argument<string?>("path", () => null));
        }

        private void Execute(bool debug, string? path)
        {
            Log.IsDebug = debug;

            if (path is not null)
            {
                path = Path.GetFullPath(path);
            }

            path ??= Environment.CurrentDirectory;

            var workingDirectory = new AbsoluteDirectoryPath(path);
            var versionConfigPath = workingDirectory.CombineToFile(ContextResolver.VersionConfigFileName);

            if (versionConfigPath.Exists)
            {
                Console.WriteLine($"{ContextResolver.VersionConfigFileName} already exists.");
                return;
            }

            var presetChoice = AnsiConsole.Prompt(new SelectionPrompt<string>()
                .Title("Which [lime]version preset[/] do you want to use?")
                .AddChoices(Enum.GetNames<VersionPreset>()));

            var versionPreset = Enum.Parse<VersionPreset>(presetChoice);

            AnsiConsole.MarkupLine($"Version preset: [lime]{versionPreset}[/]");

            var versionConfig = VersionConfig.Default;

            versionConfig.Preset = versionPreset;

            if (versionPreset == VersionPreset.SemanticVersion)
            {
                var baseVersionPrompt = new TextPrompt<string>("What is the base version?")
                    .Validate(s => SemVersion.TryParse(s, out _));

                var baseVersionString = AnsiConsole.Prompt(baseVersionPrompt);
                var baseVersion = SemVersion.Parse(baseVersionString)
                    .Change(prerelease: string.Empty);

                versionConfig.BaseVersion = baseVersion.ToString();
                AnsiConsole.MarkupLine($"Version preset: [lime]{baseVersion}[/]");
            }
            else if (versionPreset == VersionPreset.CalendarVersion)
            {
                versionConfig.BaseVersion = null;
            }

            File.WriteAllText(versionConfigPath.ToString(), JsonSerializer.Serialize(versionConfig, JsonOptions.DefaultOptions));
        }
    }
}