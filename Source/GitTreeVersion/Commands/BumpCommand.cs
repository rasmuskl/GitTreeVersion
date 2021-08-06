using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using GitTreeVersion.Context;
using GitTreeVersion.Paths;

namespace GitTreeVersion.Commands
{
    public class BumpCommand : Command
    {
        public BumpCommand() : base("bump", "Bump versions")
        {
            Handler = CommandHandler.Create<VersionTypeOptions, bool, string?>(Execute);

            AddArgument(new Argument<VersionTypeOptions>("type"));
            AddArgument(new Argument<string?>("path", () => null));
        }

        private void Execute(VersionTypeOptions type, bool debug, string? path)
        {
            Log.IsDebug = debug;
            path ??= Environment.CurrentDirectory;
            Console.WriteLine($"Bumping {type}");

            var fileGraph = ContextResolver.GetFileGraph(new AbsoluteDirectoryPath(path));
            var versionRootPath = fileGraph.VersionRootPath;
            Console.WriteLine($"Version root path: {versionRootPath}");

            var bumper = new Bumper();
            bumper.Bump(versionRootPath, (VersionType) type);
        }

        private enum VersionTypeOptions
        {
            // ReSharper disable once InconsistentNaming
            major = VersionType.Major,

            // ReSharper disable once InconsistentNaming
            minor = VersionType.Minor,
        }
    }
}