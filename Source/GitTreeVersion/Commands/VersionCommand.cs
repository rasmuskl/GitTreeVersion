using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using GitTreeVersion.Context;
using GitTreeVersion.Paths;

namespace GitTreeVersion.Commands
{
    public class VersionCommand : Command
    {
        public VersionCommand() : base("version", "Versions the thing")
        {
            Handler = CommandHandler.Create<bool, bool, bool>(Execute);

            AddOption(new Option<bool>("--directory-build-props"));
            AddOption(new Option<bool>("--apply"));
        }

        private void Execute(bool directoryBuildProps, bool apply, bool debug)
        {
            Log.IsDebug = debug;

            var stopwatch = Stopwatch.StartNew();
            var fileGraph = ContextResolver.GetFileGraph(new AbsoluteDirectoryPath(Environment.CurrentDirectory));

            Console.WriteLine($"Repository root: {fileGraph.RepositoryRootPath}");
            Console.WriteLine($"Version root: {fileGraph.VersionRootPath}");

            var versionCalculator = new VersionCalculator();
            var version = versionCalculator.GetVersion(fileGraph);

            if (apply)
            {
                var relevantDeployables = fileGraph.DeployableFileVersionRoots.Where(p => p.Value == fileGraph.VersionRootPath).Select(p => p.Key).ToArray();

                foreach (var deployable in relevantDeployables)
                {
                    Console.WriteLine(deployable.FullName);

                    if (deployable.Extension == ".csproj")
                    {
                        var applier = new DotnetVersionApplier();
                        applier.ApplyVersion(deployable, version);
                    }
                }
            }

            if (directoryBuildProps)
            {
                var xDocument = new XDocument(
                    new XElement("Project",
                        new XElement("PropertyGroup",
                            new XElement("Version", version.ToString()))));


                xDocument.Save("Directory.Build.props");
                Console.WriteLine($"Wrote version {version} to Directory.Build.props");
            }

            Console.WriteLine($"Version: {version}");

            Log.Debug($"Elapsed: {stopwatch.ElapsedMilliseconds} ms");
        }
    }
}