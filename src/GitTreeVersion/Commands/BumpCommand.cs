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
            Handler = CommandHandler.Create<VersionTypeOptions, bool, string?>(Execute);

            AddArgument(new Argument<VersionTypeOptions>("type"));
            AddArgument(new Argument<string?>("path", () => null));
        }

        private void Execute(VersionTypeOptions type, bool debug, string? path)
        {
            Log.IsDebug = debug;

            if (path is not null)
            {
                path = Path.GetFullPath(path);
            }

            path ??= Environment.CurrentDirectory;

            Console.WriteLine($"Bumping {type}");

            var versionGraph = ContextResolver.GetVersionGraph(new AbsoluteDirectoryPath(path));

            Console.WriteLine($"Version root path: {versionGraph.PrimaryVersionRootPath}");

            new Bumper().Bump(versionGraph, versionGraph.PrimaryVersionRootPath, (VersionType)type);
        }

        private enum VersionTypeOptions
        {
            // ReSharper disable once InconsistentNaming
            major = VersionType.Major,

            // ReSharper disable once InconsistentNaming
            minor = VersionType.Minor,

            // ReSharper disable once InconsistentNaming
            patch = VersionType.Patch,
        }
    }
}