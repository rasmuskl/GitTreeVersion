using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Text.Json;
using GitTreeVersion.Context;
using GitTreeVersion.Paths;

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

            File.WriteAllText(versionConfigPath.ToString(), JsonSerializer.Serialize(VersionConfig.Default, JsonOptions.DefaultOptions));
        }
    }
}