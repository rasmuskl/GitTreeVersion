using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Text.Json;
using GitTreeVersion.Context;

namespace GitTreeVersion.Commands
{
    public class InitCommand : Command
    {
        public InitCommand() : base("init", "Creates version root")
        {
            Handler = CommandHandler.Create<bool>(Execute);
        }

        private void Execute(bool debug)
        {
            Log.IsDebug = debug;

            var workingDirectory = Environment.CurrentDirectory;
            var versionConfigPath = Path.Combine(workingDirectory, ContextResolver.VersionConfigFileName);

            if (File.Exists(versionConfigPath))
            {
                Console.WriteLine($"{ContextResolver.VersionConfigFileName} already exists.");
                return;
            }

            File.WriteAllText(versionConfigPath, JsonSerializer.Serialize(new VersionConfig(), JsonOptions.DefaultOptions));
        }
    }
}