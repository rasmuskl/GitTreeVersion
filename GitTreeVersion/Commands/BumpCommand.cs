using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using GitTreeVersion.Context;

namespace GitTreeVersion.Commands
{
    public class BumpCommand : Command
    {
        public BumpCommand() : base("bump", "Bump versions")
        {
            Handler = CommandHandler.Create<VersionType>(Execute);

            var typeArgument = new Argument<VersionType>("type");


            AddArgument(typeArgument);
        }

        private void Execute(VersionType type)
        {
            Console.WriteLine($"Bumping {type}");

            var fileGraph = ContextResolver.GetFileGraph(Environment.CurrentDirectory);
            var versionRootPath = fileGraph.VersionRootPath;
            Console.WriteLine($"Version root path: {versionRootPath}");

            var versionBumpDirectoryPath = Path.Combine(versionRootPath, ".version", type.ToString());
            var versionBumpFilePath = Path.Combine(versionBumpDirectoryPath, DateTime.UtcNow.ToString("yyyyMMddHHmmssff"));
            Directory.CreateDirectory(versionBumpDirectoryPath);
            File.WriteAllText(versionBumpFilePath, string.Empty);
        }

        private enum VersionType
        {
            // ReSharper disable once InconsistentNaming
            major,
            // ReSharper disable once InconsistentNaming
            minor
        }
    }
}
 