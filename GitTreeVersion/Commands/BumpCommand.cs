using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using GitTreeVersion.Context;
using GitTreeVersion.Paths;

namespace GitTreeVersion.Commands
{
    public class BumpCommand : Command
    {
        public BumpCommand() : base("bump", "Bump versions")
        {
            Handler = CommandHandler.Create<VersionTypeOptions, bool>(Execute);

            AddArgument(new Argument<VersionTypeOptions>("type"));
        }

        private void Execute(VersionTypeOptions type, bool debug)
        {
            Log.IsDebug = debug;
            Console.WriteLine($"Bumping {type}");

            var fileGraph = ContextResolver.GetFileGraph(new AbsoluteDirectoryPath(Environment.CurrentDirectory));
            var versionRootPath = fileGraph.VersionRootPath;
            Console.WriteLine($"Version root path: {versionRootPath}");

            var bumper = new Bumper();
            bumper.Bump(versionRootPath, (VersionType)type);
        }

        private enum VersionTypeOptions
        {
            // ReSharper disable once InconsistentNaming
            major = VersionType.Major,
            // ReSharper disable once InconsistentNaming
            minor = VersionType.Minor
        }
    }
}
 