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
            Handler = CommandHandler.Create<bool>(Execute);
        }

        private void Execute(bool debug)
        {
            Log.IsDebug = debug;

            var workingDirectory = new AbsoluteDirectoryPath(Environment.CurrentDirectory);
            var versionConfigPath = workingDirectory.CombineToFile(ContextResolver.VersionConfigFileName);

            if (versionConfigPath.Exists)
            {
                Console.WriteLine($"{ContextResolver.VersionConfigFileName} already exists.");
                return;
            }

            File.WriteAllText(versionConfigPath.ToString(), JsonSerializer.Serialize(new VersionConfig(), JsonOptions.DefaultOptions));
        }
    }
}